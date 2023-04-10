using RexLabsWifiShock;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MK312WifiLibDotNet
{

    /// <summary>
    /// Connection adapter for the classic rs232 Interface
    /// </summary>
    public class SerialComm : IComm
    {

        public String connectionString = null;   // The String used to open the RS232 interface

        private SerialPort serialPort = null;    // The Serial port that will be used for the communication

        /// <summary>
        /// Returns a list of available port names
        /// </summary>
        /// <returns></returns>
        public static String[] getAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Creates an instance of the SerialComm component
        /// </summary>
        /// <param name="connectionString">The connection String eg. COM1 or /dev/USBttsy0</param>
        public SerialComm(String connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Connect()
        {
            if (connectionString == null) throw new Exception("Please use setPort() before calling Connect.");
            serialPort = new SerialPort(connectionString, 19200, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            serialPort.Open();
        }

        /// <summary>
        /// Closes the Serial port
        /// </summary>
        public void Close()
        {
            serialPort.Close();
            serialPort = null;
        }

        public string GetConnectorName()
        {
            return "Serial " + connectionString;
        }

        /// <summary>
        /// Returns true, if the port has been connected
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return serialPort != null;
        }

        /// <summary>
        /// Reads bytes from the serial buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="timeout"></param>
        public void ReadBytes(byte[] buffer, long timeout)
        {
            long timeout_at = System.Environment.TickCount + timeout; // We wait a maximum of one
            while (serialPort.BytesToRead < buffer.Length)
            {
                //Console.WriteLine(deviceSocket.Available + " "  + buffer.Length);
                if (System.Environment.TickCount > timeout_at) throw new TimeoutException("Timeout waiting for reply from Socket (" + timeout + "ms have passed)");
                Thread.Sleep(1);
            }
            int bytesRec1 = serialPort.Read(buffer, 0, buffer.Length);
            if (bytesRec1 != buffer.Length)
                throw new IOException("Not exactly one byte was returned from readbyte");
        }

        /// <summary>
        /// Reads bytes into the buffer, and throws an error if more than a second passes
        /// </summary>
        /// <param name="buffer"></param>
        public void ReadBytes(byte[] buffer)
        {
            ReadBytes(buffer, 1000);
        }

        /// <summary>
        /// Writes the passed buffer into the serial port
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteBytes(byte[] buffer)
        {
            serialPort.Write(buffer, 0, buffer.Length);
        }
    }
}
