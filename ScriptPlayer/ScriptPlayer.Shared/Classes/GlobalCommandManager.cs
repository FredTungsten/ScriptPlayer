using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ScriptPlayer.Shared
{
    public static class GlobalCommandManager
    {
        public const string GlobalShortcutSuffix = " @";

        public static event EventHandler<PreviewKeyEventArgs> PreviewKeyReceived;

        private static bool OnPreviewKeyReceived(Key key, ModifierKeys modifiers)
        {
            var e = new PreviewKeyEventArgs
            {
                Key = key,
                Modifiers = modifiers
            };

            PreviewKeyReceived?.Invoke(null, e);
            return e.CancelProcessing;
        }

        static GlobalCommandManager()
        {
            CommandMappings = new List<InputMapping>();
            Commands = new Dictionary<string, ScriptplayerCommand>();
        }

        public static bool IsEnabled { get; set; } = true;

        public static List<InputMapping> CommandMappings { get; set; }

        public static Dictionary<string, ScriptplayerCommand> Commands { get; set; }

        public static void RegisterCommand(ScriptplayerCommand command)
        {
            Commands.Add(command.CommandId, command);
        }

        public static bool LoadMappings(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                using (FileStream stream = new FileStream(path, FileMode.Open))
                    LoadMappings(stream);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }

        private static void LoadMappings(FileStream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<InputMapping>));

            CommandMappings = serializer.Deserialize(stream) as List<InputMapping>;
        }

        public static void SaveMappings(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
                SaveMappings(stream);
        }

        private static void SaveMappings(FileStream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<InputMapping>));
            serializer.Serialize(stream, CommandMappings);
        }

        public static void BuildDefaultShortcuts()
        {
            CommandMappings = GetDefaultCommandMappings();
        }

        public static List<InputMapping> GetDefaultCommandMappings()
        {
            List<InputMapping> mappings = new List<InputMapping>();

            foreach (ScriptplayerCommand command in Commands.Values)
            {
                if (command.DefaultShortCuts == null || command.DefaultShortCuts.Count == 0)
                    continue;

                foreach (string shortcut in command.DefaultShortCuts)
                {
                    string keyboardShortcut = shortcut;
                    bool isGlobal = false;

                    if (keyboardShortcut.EndsWith(GlobalShortcutSuffix))
                    {
                        keyboardShortcut =
                            keyboardShortcut.Substring(0, keyboardShortcut.Length - GlobalShortcutSuffix.Length);
                        isGlobal = true;
                    }

                    mappings.Add(new InputMapping
                    {
                        CommandId = command.CommandId,
                        KeyboardShortcut = keyboardShortcut,
                        IsGlobal = isGlobal
                    });
                }
            }

            return mappings;
        }

        public static bool ProcessInput(Key key, ModifierKeys modifiers, KeySource source)
        {
            if (OnPreviewKeyReceived(key, modifiers))
                return true;

            bool isGlobal = source == KeySource.Global;
            string shortcut = GetShortcut(key, modifiers);

            return ProcessInput(shortcut, isGlobal);
        }

        public static bool ProcessInput(MouseWheelEventArgs args)
        {
            string shortCut = GetShortcut(args);
            return ProcessInput(shortCut, false);
        }

        public static bool ProcessInput(MouseButtonEventArgs args)
        {
            string shortCut = GetShortcut(args);
            return ProcessInput(shortCut, false);
        }

        private static bool ProcessInput(string shortcut, bool isGlobal)
        {
            if (!IsEnabled)
                return false;

            InputMapping mapping = CommandMappings.FirstOrDefault(
                c => c.KeyboardShortcut == shortcut
                     && c.IsGlobal == isGlobal);

            if (mapping == null)
                return false;

            Debug.WriteLine($"Processing '{shortcut}' (global = {isGlobal}) => {mapping.CommandId}");

            if (!Commands.ContainsKey(mapping.CommandId))
                return false;

            ScriptplayerCommand command = Commands[mapping.CommandId];
            if (!command.CanExecute(null))
                return false;

            command.Execute(null);
            return true;
        }

        public static string GetShortcut(Key key, ModifierKeys modifiers, bool global = false)
        {
            string shortcut = key.ToString();

            if (modifiers.HasFlag(ModifierKeys.Control))
                shortcut += " + Ctrl";

            if (modifiers.HasFlag(ModifierKeys.Shift))
                shortcut += " + Shift";

            if (modifiers.HasFlag(ModifierKeys.Alt))
                shortcut += " + Alt";

            if (global)
                shortcut += GlobalShortcutSuffix;

            return shortcut;
        }

        public static ModifierKeys GetActiveModifierKeys()
        {
            var activeMods = ModifierKeys.None;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                activeMods |= ModifierKeys.Alt;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                activeMods |= ModifierKeys.Shift;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                activeMods |= ModifierKeys.Control;

            return activeMods;
        }

        public static string GetShortcut(MouseWheelEventArgs args)
        {
            return "Mouse Wheel " + (args.Delta > 0 ? "Up" : "Down");
        }

        public static string GetShortcut(MouseButtonEventArgs args)
        {
            return "Mouse Click " + args.ChangedButton;
        }
    }
}