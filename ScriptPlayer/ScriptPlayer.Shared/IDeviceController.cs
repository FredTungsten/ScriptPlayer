using System.Reflection;
using System.Text;

namespace ScriptPlayer.Shared
{
    public interface IDeviceController
    {
        void Set(DeviceCommandInformation information);
    }
}
