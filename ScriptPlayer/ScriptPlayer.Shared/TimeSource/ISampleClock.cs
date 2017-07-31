using System;

namespace ScriptPlayer.Shared
{
    public interface ISampleClock
    {
        event EventHandler Tick;
    }
}
