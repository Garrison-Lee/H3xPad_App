using System;
using System.IO.Ports;
using System.Management;

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

        public static void ListAllCOMPorts()
        {
            Console.WriteLine("\n===== LISTING COM PORTS =====");
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\WMI",
                    "SELECT * FROM MSSerial_PortName"
                );
                
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("MSSerial_PortName instance");
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("InstanceName: {0}", queryObj["InstanceName"]);

                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("MSSerial_PortName instance");
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("PortName: {0}", queryObj["PortName"]);

                    //If the serial port's instance name contains USB 
                    //it must be a USB to serial device
                    if (queryObj["InstanceName"].ToString().Contains("USB"))
                    {
                        Console.WriteLine(queryObj["PortName"] + " is a USB to SERIAL adapter / converter");
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }

            Console.WriteLine("===== LISTED COM PORTS =====\n");
        }
    }
}
