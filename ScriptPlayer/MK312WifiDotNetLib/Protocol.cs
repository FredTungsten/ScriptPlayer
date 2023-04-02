using System;
using System.IO;

namespace RexLabsWifiShock
{

    /// Implementation of the MK312 Communication protocol
    public class Protocol {
        private IComm comm = null; // The Communication layer to the device

        private bool encryptionEnabled = true; // Do we use the Encryptionless method (only supported by RexLabs Wifi adapter)
        private byte boxkey = 0; // The key sent by the box
        private const byte hostkey = 0; // The key sent by us, 0 for simplicity
        private const byte extraEncryptKey = 0x55; // The key always added to the encryption

        private byte encryptionKey = 0; // The resulting encryption key if any

        private bool connected = false; // Is set to true, when a handshake was successful

        /// Sets the communication class to use
        public void setComm(IComm comm) {
            this.comm = comm;
        }

        /// <summary>
        /// Returns the name of the connector the protocoll uses for communication
        /// </summary>
        /// <returns></returns>
        public string getConnectorName()
        {
            return this.comm.GetConnectorName();
        }


        /// Instances the Protocol Class
        /// <param name="comm">The communication class to use e.g. WifiComm</param>
        /// <param name="encryptionEnabled">Should we use encryption (if in doubt, yes)</param>
        public Protocol(bool encryptionEnabled) {
            this.encryptionEnabled = encryptionEnabled;
        }

        /// Adds a checksum to the last byte of the buffer containing the previous bytes
        private void addChecksum(byte[] buffer) {
            long checksum = 0;
            for (int i = 0; i < buffer.Length-1; i++)
                checksum += buffer[i];
            buffer[buffer.Length-1] = (byte)(checksum % 256);
        }

        /// Checks if the checksum in the buffer is correct
        private void checkChecksum(byte[] buffer) {
            byte tmpchk = buffer[buffer.Length-1];
            addChecksum(buffer);
            if (tmpchk != buffer[buffer.Length-1]) throw new InvalidDataException("Checksum check failed. Expected " + tmpchk + " recieved " + buffer[buffer.Length-1]);
        }

        // Encrypts the buffer for sending to the device
        private void encrypt(byte[] buffer) {
            if (!encryptionEnabled) return; // There is nothing to do

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] ^= encryptionKey; 
        }

        // Read all incoming bytes so we can have a clean start
        private void flushIncoming() {
            try
            {
                byte[] readBuffer = new Byte[1];
                while (true) comm.ReadBytes(readBuffer);
            }
            catch (TimeoutException) // Once a timeout exception occurs we will know no more bytes are waiting... or something is seriously wrong, but the followupcalls should take care of that.
            {
            }
        }

        /// Attempts the handshake with the MK312 device, and continues, once successfull
        /// <param name="send">The byte to send to the device e.g. 0</param>
        /// <param name="expect">The expected Reply e.g. 7</param>
        private void handshake(byte send, byte expect) {
            int attempts = 12;
            byte[] sendBuffer = new byte[1];
            sendBuffer[0] = send;
            byte[] reply = new byte[1];
            while (attempts > 0) {
                comm.WriteBytes(sendBuffer); // Send a 0 as a hello
                comm.ReadBytes(reply);
                if (reply[0] == expect) break;
                attempts--;
            }
            if (attempts == 0) throw new Exception("Handshake with Device failed");
        }

        /// Does the key handshake with the device
        private void negotiateKeys() {
            if (!encryptionEnabled) {
                byte[] sendBufferNE = {0x2f,0x42,0x42};
                comm.WriteBytes(sendBufferNE);
                byte[] readBufferNE = new byte[1];
                comm.ReadBytes(readBufferNE);
                if (readBufferNE[0] != 0x69) throw new Exception("Failed to establish non encryption mode");
            }
            // Send key negotiation
            byte[] sendBuffer = {0x2f,hostkey,0xff};
            addChecksum(sendBuffer);
            comm.WriteBytes(sendBuffer);

            // Read the systems reply
            byte[] readBuffer = new byte[3];
            comm.ReadBytes(readBuffer);
            checkChecksum(readBuffer);

            // Recieve the key from the box
            boxkey = readBuffer[1];

            // Put all 3 keys together, so encryption can happen
            encryptionKey = (byte)(boxkey ^ hostkey ^ extraEncryptKey);
        }

        /// Sets up communication to the device and does the initial handshake
        public void connect() {
            comm.Connect();
            flushIncoming(); // Look for a clean start
            //if (encryptionEnabled) {
            handshake(0x00,0x07);
            negotiateKeys();
            //} else {
            //    handshake(0x42,0x69); // Request encryptionless protocol
            //}
            connected = true;
        }

        /// Closes the connection to the device
        public void disconnect() {
            comm.Close();
            boxkey = 0;
            connected = false;
        }

        // Returns true if the connection is established and ready to recieve commands
        public bool isConnected() {
            if (!comm.IsConnected()) return false;
            return connected;
        }

        /// Sends a command to the box
        public void sendCommand(byte[] buffer) {
            addChecksum(buffer); // Adds the checksum
            //WifiComm.printBuffer('c', buffer);

            encrypt(buffer); // Encrypts the buffer
            comm.WriteBytes(buffer); // Sends the buffer
        }

        // Waits for a reply from the box
        public void recieveReply(byte[] buffer) {
            comm.ReadBytes(buffer); // Receives the expected number of bytes
            checkChecksum(buffer); // Checks the checksum
        }

        // Waits for a single reply byte, no checksum check
        public byte recieveSingleByteReply() {
            byte[] reply = new byte[1];
            comm.ReadBytes(reply);
            return reply[0];
        }

        /// <summary>
        /// Allows for changing of the <see cref="boxkey"/>
        /// </summary>
        /// <param name="key">The box key</param>
        public void setEncryptionKey(byte key)
        {
            encryptionKey = key;
        }
    }


}