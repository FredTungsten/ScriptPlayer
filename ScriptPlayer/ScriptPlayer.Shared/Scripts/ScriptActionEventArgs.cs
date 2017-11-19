using System;

namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptActionEventArgs : EventArgs
    {
        public ScriptAction RawPreviousAction;
        public ScriptAction RawCurrentAction;
        public ScriptAction RawNextAction;

        public ScriptActionEventArgs(ScriptAction previous, ScriptAction current, ScriptAction next)
        {
            RawPreviousAction = previous;
            RawCurrentAction = current;
            RawNextAction = next;
        }

        public ScriptActionEventArgs(ScriptAction current)
        {
            RawPreviousAction = null;
            RawCurrentAction = current;
            RawNextAction = null;
        }

        public ScriptActionEventArgs<T> Cast<T>() where T : ScriptAction
        {
            return new ScriptActionEventArgs<T>(RawPreviousAction as T, RawCurrentAction as T, RawNextAction as T);
        }
    }

    public class ScriptActionEventArgs<T> : ScriptActionEventArgs where T : ScriptAction
    {
        public T PreviousAction => (T)RawPreviousAction;
        public T CurrentAction => (T)RawCurrentAction;
        public T NextAction => (T)RawNextAction;

        public ScriptActionEventArgs(T previous, T current, T next) : base(previous, current, next)
        { }

        public ScriptActionEventArgs(T current) : this(null, current, null) { }
    }

    public class IntermediateScriptActionEventArgs<T> : IntermediateScriptActionEventArgs where T : ScriptAction
    {
        public T PreviousAction => (T)RawPreviousAction;
        public T NextAction => (T)RawNextAction;

        public IntermediateScriptActionEventArgs(T previous, T next, double progress) : base(previous, next, progress)
        { }
    }
}