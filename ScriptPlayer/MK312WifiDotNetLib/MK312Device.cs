using MK312WifiLibDotNet;
using System;
using System.Text;
using System.Threading;

namespace RexLabsWifiShock
{

    /// Implementation of the MK312 commands, basically just read and write byte
    public class MK312Device {

        private Commands cmd = null; // The protocol to communicate with the device
        private double _fade = 0.5;
        private Boolean balance = false; // Makes the two channels be inverted to each other

        public MK312Device(Commands cmd) {
            this.cmd = cmd;
        }

        public MK312Device(IComm icomm, bool useEncryption, bool threadsafe) {
            if ((!useEncryption) && (icomm is SerialComm)) throw new Exception("Non encrypted comm is not supported by RS232 Connections");
            Protocol prot = new Protocol(useEncryption);
            if (!threadsafe)
                cmd = new Commands(prot,icomm);
            else
                cmd = new ThreadSafeCommands(prot,icomm);
        }

        /// <summary>
        /// Returns the name of the connector the protocol communicates through
        /// </summary>
        /// <returns></returns>
        public string getConnectorName()
        {
            return cmd.getConnectorName();
        }


        // Establishes a connection to the device
        public void connect() {
            cmd.connect();
        }

        // Closes the connnection to the device
        public void disconnect() {
            cmd.disconnect();
        }

        /// Writes the devices current mode
        public void setMode(MK312Constants.Mode mode) {
            cmd.poke((uint)MK312Constants.RAM.CurrentMode, (byte)mode);
        }

        /// Reads the devices current mode
        public MK312Constants.Mode readMode() {
            return (MK312Constants.Mode)Enum.ToObject(typeof(MK312Constants.Mode),cmd.peek((uint)MK312Constants.RAM.CurrentMode));
        }

        /// Writes the passed string onto the MK312 display
        public void writeToDisplay(String text) {
            if (text.Length > 8) throw new Exception("Text is too big!");
            cmd.poke((uint)MK312Constants.RAM.WriteLCDParameter, (byte)0x64);
            cmd.poke((uint)MK312Constants.RAM.BoxCommand1, (byte)MK312Constants.BoxCommand.LCDWriteString);
            while (cmd.peek((uint)MK312Constants.RAM.BoxCommand1) != (byte)MK312Constants.BoxCommand.None) Thread.Sleep(10); // Wait for confirmation
            byte[] wbuf = new byte[2];
            char[] ctext = text.ToCharArray();
            for (int i = 0; i < ctext.Length; i++)
            {
                wbuf[0] = (byte)ctext[i];
                wbuf[1] = (byte)(8+i);
                cmd.poke((uint)MK312Constants.RAM.WriteLCDParameter, wbuf);
                cmd.poke((uint)MK312Constants.RAM.BoxCommand1, (byte)MK312Constants.BoxCommand.LCDWriteCharacter);
                while (cmd.peek((uint)MK312Constants.RAM.BoxCommand1) != (byte)MK312Constants.BoxCommand.None) Thread.Sleep(10); // Wait for confirmation
            }

        }


        // Returns the MK312 Firmware Version information
        public String getMK312Version() {
            byte v1 = cmd.peek((uint)MK312Constants.Flash.VersionMajor);
            byte v2 = cmd.peek((uint)MK312Constants.Flash.VersionMinor);
            byte v3 = cmd.peek((uint)MK312Constants.Flash.VersionInternal);
            byte box = cmd.peek((uint)MK312Constants.Flash.BoxModel);

            StringBuilder db = new StringBuilder();
            db.Append("Box:");
            db.Append(box.ToString("X2"));

            db.Append(" Version:");
            db.Append(v1.ToString("X2"));
            db.Append(v2.ToString("X2"));
            db.Append(v3.ToString("X2"));

            return db.ToString();
        }

        // Enables and disables the ADC
        public void enableADC(bool adc) {
            byte b = 0;
            if (adc) b = 1;
            cmd.poke(0x400f,b); // Disable ADC
        }

        public void setEncryptionKey(byte key) {
            cmd.poke((uint)MK312Constants.RAM.BoxKey,key);
            cmd.getProtocol().setEncryptionKey(key);
        }

        // Executes a command
        public void execute(MK312Constants.BoxCommand command) {
            cmd.poke((uint)MK312Constants.RAM.BoxCommand1, (byte)command);
        }

        // Initializes channels
        public void initializeChannels() {
            setMode(MK312Constants.Mode.PowerOn);
            cmd.poke((uint)MK312Constants.RAM.ChannelAGateSelect, (byte)MK312Constants.Gate.Off);
            cmd.poke((uint)MK312Constants.RAM.ChannelBGateSelect, (byte)MK312Constants.Gate.Off);
            cmd.poke((uint)MK312Constants.RAM.ChannelAIntensitySelect, (byte)MK312Constants.Select.Static);
            cmd.poke((uint)MK312Constants.RAM.ChannelBIntensitySelect, (byte)MK312Constants.Select.Static);
            cmd.poke((uint)MK312Constants.RAM.ChannelAIntensity, 0);
            cmd.poke((uint)MK312Constants.RAM.ChannelBIntensity, 0);
            cmd.poke((uint)MK312Constants.RAM.ChannelAFrequencySelect, (byte)MK312Constants.Select.Static);
            cmd.poke((uint)MK312Constants.RAM.ChannelBFrequencySelect, (byte)MK312Constants.Select.Static);
            cmd.poke((uint)MK312Constants.RAM.ChannelAFrequency, 30);
            cmd.poke((uint)MK312Constants.RAM.ChannelBFrequency, 15);
            cmd.poke((uint)MK312Constants.RAM.ChannelAWidthSelect, (byte)MK312Constants.Select.Advanced);
            cmd.poke((uint)MK312Constants.RAM.ChannelBWidthSelect, (byte)MK312Constants.Select.Advanced);
        }

        // Sets a value to the channel with adjustment
        private void setChannel(uint channeladdress, double value) {
           double gamma = 1.5;

           double correctedA = 255 * Math.Pow(value / 255, 1 / gamma);
           cmd.poke(channeladdress, (byte)correctedA);          // Channel A: Set intensity value
        }


        // Sets the intensity of the first port (0-1)
        public void setChannelALevel(double a) {
            // Check limits
            if (a < 0) a = 0;
            if (a > 1) a = 1;
            // Do value correction
            double valueA = 115 + (80 * _fade) + ((a * 100) * 64 / 100);

            setChannel((uint)MK312Constants.RAM.ChannelAIntensity,valueA);
        }


        // Sets the intensity of the second port (0-1)
        public void setChannelBLevel(double b) {
            // Check Limits
            if (b < 0) b = 0;
            if (b > 1) b = 1;
            // Do Value correction
            double valueB = 0;
            if (balance)
                valueB = 115 + (80 * _fade) + ((100 - (b * 100)) * 64 / 100);
            else
                valueB = 115 + (80 * _fade) + ((b * 100) * 64 / 100);

            setChannel((uint)MK312Constants.RAM.ChannelBIntensity,valueB);
        }

        // Sets all of the channels to 0
        public void resetChannels() {
            cmd.poke((uint)MK312Constants.RAM.ChannelAIntensity, 0x00);          // Channel A: Set intensity value
            cmd.poke((uint)MK312Constants.RAM.ChannelBIntensity, 0x00);          // Channel B: Set intensity value
        }




    }

}
