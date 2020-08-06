using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.Devices.Interfaces
{
    public interface IDevice
    {
        bool IsEnabled { get; set; }
       
        string Name { get; set; }
    }

    public interface IActionBasedDevice : IDevice
    {
        void SetMinCommandDelay(TimeSpan settingsCommandDelay);
        void Enqueue(DeviceCommandInformation information);
        Task Set(IntermediateCommandInformation intermediateInfo);
        void Stop();
    }

    public interface ISyncBasedDevice : IDevice
    {
        void SetScript(string scriptTitle, IEnumerable<FunScriptAction> actions);
        void SetScriptOffset(TimeSpan offset);
        void Resync(TimeSpan time);
        void Play(bool playing, TimeSpan progress);
    }
}
