using Newtonsoft.Json.Serialization;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public enum HsspState
    {
        /// <summary>
        /// The device need to synchronize with the server. Only returned from devices with firmware version <= 3.1.x
        /// </summary>
        NeedSync = 1,

        /// <summary>
        /// No script have yet been setup on the device.
        /// </summary>
        NeedSetup = 2,

        /// <summary>
        /// The script execution is stopped.
        /// </summary>
        Stopped = 3,

        /// <summary>
        /// The device is executing the script.
        /// </summary>
        Playing = 4,
    }
}