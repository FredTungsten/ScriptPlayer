using System;

namespace ScriptPlayer.ViewModels
{
    public class RequestFileEventArgs : EventArgs
    {
        public bool SaveFile { get; set; }
        public string Filter { get; set; }
        public int FilterIndex { get; set; }
        public bool Handled { get; set; }
        public bool MultiSelect { get; set; }
        public string SelectedFile { get; set; }
        public string[] SelectedFiles { get; set; }
    }
}