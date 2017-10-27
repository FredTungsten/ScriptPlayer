using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class RawScriptAction : ScriptAction
    {
        [JsonProperty(PropertyName = "pos")]
        public byte Position { get; set; }

        [JsonProperty(PropertyName = "spd")]
        public byte Speed { get; set; }

        [JsonProperty(PropertyName = "at")]
        public long TimeStampWrapper
        {
            get => TimeStamp.Ticks / TimeSpan.TicksPerMillisecond;
            set => TimeStamp = TimeSpan.FromTicks(value * TimeSpan.TicksPerMillisecond);
        }

        private string DebuggerDisplay => $"{TimeStamp.TotalSeconds:f2} - P:{Position} S:{Speed}";
        public override bool IsSameAction(ScriptAction action)
        {
            if (action is RawScriptAction raw)
            {
                if (Position != raw.Position) return false;
                return Speed == raw.Speed;
            }
            return false;
        }
    }
}