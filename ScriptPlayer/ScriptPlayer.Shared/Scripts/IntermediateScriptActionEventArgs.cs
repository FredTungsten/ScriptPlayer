using System;

namespace ScriptPlayer.Shared.Scripts
{
    public class IntermediateScriptActionEventArgs : EventArgs
    {
        public double Progress;
        public ScriptAction RawPreviousAction;
        public ScriptAction RawNextAction;
        public TimeSpan TimeStamp;

        public TimeSpan SincePrevious => TimeStamp - RawPreviousAction.TimeStamp;
        public TimeSpan ToNext => RawNextAction.TimeStamp - TimeStamp;

        public IntermediateScriptActionEventArgs(ScriptAction previous, ScriptAction next, double progress)
        {
            RawPreviousAction = previous;
            RawNextAction = next;
            Progress = progress;
        }

        public IntermediateScriptActionEventArgs<T> Cast<T>() where T : ScriptAction
        {
            return new IntermediateScriptActionEventArgs<T>(RawPreviousAction as T, RawNextAction as T, Progress);
        }
    }
}