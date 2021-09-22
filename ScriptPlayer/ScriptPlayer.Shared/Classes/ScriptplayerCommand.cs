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

    public class ActionResult
    {
        public ActionResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ScriptPlayerAction
    {
        public delegate ActionResult ActionDelegate(string[] parameters);

        public string Name { get; }

        private ActionDelegate _action;

        public ScriptPlayerAction(string name, ActionDelegate action)
        {
            Name = name;
            _action = action;
        }

        public void Execute(string[] parameters, out ActionResult result)
        {
            result = _action.Invoke(parameters);
        }
    }
}