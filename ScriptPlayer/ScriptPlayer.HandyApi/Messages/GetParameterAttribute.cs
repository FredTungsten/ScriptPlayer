using System;

namespace ScriptPlayer.HandyApi.Messages
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
