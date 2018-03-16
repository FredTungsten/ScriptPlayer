using System.Collections.Generic;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public static class VorzeToFunscriptConverter
    {
        public static List<FunScriptAction> Convert(List<VorzeScriptAction> actions)
        {
            actions = actions.OrderBy(a => a.TimeStamp).ToList();

            List<VorzeScriptAction> filteredActions = new List<VorzeScriptAction>();

            foreach (VorzeScriptAction action in actions)
            {
                if (filteredActions.Count == 0)
                    filteredActions.Add(action);
                else if (filteredActions.Last().TimeStamp >= action.TimeStamp)
                    continue;
                else
                    filteredActions.Add(action);
            }

            actions = filteredActions;

            List<FunScriptAction> funActions = new List<FunScriptAction>();

            for (int i = 0; i < actions.Count; i++)
            {
                int samePosition = 0;

                for (int j = i + 1; j < actions.Count; j++)
                {
                    if (actions[j].Action == actions[i].Action)
                        samePosition++;
                    else
                        break;
                }

                funActions.Add(new FunScriptAction
                {
                    Position = (byte)(actions[i].Action == 0 ? 95 : 5),
                    TimeStamp = actions[i + samePosition].TimeStamp
                });
            }

            return funActions;
        }
    }
}