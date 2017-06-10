using System;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptAction
    {
        [JsonIgnore]
        public TimeSpan TimeStamp;
    }
}