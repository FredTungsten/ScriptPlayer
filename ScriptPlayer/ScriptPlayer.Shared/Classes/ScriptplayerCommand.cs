using System;

namespace ScriptPlayer.Shared
{
    public class ScriptplayerCommand : RelayCommand
    {
        public string CommandId { get; set; }
        public string DisplayText { get; set; }
        public string DefaultShortCut { get; set; }

        public ScriptplayerCommand(Action execute, Func<bool> canExecute) : base(execute, canExecute)
        {
        }

        public ScriptplayerCommand(Action execute) : base(execute)
        {
        }
    }
}