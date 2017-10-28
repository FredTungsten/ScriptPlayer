using System;
using System.Windows;

namespace ScriptPlayer.ViewModels
{
    public class MessageBoxEventArgs : EventArgs
    {
        public string Text { get; set; }
        public string Title { get; set; }
        public MessageBoxImage Icon { get; set; }
        public MessageBoxButton Buttons { get; set; }
        public MessageBoxResult Result { get; set; }
        public bool Handled { get; set; }
    }
}