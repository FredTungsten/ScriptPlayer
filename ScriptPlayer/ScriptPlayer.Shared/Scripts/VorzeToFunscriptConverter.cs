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
                // removed because vorze rotation speed changes while still rotating in the same direction are an essential feature
                /*int samePosition = 0;

                for (int j = i + 1; j < actions.Count; j++)
                {
                    if (actions[j].Action == actions[i].Action)
                        samePosition++;
                    else
                        break;
                }*/

                // This has been modified to make Vorze CSVs round-trip better through the conversions, but other toys should still get something out of this too
                funActions.Add(new FunScriptAction
                {
                    Position = (byte)(50 + actions[i].Parameter * (actions[i].Action == 0 ? 0.5 : -0.5)),
                    TimeStamp = actions[i].TimeStamp
                });
            }

            return funActions;
        }
    }
}