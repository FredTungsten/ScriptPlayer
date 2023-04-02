using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RexLabsWifiShock
{

    // This implemetnation of the comm class uses wifi to connect to the CLX/Rangarig ESP8266-01 sollution on the MK312
    // An UDP Broadcast is used to determine the ESP Modules IP address, and then the class will keep a connection to that IP address, reconnecting as neccessary

    public class WifiComm:IComm
    {
        public int UDP_DiscoveryPort = 8842; // The port the device listens to UDP requests from
        public int TCP_Port = 8843; // The port to make the actual connection to
        private String MK312IDString = "ICQ-MK312"; // The string to send via UDP

        public bool connected = false;           // True when connected

        private Socket deviceSocket = null;              // The socket connection
        private const int timeout_WaitForUDPReply = 5000;  // How long do we wait after sending the UDP request to get the devices IP Address
        private const int timeout_TotalWaitForIPAddress = 60000; // How long do we wait for an IP Address in total

        private IPAddress ipAddress = null;  // IP Adress of the MK312 device
        private UdpClient udpClient = null; // The UDP Client used to figure out the IP address

        public String GetConnectorName()
        {
            return "" + ipAddress;
        }
        /// <summary>
        /// Waits for the IP Address answer after sending the UDP Request
        /// Then the global IP Address is set after successfully recieving 4 bytes
        /// </summary>
        private void WaitForIp()
        {
            try
            {
                long timeout = System.Environment.TickCount + timeout_WaitForUDPReply;
                var from = new IPEndPoint(0, 0);
                while ((ipAddress == null) && (System.Environment.TickCount < timeout))
                {
                    var recvBuffer = udpClient.Receive(ref from);
                    if (recvBuffer.Length != 4) continue;
                    ipAddress = new IPAddress(recvBuffer);
                }
            }
            catch (Exception al)
            {
            }

        }

        /// <summary>
        /// Determines the IP address of the hardware device by doing an UDP broadcast
        /// </summary>
        /// <returns>The IP Address</returns>
        private IPAddress FetchIPAddress()
        {
            int timeout_at = System.Environment.TickCount + 60000; // The time we wait for an answer before we fail
            ipAddress = null;
            // ipAddress = IPAddress.Parse("192.168.178.59"); <- Cheetah's old wifi firmware can't do autodiscover
            while (ipAddress == null)
            {
                udpClient = new UdpClient();
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_DiscoveryPort));

                Thread thr = new Thread(new ThreadStart(WaitForIp));
                thr.Start();

                var data = Encoding.UTF8.GetBytes(MK312IDString);
                udpClient.Send(data, data.Length, "255.255.255.255", UDP_DiscoveryPort);

                long waituntil = System.Environment.TickCount + timeout_WaitForUDPReply;
                while ((ipAddress == null) && (System.Environment.TickCount < waituntil))
                    Thread.Sleep(500);

                udpClient.Close();
                if (System.Environment.TickCount > timeout_at) 
                    throw new TimeoutException("Timeout while wiating for UDP Answer from device");
            }
            return ipAddress;
        }

        /// <summary>
        /// Reads a single byte from the Socket
        /// </summary>
        /// <param name="timeout">How long in ms to wait until we give up on recieving a byte</param>
       /// <returns>The byte that was read</returns>
        public void ReadBytes(byte[] buffer, long timeout) {
            long timeout_at = System.Environment.TickCount + timeout; // We wait a maximum of one
            while (deviceSocket.Available < buffer.Length) {
                //Console.WriteLine(deviceSocket.Available + " "  + buffer.Length);
                if (System.Environment.TickCount > timeout_at) throw new TimeoutException("Timeout waiting for reply from Socket ("+timeout+"ms have passed)");
                Thread.Sleep(1);
            }
            int bytesRec1 = deviceSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            if (bytesRec1 != buffer.Length)
                throw new IOException("Not exactly one byte was returned from readbyte");

            //printBuffer('<', buffer);

        }

        /// <summary>
        /// Reads a single byte from the Socket with fixed timeout
        /// </summary>
        /// <param name="buffer"></param>
        public void ReadBytes(byte[] buffer)
        {
            ReadBytes(buffer, 1000); // The defaut timeout waiting for an answer
        }


        // Debug helper that prints a buffer to the console
        public static void printBuffer( char prefix , byte[] buffer) {
            Console.Write(prefix);
            for (int i = 0; i < buffer.Length; i++)
            {
                Console.Write("{0:X}", buffer[i]);
            }
            Console.WriteLine();

        }

        /// <summary>
        /// Writes a number of bytes into the socket
        /// </summary>
        /// <param name="buffer">The data to be send, the length of the buffer will be sent</param>
        public void WriteBytes(byte[] buffer) {
            deviceSocket.Send(buffer, buffer.Length, SocketFlags.None);
            //printBuffer('>', buffer);
        }

        /// <summary>
        /// Sets up a connection to the Device or returns with an exception
        /// </summary>
        public void Connect()
        {
            FetchIPAddress(); // Gets the IP Address or fails
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, TCP_Port); // Okay, IP Address aquired

            Console.WriteLine("Connected:" + ipAddress);

            // Create a TCP/IP  socket to attempt connection
            deviceSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            deviceSocket.NoDelay = true;
            deviceSocket.ReceiveTimeout = -1;
            deviceSocket.Connect(remoteEP);

            while (!deviceSocket.Connected) {
                Thread.Sleep(1000);
            }

            connected = true;
        }

        /// <summary>
        /// Queries the connected status of the comm connection
        /// </summary>
        /// <returns>true if a successful connection has been set up</returns>
        public bool IsConnected() {
            return connected;
        }

        /// <summary>
        /// Closes the connection to the device
        /// </summary>
        public void Close() {
            try
            {
                        deviceSocket.Shutdown(SocketShutdown.Both);
            }
            catch (System.Exception)
            {
            }
            try
            {
                        deviceSocket.Close();
            }
            catch (System.Exception)
            {
            }
            connected = false;
        }

    }
}
