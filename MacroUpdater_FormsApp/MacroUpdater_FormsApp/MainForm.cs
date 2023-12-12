using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Management;

namespace MacroUpdater_FormsApp
{
    public partial class MainForm : Form
    {
        public const string PORT_NAME = "COM5";
        private SerialPort _port;
        private const long TIMEOUT = TimeSpan.TicksPerSecond * 10;

        // For serial communication protocol with our board XD
        // Command/response wrapper delimeter
        public const string HEXLIMITER = ":H3X:";
        // Passing args w/ commands to board, within a HEXLIMITER wrapping
        public const string INTERNAL_LIM = "/-/";
        // Wrapper board uses when sharing debug information
        public const string VERBOSE_LIM = "<esobreV>";
        public const string GET = "GET";
        public const string PUT = "PUT";
        public const string LOG = "LOG";
        public const string READY = "RDY";
        public const string ERROR = "ERR";
        public const string OKK = "OKK";
        public const string TAP_MACRO = "TAP.TXT";
        public const string PRESS_MACRO = "PRESS.TXT";

        private const string DEFAULT_MACRO = "loading from MacroPad...";

        // We are expecting a response from the board
        // KISS, if you try to start a job while another thing is listening, I'm just going to drop your job
        // TODO: Something more involved
        public static bool _awaitingResponse = false;
        public static string _lastResponse = "";
        private static string _buffer = "";

        private string _savedTapMacro = "";
        private string _savedPressMacro = "";

        private bool _connected = false;

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

            // TODO: Really need to come up with rules for the Macros and commit to them and set up sanitizing
        }

        private async Task ReadMacrosFromCardAsync(bool awaitPort = false)
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: {nameof(ReadMacrosFromCardAsync)} was invoked while board was already being communicated with by another process!");
                return;
            }

            switch (await CommandBoard($"{GET}{INTERNAL_LIM}{TAP_MACRO}", nameof(ReadMacrosFromCardAsync), awaitPort))
            {
                case CommunicationResult.OK:
                    // As soon as we are done _awaitingResponse, _lastResponse is what we want!
                    _savedTapMacro = _lastResponse;
                    tapTextBox.Text = _lastResponse;
                    tapTextBox.ForeColor = _lastResponse == ERROR ? Color.Red : Style.Blue;
                    break;

                case CommunicationResult.ERROR:
                    // Communication error, port will be closed by CommandBoard process
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(ReadMacrosFromCardAsync)}");
                    return;
            }

            switch (await CommandBoard($"{GET}{INTERNAL_LIM}{PRESS_MACRO}", nameof(ReadMacrosFromCardAsync)))
            {
                case CommunicationResult.OK:
                    // As soon as we are done _awaitingResponse, _lastResponse is what we want!
                    _savedPressMacro = _lastResponse;
                    pressTextBox.Text = _lastResponse;
                    pressTextBox.ForeColor = _lastResponse == ERROR ? Color.Red : Style.Blue;
                    TellUser("Displayed macros are up to date!");
                    return;

                case CommunicationResult.ERROR:
                    // Communication error, port will be closed by CommandBoard process
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(ReadMacrosFromCardAsync)}");
                    return;
            }
        }

        private async Task SaveMacrosToCardAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: {nameof(SaveMacrosToCardAsync)} was invoked while board was already being communicated with by another process!");
                return;
            }

            if (tapTextBox.Text == DEFAULT_MACRO || pressTextBox.Text == DEFAULT_MACRO)
            {
                Console.WriteLine($"ERROR: {nameof(SaveMacrosToCardAsync)} tried to save out the DEFAULT_MACRO, what's going on?");
                return;
            }

            bool tapSaved = false;
            bool pressSaved = false;

            TellUser("Saving macros to MacroPad...");
            switch (await CommandBoard($"{PUT}{INTERNAL_LIM}{TAP_MACRO}{INTERNAL_LIM}{tapTextBox.Text}", nameof(SaveMacrosToCardAsync)))
            {
                case CommunicationResult.OK:
                    // Board could say saving failed without a CommunicationResult ERROR
                    if (tapSaved = _lastResponse == tapTextBox.Text)
                        _savedTapMacro = _lastResponse;
                    else
                        Console.WriteLine($"ERROR: Failed to save shortPress Macro!");
                    break;

                case CommunicationResult.ERROR:
                    // Main handling of this is done by CommandBoard() itself
                    return;
                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(SaveMacrosToCardAsync)}");
                    return;
            }

            switch (await CommandBoard($"{PUT}{INTERNAL_LIM}{PRESS_MACRO}{INTERNAL_LIM}{pressTextBox.Text}", nameof(SaveMacrosToCardAsync)))
            {
                case CommunicationResult.OK:
                    // Board could say saving failed without a CommunicationResult ERROR
                    if (pressSaved = _lastResponse == pressTextBox.Text)
                        _savedPressMacro = _lastResponse;
                    else
                        Console.WriteLine($"ERROR: Failed to save longPress Macro!");
                    break;

                case CommunicationResult.ERROR:
                    // Main handling of this is done by CommandBoard() itself
                    return;

                case CommunicationResult.BUSY:
                default:
                    // Shouldn't be possible since we're checking for this above...
                    Console.WriteLine($"ERROR: Unexpected CommunicationResult of after invoking CommandBoard from {nameof(SaveMacrosToCardAsync)}");
                    return;
            }

            UpdateTextBoxStyle();
            TellUser($"{(!tapSaved || !pressSaved ? "Failed to save!" : "Saved!")}", !pressSaved || !tapSaved ? LogStates.ERROR : LogStates.OK);
        }

        private async Task PrintCardContentsAsync()
        {
            CommunicationResult res = await CommandBoard(LOG, nameof(PrintCardContentsAsync));
        }

        // Does not wait for anything, more of a debugging thing
        private void PingBoard()
        {
            // Unlike the others it does not check to see if it's going to mess anything up
            //  use this as a debugging type of thing
            SendToPort($"{HEXLIMITER}{READY}{HEXLIMITER}");
        }

        /// <param name="msg">Message to send to board, do not include opening or closing HEXLIMITER</param>
        /// <param name="PID">For logging purposes only, who should I say timed out, etc.</param>
        /// <returns>Nothing, but only after the board has responded or we've timed out</returns>
        private async Task<CommunicationResult> CommandBoard(string msg, string PID = "Unspecified Process", bool awaitPort = false)
        {
            if (awaitPort)
            {
                long start = DateTime.Now.Ticks;
                while (!_port.IsOpen)
                {
                    await Task.Delay(33);

                    if (DateTime.Now.Ticks - start >= TIMEOUT)
                    {
                        Console.WriteLine($"ERROR: '{PID}' Took too long waiting to see the _port open!");
                        return CommunicationResult.ERROR;
                    }
                }
            }

            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: '{PID}' Attemtped to start a SerialCommunication while we are already awaiting a response. Ignoring this request!");
                return CommunicationResult.BUSY;
            }

            _awaitingResponse = true;
            DisableUI();
            SendToPort($"{HEXLIMITER}{msg}{HEXLIMITER}");
            long startTime = DateTime.Now.Ticks;
            while (_awaitingResponse)
            {
                // polling at ~30x / sec
                await Task.Delay(33);
                if (DateTime.Now.Ticks - startTime > TIMEOUT)
                {
                    Console.WriteLine($"ERROR: '{PID}' Experienced Timeout while waiting for response!");
                    CloseSerialPortConnection();
                    //EnableUI() in CloseSerialPortConnection();
                    return CommunicationResult.ERROR;
                }
            }

            EnableUI();
            return CommunicationResult.OK;
        }
        // How our app-side flow of locking control, waiting for response, etc. terminated. NOT dictated by the board
        public enum CommunicationResult : byte
        {
            // No errors, consuming context should continue on
            OK = 0,
            // Someone already waiting response, Communication denied
            BUSY = 5,
            // Uh oh, consuming context should stop and inform user
            ERROR = 10,
        }

        private void SendToPort(string msg)
        {
            if (!_port.IsOpen)
            {
                TellUser($"Failed to update, port {PORT_NAME} is closed!", LogStates.ERROR);
                CloseSerialPortConnection();
                return;
            }
            Console.WriteLine($"Writing {msg} to the serial port!");
            _port.WriteLine(msg);
        }

        private bool _freshBuffer = true;
        private bool _hexLim = true;
        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            // Board now promises tags to wrap our buffer
            _buffer += _port.ReadExisting();
            _port.DiscardInBuffer();

            if (_freshBuffer)
            {
                // This fresh buffer doesn't start with a tag...
                if (_buffer.StartsWith(VERBOSE_LIM))
                {
                    _hexLim = false;
                    _freshBuffer = false;
                }
                else if (_buffer.StartsWith(HEXLIMITER))
                {
                    _hexLim = true;
                    _freshBuffer = false;
                }
                else
                {
                    Console.WriteLine($"WARNING: Buffer does not begin with a tag, could we have started mid-stream? Dumping buffer up to next tag, this error could cascade since we're off now");
                    _buffer = "";
                }
            }
            if (_hexLim && _buffer.EndsWith(HEXLIMITER))
            {
                // RESPONSE detected!
                int closeIndex = _buffer.IndexOf(HEXLIMITER, HEXLIMITER.Length);
                _lastResponse = _buffer.Substring(HEXLIMITER.Length, closeIndex - HEXLIMITER.Length);
                Console.WriteLine($"< BOARD > `{_lastResponse}`\n");
                _awaitingResponse = false;
                
            }
            else if (!_hexLim && _buffer.EndsWith(VERBOSE_LIM))
            {
                // VERBOSE response detected
                int closeIndex = _buffer.IndexOf(VERBOSE_LIM, VERBOSE_LIM.Length);
                Console.WriteLine($"< (VERBOSE) >\t{_buffer.Substring(VERBOSE_LIM.Length, closeIndex - VERBOSE_LIM.Length)}");
            }
            else
            {
                // Buffer still accumulating response!
                return;
            }

            // One entire line dealt with at a time
            _buffer = "";
            _freshBuffer = true;
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

        #region START_UP_SHUT_DOWN
        // Creates a SerialPort object on a specific COM port, configures the port
        //  and subscribes to the port's OnDataReceived event
        private bool ConnectToBoard()
        {
            // TODO: Scan Ports to find and match the board, hardcoded port rn
            _port = new SerialPort(PORT_NAME, 9600)
            {
                // Configured to match the way the arduino sends its shit
                DtrEnable = true,
                RtsEnable = true,
                DataBits = 8,
                StopBits = StopBits.One
            };
            _port.DataReceived += OnSerialPortDataReceived;

            try
            {
                _port.Open();
                _connected = true;
                FormClosing += CloseSerialPortConnection;
                TellUser("Loading Macros from MacroPad...");
                updateButton.Text = "Update";
                Console.WriteLine($"Opened port {_port.PortName}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // TODO: Instructions for aunty
                TellUser("Failed to connect to MacroPad...", LogStates.ERROR);
                _connected = false;
                updateButton.Text = "Retry Connect";
                return false;
            }


            // We will have this very first async call spin it's wheels to wait to see _port open
            _ = ReadMacrosFromCardAsync(awaitPort: true);
            return true;
        }

        private void CloseSerialPortConnection(object sender, FormClosingEventArgs e)
        {
            CloseSerialPortConnection();
        }
        private void CloseSerialPortConnection()
        {
            _port.Close();
            _connected = false;
            _awaitingResponse = false;
            _lastResponse = "";
            _savedPressMacro = "";
            _savedTapMacro = "";
            _buffer = "";
            TellUser("Not Connected to MacroPad...", LogStates.ERROR);
            updateButton.Text = "Retry Connect";
            EnableUI();
            Console.WriteLine($"Closed port {PORT_NAME}");
        }
        #endregion

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

        #region FORM_EVENTS
        private void UpdateButton_Click(object sender, System.EventArgs args)
        {
            if (_connected)
            {
                _ = SaveMacrosToCardAsync();
            }
            else
            {
                ConnectToBoard();
            }
        }

        private void UpdateTextBoxStyle()
        {
            // We don't want to look like we're live in this case
            if (!_connected) 
                return;

            bool tapGood = tapTextBox.Text == _savedTapMacro;
            bool pressGood = pressTextBox.Text == _savedPressMacro;

            // Individual box colors
            tapTextBox.ForeColor = tapGood ? Style.Blue : Style.Orange;
            pressTextBox.ForeColor = pressGood ? Style.Blue : Style.Orange;

            // UserLog message
            userLog.Text = tapGood && pressGood ? "Displayed Macros are up to date!" : "Unsaved changes...";
        }

        private void tapTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxStyle();
        }

        private void pressTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTextBoxStyle();
        }
        #endregion
    }
}
