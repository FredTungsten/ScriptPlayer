using System;
using System.Windows.Markup;

namespace ScriptPlayer.VideoSync.Extensions
{
    public sealed class Int32Extension : MarkupExtension
    {
        public Int32Extension(int value) { Value = value; }
        public int Value { get; set; }
        public override Object ProvideValue(IServiceProvider sp) { return Value; }
    };

    public sealed class DoubleExtension : MarkupExtension
    {
        public DoubleExtension(double value) { Value = value; }
        public double Value { get; set; }
        public override Object ProvideValue(IServiceProvider sp) { return Value; }
    };
}
