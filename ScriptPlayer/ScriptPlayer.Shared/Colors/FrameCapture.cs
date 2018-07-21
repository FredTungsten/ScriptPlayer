namespace ScriptPlayer.Shared
{
    public struct FrameCapture
    {
        public long FrameIndex;
        public byte[] Capture;
        public float AudioLevel;

        public FrameCapture(long frame, System.Drawing.Color[] samples)
        {
            AudioLevel = 0;

            ulong r = 0, g = 0, b = 0, c = 0;


            FrameIndex = frame;
            Capture = new byte[3];
            for (int i = 0; i < samples.Length; i++)
            {
                r += samples[i].R;
                g += samples[i].G;
                b += samples[i].B;
                c++;
            }

            Capture[0] = (byte)(r / c);
            Capture[1] = (byte)(g / c);
            Capture[2] = (byte)(b / c);
        }
    }
}