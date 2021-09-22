using System.IO;
using System.Text;

namespace ScriptPlayer.Ipc
{
    // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
    // Defines the data protocol for reading and writing strings on our stream
    public class StreamString
    {
        private readonly Stream _ioStream;
        private readonly Encoding _streamEncoding;

        public StreamString(Stream ioStream)
        {
            this._ioStream = ioStream;
            _streamEncoding = Encoding.UTF8;
        }

        public string ReadString()
        {
            int b1 = _ioStream.ReadByte();
            int b2 = _ioStream.ReadByte();

            if (b1 < 0 || b2 < 0)
                return null;

            int len = b1 * 256 + b2;
            
            byte[] inBuffer = new byte[len];
            _ioStream.Read(inBuffer, 0, len);

            return _streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = _streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > ushort.MaxValue)
            {
                len = ushort.MaxValue;
            }
            _ioStream.WriteByte((byte)(len / 256));
            _ioStream.WriteByte((byte)(len & 255));
            _ioStream.Write(outBuffer, 0, len);
            _ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
