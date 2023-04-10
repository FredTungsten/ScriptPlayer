namespace RexLabsWifiShock
{
    public interface IComm {

        /// <summary>
        /// Sets up a connection to the Device or returns with an exception
        /// </summary>
        void Connect();

        /// <summary>
        /// Queries the connected status of the comm connection
        /// </summary>
        /// <returns>true if a successful connection has been set up</returns>
        bool IsConnected();

        /// <summary>
        /// Closes the connection to the device
        /// </summary>
        void Close();

        /// <summary>
        /// Reads byte(s) from the Socket
        /// </summary>
        /// <param name="timeout">How long in ms to wait until we give up on recieving a byte</param>
        /// <param name="buffer">The buffer to read the bytes into, length determines how many bytes are read</param>
        /// <returns>The byte that was read</returns>
        void ReadBytes(byte[] buffer,long timeout);

        /// <summary>
        /// Reads byte(s) from the Socket
        /// </summary>
        /// <returns>The byte that was read</returns>
        void ReadBytes(byte[] buffer);


        /// <summary>
        /// Writes a number of bytes into the socket
        /// </summary>
        /// <param name="buffer">The data to be send, the length of the buffer will be sent</param>
        void WriteBytes(byte[] buffer);

        /// <summary>
        /// Returns the name of the connector the device is communicated through
        /// </summary>
        /// <returns></returns>
        string GetConnectorName();
    }

}