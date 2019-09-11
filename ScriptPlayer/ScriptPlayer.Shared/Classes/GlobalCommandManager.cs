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
        static GlobalCommandManager()
        {
            CommandMappings = new List<InputMapping>();
            Commands = new Dictionary<string, ScriptplayerCommand>();
        }

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
                if (string.IsNullOrWhiteSpace(command.DefaultShortCut))
                    continue;

                mappings.Add(new InputMapping
                {
                    CommandId = command.CommandId,
                    KeyboardShortcut = command.DefaultShortCut
                });
            }

            return mappings;
        }

        public static bool ProcessInput(Key key, ModifierKeys modifiers, KeySource source)
        {
            string shortcut = GetShortcut(key, modifiers);

            InputMapping mapping = CommandMappings.FirstOrDefault(c => c.KeyboardShortcut == shortcut);
            if (mapping == null)
                return false;

            if (!Commands.ContainsKey(mapping.CommandId))
                return false;

            ScriptplayerCommand command = Commands[mapping.CommandId];
            if (!command.CanExecute(null))
                return false;

            command.Execute(null);
            return true;
        }

        public static string GetShortcut(Key key, ModifierKeys modifiers)
        {
            string shortcut = key.ToString();

            if (modifiers.HasFlag(ModifierKeys.Control))
                shortcut += " + Ctrl";

            if (modifiers.HasFlag(ModifierKeys.Shift))
                shortcut += " + Shift";

            if (modifiers.HasFlag(ModifierKeys.Alt))
                shortcut += " + Alt";

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
    }

    public enum KeySource
    {
        DirectInput,
        WndProc,
        Global
    }
}