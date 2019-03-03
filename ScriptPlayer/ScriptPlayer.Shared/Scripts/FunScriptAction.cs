using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class FunScriptAction : ScriptAction
    {
        [JsonProperty(PropertyName = "pos")]
        public byte Position { get; set; }

        [DebuggerHidden]
        [JsonProperty(PropertyName = "at")]
        public long TimeStampWrapper {
            get => TimeStamp.Ticks / TimeSpan.TicksPerMillisecond;
            set => TimeStamp = TimeSpan.FromTicks(value * TimeSpan.TicksPerMillisecond);
        }

        private string DebuggerDisplay => $"{TimeStamp.TotalSeconds:f2} - {Position}";

        public override bool IsSameAction(ScriptAction action)
        {
            return (action as FunScriptAction)?.Position == Position;
        }

        public FunScriptAction Duplicate()
        {
            return new FunScriptAction
            {
                Position = Position,
                TimeStamp = TimeStamp,
                OriginalAction = OriginalAction
            };
        }
    }
}