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

    public class RequestEventArgs<TIn, TOut> : EventArgs
    {
        public bool Handled { get; set; }
        public TIn ValueIn { get; set; }
        public TOut ValueOut { get; set; }

        public RequestEventArgs()
        {
            Handled = false;
        }

        public RequestEventArgs(TIn initialValueIn) : this()
        {
            ValueIn = initialValueIn;
        }

        public RequestEventArgs(TIn initialValueIn, TOut initialValueOut) : this(initialValueIn)
        {
            ValueOut = initialValueOut;
        }
    }
}