using System;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GetParameterAttribute : Attribute
    {
        public GetParameterAttribute(string name)
        {
            ParameterName = name;
        }

        public GetParameterAttribute()
        { }

        public string ParameterName { get; set; }

    }
}
