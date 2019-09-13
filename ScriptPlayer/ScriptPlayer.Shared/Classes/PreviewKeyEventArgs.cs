using System;
using System.Windows.Input;

namespace ScriptPlayer.Shared
{
    public class PreviewKeyEventArgs : EventArgs
    {
        public bool CancelProcessing { get; set; }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }

        public string Shortcut => GlobalCommandManager.GetShortcut(Key, Modifiers);

        public PreviewKeyEventArgs()
        {
            CancelProcessing = false;
        }
    }
}