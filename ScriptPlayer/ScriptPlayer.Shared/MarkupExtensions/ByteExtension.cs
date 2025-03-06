using System;
using System.Windows.Markup;

namespace ScriptPlayer.Shared
{
    public class ByteExtension : MarkupExtension
    {
        public ByteExtension()
        { }

        public ByteExtension(byte value)
        {
            Value = value;
        }

        public byte Value { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Value;
        }
    }
}
