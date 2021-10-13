using System;
using System.Collections.Generic;
using System.Reflection;

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

    public abstract class ScriptPlayerAction
    {
        public abstract string Name { get; }

        public abstract ActionResult Execute(string[] parameters);
    }
}