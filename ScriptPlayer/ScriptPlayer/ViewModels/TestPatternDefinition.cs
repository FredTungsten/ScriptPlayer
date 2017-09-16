using System;

namespace ScriptPlayer.ViewModels
{
    public class TestPatternDefinition
    {
        public string Name { get; set; }
        public TimeSpan Duration { get; set; }
        public byte[] Positions { get; set; }
    }
}