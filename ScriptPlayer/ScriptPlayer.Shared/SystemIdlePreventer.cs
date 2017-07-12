using System.Runtime.InteropServices;

namespace ScriptPlayer.Shared
{
    public static class SystemIdlePreventer
    {
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        public static void Prevent(bool prevent)
        {
            if(prevent)
                SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            else
                SetThreadExecutionState(ES_CONTINUOUS);
        }
    }
}
