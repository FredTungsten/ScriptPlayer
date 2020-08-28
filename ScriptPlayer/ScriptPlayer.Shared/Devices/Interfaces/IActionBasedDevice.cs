using System;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Interfaces
{
    public interface IActionBasedDevice : IDevice
    {
        void SetMinCommandDelay(TimeSpan settingsCommandDelay);
        void Enqueue(DeviceCommandInformation information);
        Task Set(IntermediateCommandInformation intermediateInfo);
        void Stop();
    }
}