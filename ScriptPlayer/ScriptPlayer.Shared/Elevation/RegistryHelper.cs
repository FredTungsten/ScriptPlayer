using System;
using System.Security;
using Microsoft.Win32;

namespace ScriptPlayer.Shared.Elevation
{
    public class RegistryHelper
    {
        private readonly RegistryView _view;

        public RegistryHelper(RegistryView view)
        {
            _view = view;
        }

        public RegistryValue Read(string path, string name)
        {
            RegistryKey key = OpenKey(path);

            RegistryValue result = new RegistryValue(path, name);

            if (key == null)
                return result;

            object value = key.GetValue(name);
            result.Value = value;

            if(value != null)
                result.Type = key.GetValueKind(name);

            return result;
        }

        public bool Exists(string path)
        {
            RegistryKey key = OpenKey(path);
            return key != null;
        }

        public bool Exists(string path, string valueName)
        {
            RegistryKey key = OpenKey(path);
            if (key == null)
                return false;

            return key.GetValue(valueName) != null;
        }

        private RegistryKey OpenKey(string path)
        {
            string[] parts = path.Split('\\');

            RegistryHive hive;

            if (!TryParseHive(parts[0], out hive))
                throw new ArgumentException(parts[0] + " was not recognised as a valid registry hive identifier!");

            RegistryKey hiveRootKey = RegistryKey.OpenBaseKey(hive, _view);
            RegistryKey currentKey = hiveRootKey;

            for (int index = 1; index < parts.Length; index++)
            {
                currentKey = currentKey.OpenSubKey(parts[index]);
                if (currentKey == null)
                    return null;
            }

            return currentKey;
        }

        private bool TryParseHive(string hiveName, out RegistryHive hive)
        {
            switch (hiveName.ToUpper())
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                {
                    hive = RegistryHive.ClassesRoot;
                    break;
                }
                case "HKEY_CURRENT_USER":
                case "HKCU":
                {
                    hive = RegistryHive.CurrentUser;
                    break;
                }
                case "HKEY_LOCAL_MACHINE":
                case "KHLM":
                {
                    hive = RegistryHive.LocalMachine;
                    break;
                }
                case "HKEY_USERS":
                case "HKU":
                {
                    hive = RegistryHive.Users;
                    break;
                }
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                {
                    hive = RegistryHive.CurrentConfig;
                    break;
                }
                default:
                {
                    hive = RegistryHive.LocalMachine;
                    return false;
                }
            }

            return true;
        }

        public RegistryKeyResult.ResultState Write(RegistryValue value)
        {
            RegistryKeyResult key = OpenOrCreate(value.Path);
            if (key.State != RegistryKeyResult.ResultState.Success)
                return key.State;

            key.Key.SetValue(value.Name, value.Value, value.Type);
            return RegistryKeyResult.ResultState.Success;
        }

        private RegistryKeyResult OpenOrCreate(string path)
        {
            try
            {
                string[] parts = path.Split('\\');

                RegistryHive hive;

                if (!TryParseHive(parts[0], out hive))
                    throw new ArgumentException(parts[0] + " was not recognised as a valid registry hive identifier!");

                RegistryKey hiveRootKey = RegistryKey.OpenBaseKey(hive, _view);
                RegistryKey currentKey = hiveRootKey;

                for (int index = 1; index < parts.Length; index++)
                {
                    RegistryKey nextKey = currentKey.OpenSubKey(parts[index], true);
                    if (nextKey == null)
                        nextKey = currentKey.CreateSubKey(parts[index]);

                    if (nextKey == null)
                        return RegistryKeyResult.DoesntExist;

                    currentKey = nextKey;
                }

                return new RegistryKeyResult(currentKey, RegistryKeyResult.ResultState.Success);
            }
            catch (SecurityException)
            {
                return RegistryKeyResult.AccessDenied;
            }
            catch (UnauthorizedAccessException)
            {
                return RegistryKeyResult.AccessDenied;
            }
        }
    }
}