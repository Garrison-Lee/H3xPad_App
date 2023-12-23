using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO.Ports;
using System.Threading.Tasks;

namespace MacroUpdater_FormsApp
{
    public partial class MainForm : Form
    {
        public const string PORT_NAME = "COM5";
        private SerialPort _port;
        private const long TIMEOUT = TimeSpan.TicksPerSecond * 10;

        // For serial communication protocol with our board XD
        // All communication will be wrapped in tags and have no reliance on /n newlines
        public const int MAX_MACRO_LENGTH = 64;
        public const string OPEN_MAIN = "<H3X>";
        public const string CLOSE_MAIN = "</H3X>";
        public const string OPEN_VERBOSE = "<VBE>";
        public const string CLOSE_VERBOSE = "</VBE>";
        private static readonly Dictionary<string, string> TagPairs = new Dictionary<string, string>() { { OPEN_MAIN, CLOSE_MAIN }, { OPEN_VERBOSE, CLOSE_VERBOSE } };
        private string _openingTag = ""; // So we know what closing tag we're looking for!
        // Used for splitting args or whatever within a response/command
        public const string INTERNAL_DELIMITER = "<->";

        // Known keywords
        // Board will read from a text file, name given as arg
        public const string GET = "GET";
        // Board will update the contents of a text file, name and contents given as args
        public const string PUT = "PUT";
        // Board will return a read-only representation of the contents on its disc
        public const string LOG = "LOG";
        // Used as a ping/communication-estabilishing message
        public const string READY = "RDY";
        // Used to return the status of the board's request resolution
        public const string ERROR = "ERR";
        public const string OKK = "OKK";

        // Names of files on the H3xPad's disc, and a default
        //  message to show in our frontend before the actual macros have been read
        // UPDATE: FileNames are dicated by H3xPad now, from our side we just request by 0 or 1
        public const int TAP_MACRO_ID = 0;
        public const int PRESS_MACRO_ID = 1;
        private const string DEFAULT_MACRO = "loading from H3xPad...";

        // We are expecting a response from the board
        // KISS, if you try to start a job while another thing is listening, I'm just going to drop your job
        // TODO: Something more involved
        public static bool _awaitingResponse = false;
        public static string _lastResponse = "";
        private static string _buffer = "";

        private string _savedTapMacro = "";
        private string _savedPressMacro = "";

        private bool _connected = false;
        private bool _boardReady = false;

        // Our app-side flow of locking control, waiting for response, etc.
        // This is NOT the result from the board. That will be in _lastResponse
        public enum CommunicationResult : byte
        {
            // No errors, consuming context should continue on
            OK = 0,
            // Someone already waiting response, Communication denied
            BUSY = 5,
            // Uh oh, consuming context should stop and inform user
            ERROR = 10,
        }

        public MainForm()
        {
            InitializeComponent();
            tapTextBox.AutoSize = false;
            tapTextBox.Height = 28;
            tapTextBox.Text = DEFAULT_MACRO;
            pressTextBox.AutoSize = false;
            pressTextBox.Height = 28;
            pressTextBox.Text = DEFAULT_MACRO;

            // TODO: Screw this ListAll, just scan and auto-pick for aunty
            // TODO: This is going to be critical because the effing boards are lil' bitches
            SerialUtility.ListAllCOMPorts();
            ConnectToBoard();

            // TODO: SANITIZE user input
        }

        // Whether or not we are within a tag. i.e. if the board is still sending more
        private bool _midTag = false;
        /// <summary>
        /// Our event handler for something showing up in the SerialPort's buffer. Basically, we wait
        ///  until we see an OPENing tag. At that point we grab everything up until the matching CLOSE
        ///  tag and then handle the response. VERBOSE messages are simply logged. MAIN messages are
        ///  put in _lastResponse for other Tasks/whatevah to consume.
        /// </summary>
        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            // We will clear our _buffer as we receive and handle complete messages wrapped in a tag
            // TODO: We are accruing closing tags somehow. Clearly I don't understand how this port is being read from
            //Console.WriteLine($"\nAPP: DATA RECEIVED! _buffer before reading existing: '{_buffer}'");
            _buffer += _port.ReadExisting();
            //Console.WriteLine($"APP: DATA RECEIVED! _buffer after reading existing: '{_buffer}'\n");

            // Waiting for an opening tag to appear!
            if (!_midTag)
            {
                int mainStartDex = _buffer.IndexOf(OPEN_MAIN);
                int verboseStartDex = _buffer.IndexOf(OPEN_VERBOSE);
                // if both are -1, we are not mid-tag.
                // if only is >-1, we only have that tag to worry about
                // if BOTH are >-1, we have queued responses in our buffer and should go left to right!
                if (_midTag = mainStartDex > -1 || verboseStartDex > -1)
                {
                    _openingTag = (mainStartDex > -1 && (verboseStartDex < 0 || mainStartDex < verboseStartDex)) ? OPEN_MAIN : OPEN_VERBOSE;

                    // Now that we know we're mid-tag and know what the opening tag is
                    //  we can actually drop everything before the contents of the message
                    int startDex = _buffer.IndexOf(_openingTag);
                    _buffer = _buffer.Substring(startDex + _openingTag.Length);
                }
            }

            // THIS IS NOT AN ELSE, we could have received a full line at once!
            if (_midTag)
            {
                // If we have the closing tag that matches our openingTag...
                int closeDex = _buffer.IndexOf(TagPairs[_openingTag]);
                if (closeDex > -1) // we found the closing tag!
                {
                    _midTag = false;

                    if (_openingTag == OPEN_MAIN)
                    {
                        // TODO: Do we need to enforce some sort of check to make sure _lastResponse was consumed
                        //  by our frontend before we allow another response to be parsed? ResponseQueue?
                        _lastResponse = _buffer.Substring(0, closeDex - 0);
                        _awaitingResponse = false;
                        Console.WriteLine($"BOARD: {_buffer.Substring(0, closeDex - 0)}\n");
                    }
                    else if (_openingTag == OPEN_VERBOSE)
                    {
                        // this has nothing to do with an official response and is for debugging
                        Console.WriteLine($"BOARD (VERBOSE): {_buffer.Substring(0, closeDex - 0)}");
                    }

                    // Now that we've parsed from the buffer and cached the lastResponse etc. we can drop up through here in the buffer
                    _buffer = _buffer.Substring(closeDex + TagPairs[_openingTag].Length);
                    // Kick this off again in this case because we might have a second response queued up
                    OnSerialPortDataReceived(sender, args);
                }
            }
        }

        /// <summary>
        /// This is our main wrapper for sending a command to a H3xPad. This Task will handle wrapping
        ///  our directive in the proper delimiter/tags, sending it to the board, and only returning
        ///  once either we receive a response or timeout
        /// </summary>
        /// <param name="msg">Message to send to H3xPad. Do not include bookend-tags</param>
        /// <param name="PID">In case of failure, a msg will be logged blaming this string</param>
        /// <returns></returns>
        private async Task<CommunicationResult> CommandBoard(string msg, string PID = "Unspecified Process")
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"APP: ERROR: '{PID}' Attemtped to start a SerialCommunication while we are already awaiting a response. Ignoring this request!");
                return CommunicationResult.BUSY;
            }

            Console.WriteLine($"APP: {PID} is about to send, over serial: '{OPEN_MAIN}{msg}{CLOSE_MAIN}'");
            _awaitingResponse = true;
            DisableUI();
            SendToPort($"{OPEN_MAIN}{msg}{CLOSE_MAIN}");
            long startTime = DateTime.Now.Ticks;
            while (_awaitingResponse)
            {
                // polling at ~30x / sec
                await Task.Delay(33);
                if (DateTime.Now.Ticks - startTime > TIMEOUT)
                {
                    // TODO: Somehow we have like three closing tags here that shouldn't be happening
                    Console.WriteLine($"APP: ERROR: '{PID}' Experienced Timeout while waiting for response!");
                    Console.WriteLine($"APP: Current buffer: {_buffer}");
                    CloseSerialPortConnection();
                    //EnableUI() in CloseSerialPortConnection();
                    return CommunicationResult.ERROR;
                }
            }

            EnableUI();
            return CommunicationResult.OK;
        }
        // A simple wrapper that sends an exact string over serial. Do not use
        //  directly
        private void SendToPort(string msg)
        {
            if (!_port.IsOpen)
            {
                TellUser($"Failure! Port {PORT_NAME} is closed!", LogStates.ERROR);
                CloseSerialPortConnection();
                return;
            }
            _port.Write(msg);
        }

        /// <summary>
        /// This Task will request the current macros from the H3xPad's on-board storage. Upon
        ///  successful retrieval, our app's input-text-boxes will be updated to show the current
        ///  values!
        /// </summary>
        private async Task ReadMacrosFromCardAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"APP: ERROR: {nameof(ReadMacrosFromCardAsync)} was invoked while board was already being communicated with by another process!");
                return;
            }

            switch (await CommandBoard($"{GET}{INTERNAL_DELIMITER}{TAP_MACRO_ID}", nameof(ReadMacrosFromCardAsync)))
            {
                case CommunicationResult.OK:
                    // As soon as we are done _awaitingResponse, _lastResponse is what we want!
                    _savedTapMacro = _lastResponse;
                    tapSubmitToggle.Checked = _lastResponse.EndsWith("\n");
                    tapTextBox.Text = _lastResponse.TrimEnd();
                    tapTextBox.ForeColor = _lastResponse.StartsWith(ERROR) ? Color.Red : Style.Blue;
                    break;

                case CommunicationResult.ERROR:
                    // Communication error, port will be closed by CommandBoard process
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"APP: ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(ReadMacrosFromCardAsync)}");
                    return;
            }

            switch (await CommandBoard($"{GET}{INTERNAL_DELIMITER}{PRESS_MACRO_ID}", nameof(ReadMacrosFromCardAsync)))
            {
                case CommunicationResult.OK:
                    // As soon as we are done _awaitingResponse, _lastResponse is what we want!
                    _savedPressMacro = _lastResponse;
                    pressSubmitToggle.Checked = _lastResponse.EndsWith("\n");
                    pressTextBox.Text = _lastResponse.TrimEnd();
                    pressTextBox.ForeColor = _lastResponse.StartsWith(ERROR) ? Color.Red : Style.Blue;
                    break;

                case CommunicationResult.ERROR:
                    // Communication error, port will be closed by CommandBoard process
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"APP: ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(ReadMacrosFromCardAsync)}");
                    return;
            }

            TellUser("Displayed macros are up to date!");
        }

        /// <summary>
        /// Saves the macros that are currently in the text input fields to the H3xPad's on-board sd card.
        ///  We do this by sending, one-at-a-time, a PUT command, a fileName, and the macroContents to the
        ///  H3xPad over serial and waiting for response(s).
        /// </summary>
        private async Task SaveMacrosToCardAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"APP: ERROR: {nameof(SaveMacrosToCardAsync)} was invoked while board was already being communicated with by another process!");
                return;
            }

            if (tapTextBox.Text == DEFAULT_MACRO || pressTextBox.Text == DEFAULT_MACRO)
            {
                Console.WriteLine($"APP: ERROR: {nameof(SaveMacrosToCardAsync)} tried to save out the DEFAULT_MACRO, what's going on?");
                return;
            }

            // Each one of these should be flipped true if the board responds OK, at the end
            //  we use these flags for user messages.
            bool tapSaved = false;
            bool pressSaved = false;
            TellUser("Saving macros to MacroPad...");

            string macro = tapTextBox.Text;
            if (tapSubmitToggle.Checked)
                macro += "\n";
            switch (await CommandBoard($"{PUT}{INTERNAL_DELIMITER}{TAP_MACRO_ID}{INTERNAL_DELIMITER}{macro}", nameof(SaveMacrosToCardAsync)))
            {
                case CommunicationResult.OK:
                    // Board could say saving failed without a CommunicationResult ERROR
                    if (tapSaved = _lastResponse == tapTextBox.Text + (tapSubmitToggle.Checked ? "\n" : ""))
                        _savedTapMacro = _lastResponse;
                    else
                        Console.WriteLine($"APP: ERROR: Failed to save shortPress Macro!");
                    break;

                case CommunicationResult.ERROR:
                    // Main handling of this is done by CommandBoard() itself
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"APP: ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(SaveMacrosToCardAsync)}");
                    return;
            }

            macro = pressTextBox.Text;
            if (pressSubmitToggle.Checked)
                macro += "\n";
            switch (await CommandBoard($"{PUT}{INTERNAL_DELIMITER}{PRESS_MACRO_ID}{INTERNAL_DELIMITER}{macro}", nameof(SaveMacrosToCardAsync)))
            {
                case CommunicationResult.OK:
                    // Board could say saving failed without a CommunicationResult ERROR
                    if (pressSaved = _lastResponse == pressTextBox.Text + (pressSubmitToggle.Checked ? "\n" : ""))
                        _savedPressMacro = _lastResponse;
                    else
                        Console.WriteLine($"APP: ERROR: Failed to save longPress Macro!");
                    break;

                case CommunicationResult.ERROR:
                    // Main handling of this is done by CommandBoard() itself
                    return;

                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"APP: ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(SaveMacrosToCardAsync)}");
                    return;
            }

            UpdateTextBoxStyle();
            TellUser($"{(!tapSaved || !pressSaved ? "Failed to save!" : "Saved!")}", !pressSaved || !tapSaved ? LogStates.ERROR : LogStates.OK);
        }

        /// <summary>
        /// Sends a request to the H3xPad to log out the contents of the onboard SD card.
        ///  H3xPad will send this back in Verbose Tags
        /// </summary>
        private async Task PrintCardContentsAsync()
        {
            CommunicationResult res = await CommandBoard(LOG, nameof(PrintCardContentsAsync));
        }

        #region START_UP_SHUT_DOWN
        // Creates a SerialPort object on a specific COM port, configures the port
        //  and subscribes to the port's OnDataReceived event
        private bool ConnectToBoard()
        {
            if (_connected)
                CloseSerialPortConnection();

            // TODO: Scan Ports to find and match the board, hardcoded port rn
            _port = new SerialPort(PORT_NAME, 9600)
            {
                // Configured to match the way the arduino sends its shit
                DtrEnable = true,
                RtsEnable = true,
                DataBits = 8,
                StopBits = StopBits.One,
            };
            _port.DataReceived += OnSerialPortDataReceived;
            _port.ErrorReceived += OnSerialPortErrorReceived;

            try
            {
                _port.Open();
                _connected = true;
                FormClosing += CloseSerialPortConnection;
                TellUser("Loading Macros from MacroPad...");
                updateButton.Text = "Update";
                Console.WriteLine($"APP: Opened port {_port.PortName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"APP: {e}");
                // TODO: Instructions for aunty
                TellUser("Failed to connect to MacroPad...", LogStates.ERROR);
                _connected = false;
                updateButton.Text = "Retry Connect";
                return false;
            }


            // We will have this very first async call spin it's wheels to wait to see _port open
            _ = EstablishBoardIsReady();
            return true;
        }

        private void OnSerialPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine($"APP: Serial received ERROR: {e}");
        }

        // Will send a READY message to the board and waits for the board to respond with READY.
        //  Will timeout eventually if the board doesn't respond
        private async Task EstablishBoardIsReady()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"APP: ERROR: {nameof(EstablishBoardIsReady)} was invoked while board was already being communicated with by another process!");
                return;
            }

            Console.WriteLine($"APP: H3xPad, are you RDY?");
            switch (await CommandBoard($"{READY}", nameof(EstablishBoardIsReady)))
            {
                case CommunicationResult.OK:
                    _boardReady = _lastResponse == READY;
                    break;
                default:
                    Console.WriteLine("APP: Communication error during RDY");
                    _boardReady = false;
                    break;
            }

            if (_boardReady)
            {
                OnH3xPadHandshakeSucceeded();
            }
            else
            {
                OnH3xPadHandshakeFailed();
            }
        }

        private void CloseSerialPortConnection(object sender, FormClosingEventArgs e)
        {
            CloseSerialPortConnection();
        }
        private void CloseSerialPortConnection()
        {
            _port.Close();
            _connected = false;
            _boardReady = false;
            _awaitingResponse = false;
            _lastResponse = "";
            _savedPressMacro = "";
            _savedTapMacro = "";
            _buffer = "";
            TellUser("Not Connected to MacroPad...", LogStates.ERROR);
            updateButton.Text = "Retry Connect";
            EnableUI();
            Console.WriteLine($"APP: Closed port {PORT_NAME}");
        }


        private void OnH3xPadHandshakeSucceeded()
        {
            _ = ReadMacrosFromCardAsync();
        }

        private void OnH3xPadHandshakeFailed()
        {
            CloseSerialPortConnection();
        }
        #endregion

        #region FORM_STUFF 
        private void EnableUI()
        {
            updateButton.Enabled = true;
            updateButton.FlatAppearance.BorderColor = Style.Blue;

            tapTextBox.Enabled = true;
            pressTextBox.Enabled = true;
        }

        private void DisableUI()
        {
            updateButton.Enabled = false;
            updateButton.FlatAppearance.BorderColor = Style.Gray;

            tapTextBox.Enabled = false;
            pressTextBox.Enabled = false;
        }

        private void TellUser(string msg, LogStates state = LogStates.OK)
        {
            switch (state)
            {
                case LogStates.OK:
                    userLog.ForeColor = Style.Mustard;
                    break;

                case LogStates.WARN:
                    userLog.ForeColor = Style.Orange;
                    break;

                case LogStates.ERROR:
                    userLog.ForeColor = Color.OrangeRed;
                    break;

                default:
                    break;
            }

            userLog.Text = msg;
        }

        private void UpdateButton_Click(object sender, System.EventArgs args)
        {
            if (_boardReady)
            {
                _ = SaveMacrosToCardAsync();
            }
            else
            {
                ConnectToBoard();
            }
        }

        private void UpdateTextBoxStyle(string prependMsg = "")
        {
            // We don't want to look like we're live in this case
            if (!_boardReady) 
                return;

            bool tapGood = tapTextBox.Text + (tapSubmitToggle.Checked ? "\n" : "") == _savedTapMacro;
            bool pressGood = pressTextBox.Text + (pressSubmitToggle.Checked ? "\n" : "") == _savedPressMacro;

            // Individual box colors
            tapTextBox.ForeColor = tapGood ? Style.Blue : Style.Orange;
            pressTextBox.ForeColor = pressGood ? Style.Blue : Style.Orange;

            // UserLog message
            TellUser(prependMsg + (tapGood && pressGood ? "Displayed Macros are up to date!" : "Unsaved changes..."), LogStates.OK);
        }

        private void TapTextBox_TextChanged(object sender, EventArgs e)
        {
            //NOTE: MAX_LENGTH-1 to leave space for an optional \n character set by toggle
            bool truncated;
            if (truncated = tapTextBox.Text.Length > MAX_MACRO_LENGTH-1)
                tapTextBox.Text = tapTextBox.Text.Substring(0, MAX_MACRO_LENGTH-1);

            UpdateTextBoxStyle(truncated ? $"Macro trimmed to {MAX_MACRO_LENGTH-1} characters. Call Garrison if you need support for a longer macro!\n" : "");
        }

        private void PressTextBox_TextChanged(object sender, EventArgs e)
        {
            bool truncated;
            if (truncated = pressTextBox.Text.Length > MAX_MACRO_LENGTH-1)
                pressTextBox.Text = pressTextBox.Text.Substring(0, MAX_MACRO_LENGTH-1);

            UpdateTextBoxStyle(truncated ? $"Macro trimmed to {MAX_MACRO_LENGTH-1} characters. Call Garrison if you need support for a longer macro!\n" : "");
        }

        private void TapSubmitToggle_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBoxStyle();
        }

        private void PressSubmitToggle_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBoxStyle();
        }
        #endregion
    }
}
