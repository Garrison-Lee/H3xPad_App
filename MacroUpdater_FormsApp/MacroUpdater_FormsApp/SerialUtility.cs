using System;
using System.IO.Ports;

namespace MacroUpdater_FormsApp
{
    public static class SerialUtility
    {
        public static void LogAllAvailablePorts()
        {
            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }
        }

        public static void PokeAllAvailablePorts()
        {
            string log = "";
            string[] portNames = SerialPort.GetPortNames();
            for (int i = 0; i < portNames.Length; i++)
            {
                SerialPort curPort = new SerialPort(portNames[i]);
                log += $"\nPoking {portNames[i]}... isOpen? {curPort.IsOpen}";
            }
            Console.WriteLine(log);
        }
    }
}
