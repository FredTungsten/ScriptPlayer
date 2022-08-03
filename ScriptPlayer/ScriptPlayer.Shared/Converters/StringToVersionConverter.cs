using System;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Converters
{
    public class StringToVersionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Version version = (Version) value;
            writer.WriteValue($"{version.Major}.{version.Minor}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                if(reader?.Value != null)
                    if(Version.TryParse(reader.Value.ToString(), out Version version))
                        return version;
            }
            catch
            {
                //
            }

            return new Version(1, 0);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Version);
        }
    }
}