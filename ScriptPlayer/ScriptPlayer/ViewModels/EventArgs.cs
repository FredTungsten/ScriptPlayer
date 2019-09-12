using System;

namespace ScriptPlayer.ViewModels
{
    public class EventArgs<T> : EventArgs
    {
        public T Value { get; set; }

        public bool Handled { get; set; }

        public EventArgs()
        {
            Handled = false;
        }

        public EventArgs(T value) : this()
        {
            Value = value;
        }
    }
}