using System;

namespace ScriptPlayer.ViewModels
{
    public class RequestEventArgs<T> : EventArgs
    {
        public bool Handled { get; set; }
        public T Value { get; set; }

        public RequestEventArgs()
        {
            Handled = false;
        }
        public RequestEventArgs(T initialValue) : this()
        {
            Value = initialValue;
        }
    }
}