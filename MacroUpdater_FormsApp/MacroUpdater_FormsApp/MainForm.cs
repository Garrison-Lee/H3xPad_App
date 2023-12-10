using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace MacroUpdater_FormsApp
{
    public partial class MainForm : Form
    {
        public const string PORT_NAME = "COM7";
        private SerialPort _port;

        // For serial communication protocol with our board XD
        public const string HEXLIMITER = ":H3X:";
        public const string GET = "GET";
        public const string PUT = "PUT";
        public const string LOG = "LOG";
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

            OpenSerialPortConnection();
            _ = ReadMacrosFromCardAsync();
        }

        private async Task ReadMacrosFromCardAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: Attempted to start a SerialCommunication ({nameof(ReadMacrosFromCardAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            _awaitingResponse = true;
            SendToPort($"{GET}{HEXLIMITER}{TAP_MACRO}");
            while (_awaitingResponse)
            {
                // No big rush, 10x a second
                await Task.Delay(100);
            }
            // As soon as we are done _awaitingResponse, _lastResponse is what we want!
            _savedTapMacro = _lastResponse;
            tapTextBox.Text = _lastResponse;

            _awaitingResponse = true;
            SendToPort($"{GET}{HEXLIMITER}{PRESS_MACRO}");
            while (_awaitingResponse)
            {
                // No big rush, 10x a second
                await Task.Delay(100);
            }
            // As soon as we are done _awaitingResponse, _lastResponse is what we want!
            _savedPressMacro = _lastResponse;
            pressTextBox.Text = _lastResponse;
        }

        private async Task UpdateMacrosAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: Attempted to start a SerialCommunication ({nameof(UpdateMacrosAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            _awaitingResponse = true;
            SendToPort($"{PUT}{HEXLIMITER}{TAP_MACRO}{HEXLIMITER}{tapTextBox.Text}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }

            // Once we are no longer awaiting response we can move on to the next file
            _awaitingResponse = true;
            SendToPort($"{PUT}{HEXLIMITER}{PRESS_MACRO}{HEXLIMITER}{pressTextBox.Text}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }

            // A form of error checking make sure we actually read back the right thing
            _ = ReadMacrosFromCardAsync();
        }

        private async Task PrintCardContentsAsync()
        {
            if (_awaitingResponse)
            {
                Console.WriteLine($"ERROR: Attempted to start a SerialCommunication ({nameof(PrintCardContentsAsync)}) job while another was going. Ignoring this request.");
                return;
            }

            _awaitingResponse = true;
            SendToPort($"{LOG}{HEXLIMITER}");
            while (_awaitingResponse)
            {
                // polling at 10x/sec
                await Task.Delay(100);
            }
        }

        private void SendToPort(string msg)
        {
            if (!_port.IsOpen)
            {
                userLog.Text = $"Failed to update, port {PORT_NAME} is closed!";
                return;
            }
            Console.WriteLine($"Writing {msg} to the serial port!");
            _port.WriteLine(msg);
        }

        private void OpenSerialPortConnection()
        {
            // Make sure you configure the microcontroller to also use this
            // serial port name (I think idk I'm new at this)
            _port = new SerialPort(PORT_NAME, 9600);
            _port.DataReceived += OnSerialPortDataReceived;

            try
            {
                _port.Open();
                this.FormClosing += CloseSerialPortConnection;
                Console.WriteLine($"Opened port {PORT_NAME}");
                userLog.Text = "Connected to MacroPad";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            string line = _port.ReadLine();
            if (_awaitingResponse)
            {
                if (line.StartsWith(HEXLIMITER))
                {
                    int closeIndex = line.LastIndexOf(HEXLIMITER);
                    _lastResponse = line.Substring(HEXLIMITER.Length, closeIndex - HEXLIMITER.Length);
                    Console.WriteLine($"Response Detected: {_lastResponse}");
                    _awaitingResponse = false;
                }
            }

            Console.WriteLine($"board: {line}");
        }

        private void CloseSerialPortConnection(object sender, FormClosingEventArgs e)
        {
            _port.Close();
            Console.WriteLine($"Closed port {PORT_NAME}");
        }

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
    }
}
