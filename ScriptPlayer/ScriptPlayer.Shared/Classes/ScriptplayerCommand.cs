using System;
using System.Collections.Generic;

namespace ScriptPlayer.Shared
{
    public class ScriptplayerCommand : RelayCommand
    {
        public string CommandId { get; set; }
        public string DisplayText { get; set; }
        public List<string> DefaultShortCuts { get; set; } = new List<string>();

        public ScriptplayerCommand(Action execute, Func<bool> canExecute) : base(execute, canExecute)
        {
        }

        public ScriptplayerCommand(Action execute) : base(execute)
        {
        }
    }
}