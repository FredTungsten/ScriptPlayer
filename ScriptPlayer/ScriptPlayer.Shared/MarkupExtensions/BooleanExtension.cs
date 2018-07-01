using System;
using System.Windows.Markup;

namespace ScriptPlayer.Shared
{
    public class BooleanExtension : MarkupExtension
    {
        public BooleanExtension()
        { }

        public BooleanExtension(string value)
        {
            Value = bool.Parse(value);
        }

        public bool Value { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Value;
        }
    }
}
