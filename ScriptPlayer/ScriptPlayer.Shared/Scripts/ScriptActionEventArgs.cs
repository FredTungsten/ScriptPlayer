namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptActionEventArgs<T> : ScriptActionEventArgs where T : ScriptAction
    {
        public T PreviousAction => (T)RawPreviousAction;
        public T CurrentAction => (T)RawCurrentAction;
        public T NextAction => (T)RawNextAction;

        public ScriptActionEventArgs(T previous, T current, T next) : base(previous, current, next)
        {}

        public ScriptActionEventArgs(T current) : this(null, current, null) { }
    }
}