using System;
using System.Collections.Generic;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.Interfaces
{
    public interface ISyncBasedDevice : IDevice
    {
        void SetScript(string scriptTitle, IEnumerable<FunScriptAction> actions);
        void ClearScript();

        bool IsScriptLoaded(string title);
        bool IsScriptLoaded();

        event EventHandler ScriptLoaded;

        void SetScriptOffset(TimeSpan offset);
        void Resync(TimeSpan time);
        void Play(bool playing, TimeSpan progress);
    }
}