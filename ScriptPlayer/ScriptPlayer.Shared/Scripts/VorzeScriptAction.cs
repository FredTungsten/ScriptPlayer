namespace ScriptPlayer.Shared.Scripts
{
    public class VorzeScriptAction : ScriptAction
    {
        public int Action;
        public int Parameter;
        public override bool IsSameAction(ScriptAction action)
        {
            if (action is VorzeScriptAction vorze)
            {
                if (vorze.Action != Action) return false;
                return vorze.Parameter == Parameter;
            }
            return false;
        }
    }
}