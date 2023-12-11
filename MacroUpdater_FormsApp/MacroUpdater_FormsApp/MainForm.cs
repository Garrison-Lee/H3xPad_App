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
        public const string PORT_NAME = "COM3";
        private SerialPort _port;

        // For serial communication protocol with our board XD
        public const string HEXLIMITER = ":H3X:";
        public const string GET = "GET";
        public const string PUT = "PUT";
        public const string LOG = "LOG";
        public const string READY = "RDY";
        public const string ERROR = "ERR";
        public const string OKK = "OKK";
        public const string TAP_MACRO = "TAP.TXT";
        public const string PRESS_MACRO = "PRESS.TXT";

        // We are expecting a response from the board
        // KISS, if you try to start a job while another thing is listening, I'm just going to drop your job
        // TODO: Something more involved
        public static bool _awaitingResponse = false;
        public static string _lastResponse = "";

        private string _savedTapMacro = "-1";
        private string _savedPressMacro = "-1";

        public MainForm()
        {
            InitializeComponent();
            tapTextBox.AutoSize = false;
            tapTextBox.Height = 28;
            pressTextBox.AutoSize = false;
            pressTextBox.Height = 28;

            // TODO: Screw this ListAll, just scan and auto-pick for aunty
            SerialUtility.ListAllCOMPorts();
            if (OpenSerialPortConnection())
            {
                userLog.Text = "Loading Macros from MacroPad...";
                _ = ReadMacrosFromCardAsync();
            }
        }

        private async Task ReadMacrosFromCardAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: Attempted to start a SerialCommunication ({nameof(ReadMacrosFromCardAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            _awaitingResponse = true;
            SendToPort($"{HEXLIMITER}{GET}{HEXLIMITER}{TAP_MACRO}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // No big rush, 10x a second
                await Task.Delay(100);
            }
            // As soon as we are done _awaitingResponse, _lastResponse is what we want!
            _savedTapMacro = _lastResponse;
            tapTextBox.Text = _lastResponse;
            tapTextBox.ForeColor = _lastResponse == ERROR ? Color.Red : Style.Blue;

            _awaitingResponse = true;
            SendToPort($"{HEXLIMITER}{GET}{HEXLIMITER}{PRESS_MACRO}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // No big rush, 10x a second
                await Task.Delay(100);
            }
            // As soon as we are done _awaitingResponse, _lastResponse is what we want!
            _savedPressMacro = _lastResponse;
            pressTextBox.Text = _lastResponse;
            pressTextBox.ForeColor = _lastResponse == ERROR ? Color.Red : Style.Blue;

            userLog.Text = "Displayed macros are up-to-date!";
        }

        private async Task UpdateMacrosAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: Attempted to start a SerialCommunication ({nameof(UpdateMacrosAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            bool tapFailed = false;
            bool pressFailed = false;

            _awaitingResponse = true;
            userLog.Text = "Saving macros to MacroPad...";
            SendToPort($"{HEXLIMITER}{PUT}{HEXLIMITER}{TAP_MACRO}{HEXLIMITER}{tapTextBox.Text}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }
            if (tapFailed = _lastResponse == ERROR)
                Console.WriteLine($"C: ERROR: Failed to save shortPress Macro!");

            // Once we are no longer awaiting response we can move on to the next file
            _awaitingResponse = true;
            SendToPort($"{HEXLIMITER}{PUT}{HEXLIMITER}{PRESS_MACRO}{HEXLIMITER}{pressTextBox.Text}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }
            if (pressFailed = _lastResponse == ERROR)
                Console.WriteLine($"C: ERROR: Failed to save longPress Macro!");

            userLog.Text = $"{(tapFailed ? "FAILED to save shortPress Macro" : "shortPress Saved")} - {(pressFailed ? "FAILED to save longPress Macro" : "longPress Saved")}";
            _ = ReadMacrosFromCardAsync();
        }

        private async Task PrintCardContentsAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"C: ERROR: Attempted to start a SerialCommunication ({nameof(PrintCardContentsAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            _awaitingResponse = true;
            SendToPort($"{HEXLIMITER}{LOG}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }
        }

        private async Task PingBoard()
        {
            // Unlike the others it does not check to see if it's going to mess anything up
            //  use this as a debugging type of thing
            _awaitingResponse = true;
            SendToPort($"{HEXLIMITER}{READY}{HEXLIMITER}");
        }

        private void SendToPort(string msg)
        {
            if (!_port.IsOpen)
            {
                userLog.Text = $"Failed to update, port {PORT_NAME} is closed!";
                return;
            }
            Console.WriteLine($"C: Writing {msg} to the serial port!");
            _port.WriteLine(msg);
        }

        

        private string buffer = "";
        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            buffer = _port.ReadExisting();
            Console.WriteLine($"C: RAW CIPO: {buffer}");
            if (!buffer.EndsWith("\n"))
                return;

            // Parse things only when we have an entire line at a time
            if (_awaitingResponse)
            {
                if (buffer.StartsWith(HEXLIMITER))
                {
                    int closeIndex = buffer.LastIndexOf(HEXLIMITER);
                    _lastResponse = buffer.Substring(HEXLIMITER.Length, closeIndex - HEXLIMITER.Length);
                    Console.WriteLine($"C: Response Detected: {_lastResponse}");
                    _awaitingResponse = false;
                }
            }

            // One entire line dealt with at a time
            buffer = "";
        }

        #region START_UP_SHUT_DOWN
        // Creates a SerialPort object on a specific COM port, configures the port
        //  and subscribes to the port's OnDataReceived event
        private bool OpenSerialPortConnection()
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
                FormClosing += CloseSerialPortConnection;
                Console.WriteLine($"Opened port {_port.PortName}");
                userLog.Text = "Connected to MacroPad";
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // TODO: Instructions for aunty
                userLog.Text = "Failed to connect to MacroPad...";
                return false;
            }
        }

        private void CloseSerialPortConnection(object sender, FormClosingEventArgs e)
        {
            _port.Close();
            Console.WriteLine($"Closed port {PORT_NAME}");
        }
        #endregion

        #region FORM_EVENTS
        private void UpdateButton_Click(object sender, System.EventArgs args)
        {
            _ = UpdateMacrosAsync();
        }

        private void tapTextBox_TextChanged(object sender, EventArgs e)
        {
            if (tapTextBox.Text != _savedTapMacro)
            {
                // Indicate there's an unsaved delta!
                tapTextBox.ForeColor = Style.Mustard;
            }
            else
            {
                tapTextBox.ForeColor = Style.Blue;
            }
        }

        private void pressTextBox_TextChanged(object sender, EventArgs e)
        {
            if (pressTextBox.Text != _savedPressMacro)
            {
                // Indicate there's an unsaved delta!
                pressTextBox.ForeColor = Style.Mustard;
            }
            else
            {
                pressTextBox.ForeColor = Style.Blue;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _ = PrintCardContentsAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _ = PingBoard();
        }
        #endregion
    }
}
