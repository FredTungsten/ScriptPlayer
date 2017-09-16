using System;

namespace ScriptPlayer.ViewModels
{
    public class ButtplugUrlRequestEventArgs : EventArgs
    {
        public string Url { get; set; }
        public bool Handled { get; set; }
    }
}