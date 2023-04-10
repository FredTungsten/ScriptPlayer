using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RexLabsWifiShock {

    /// Implementation of the MK312 commands, basically just read and write byte
    public class Commands {

        private Protocol prot = null; // The protocol to communicate with the device

        public Commands(Protocol prot, IComm comm) {
            this.prot = prot;
            prot.setComm(comm);
        }

        /// <summary>
        /// Returns the protocol implementation
        /// </summary>
        /// <returns></returns>
        public Protocol getProtocol()
        {
            return prot;
        }

        /// <summary>
        /// Returns the name of the connector of the comm interface
        /// </summary>
        /// <returns></returns>
        public string getConnectorName()
        {
            return prot.getConnectorName();
        }

        /// Reads a memory address in the devices memory
        public byte peek(uint address) {
            byte[] sendCommand = new byte[4];
            sendCommand[0] = 0x3c; // The read byte command
            sendCommand[1] = (byte) (address >> 8);  // Upper part of the address
            sendCommand[2] = (byte) (address & 0xff); // Lower part of the address
            sendCommand[3] = 0x00; // Will be replaced by checksum

            byte[] reply = new byte[3];

            prot.sendCommand(sendCommand); // Sends the command
            prot.recieveReply(reply); // Waits for the reply and checks the checksum

            // Check if we got the correct reply byte
            if (reply[0] != 0x22) throw new InvalidDataException("Unexpected reply when peeking 0x22 expected, but got " + reply[0]);

            return reply[1]; // Byte one contains the byte read
        }

        /// Writes bytes into the devices Memory
        public void poke(uint address, byte[] buffer) {
            if (buffer.Length > 16) throw new Exception("Too many bytes. Maximum number of 16 allowed");
            byte[] sendCommand = new byte[4 + buffer.Length]; // 1 command, 2 address, 1 checksum + number of bytes to send

            byte len = (byte)(sendCommand.Length-1);
            len = (byte)(len << 4);

            sendCommand[0] = (byte) (0x0d + len); // The write byte command + number of bytes to write
            sendCommand[1] = (byte) (address >> 8);  // Upper part of the address
            sendCommand[2] = (byte) (address & 0xff); // Lower part of the address
            for (int i = 0; i < buffer.Length; i++)
            {
                sendCommand[3+i] = buffer[i];
            }

            prot.sendCommand(sendCommand);

            byte rep = prot.recieveSingleByteReply();

            if (rep != 0x06) throw new InvalidDataException("Invalid reply from device, expected 0x06 reply got "+rep);
        }

        /// Writes a byte into the devices memory
        public void poke(uint address, byte b) {
            byte[] buf = new byte[1];
            buf[0] = b;

            poke(address, buf);
        }

        public void connect() {
            prot.connect();
        }

        public virtual void disconnect() {
            prot.disconnect();
        }   
    }

}
