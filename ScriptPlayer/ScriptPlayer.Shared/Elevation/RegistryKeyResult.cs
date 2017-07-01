using Microsoft.Win32;

namespace ScriptPlayer.Shared.Elevation
{
    public class RegistryKeyResult
    {
        public RegistryKey Key { get; }
        public ResultState State { get; }

        public static readonly RegistryKeyResult DoesntExist = new RegistryKeyResult(null, ResultState.DoesntExist);
        public static readonly RegistryKeyResult AccessDenied = new RegistryKeyResult(null, ResultState.AccessDenied);

        public RegistryKeyResult(RegistryKey key, ResultState state)
        {
            Key = key;
            State = state;
        }

        public enum ResultState
        {
            Success,
            AccessDenied,
            DoesntExist
        }
    }
}