namespace ScriptPlayer.Shared
{
    public struct FrameCapture
    {
        public long FrameIndex;
        public byte[] Capture;

        public FrameCapture(long frame, System.Drawing.Color[] samples)
        {
            FrameIndex = frame;
            Capture = new byte[samples.Length * 3];
            for (int i = 0; i < samples.Length; i++)
            {
                Capture[3 * i + 0] = samples[i].R;
                Capture[3 * i + 1] = samples[i].G;
                Capture[3 * i + 2] = samples[i].B;
            }
        }
    }
}