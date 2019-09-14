using System.Windows.Input;
using FMUtils.KeyboardHook;

namespace ScriptPlayer.ViewModels
{
    public struct KeyAndModifiers
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public KeyAndModifiers(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public static KeyAndModifiers FromKeyboardHookEventArgs(KeyboardHookEventArgs eventArgs)
        {
            Key key = KeyInterop.KeyFromVirtualKey((int)eventArgs.Key);
            ModifierKeys modifiers = ModifierKeys.None;

            if (eventArgs.isShiftPressed)
                modifiers |= ModifierKeys.Shift;

            if (eventArgs.isCtrlPressed)
                modifiers |= ModifierKeys.Control;

            if (eventArgs.isAltPressed)
                modifiers |= ModifierKeys.Alt;

            return new KeyAndModifiers(key, modifiers);
        }

        public static KeyAndModifiers FromWndProc(int wParam)
        {
            ModifierKeys modifiers = ModifierKeys.None;

            //0x00010000 = shift
            //0x00020000 = control
            //0x00040000 = alt

            int virtualKeyCode = (int)wParam;

            if ((virtualKeyCode & 0x00010000) > 0)
            {
                modifiers |= ModifierKeys.Shift;
                virtualKeyCode &= ~0x00010000;
            }

            if ((virtualKeyCode & 0x00020000) > 0)
            {
                modifiers |= ModifierKeys.Control;
                virtualKeyCode &= ~0x00020000;
            }

            if ((virtualKeyCode & 0x00040000) > 0)
            {
                modifiers |= ModifierKeys.Alt;
                virtualKeyCode &= ~0x00040000;
            }

            Key key = KeyInterop.KeyFromVirtualKey(virtualKeyCode);

            return new KeyAndModifiers(key, modifiers);
        }
    }
}