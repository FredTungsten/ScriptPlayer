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
            Version version = Version.Parse((string)reader.Value);
            return version;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Version);
        }
    }
}