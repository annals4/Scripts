using UnityEngine;
using static AB.Model.FSM.FSMModel;

namespace AB.Trigger.Temporal
{
    public class TemporalTrigger : MonoBehaviour
    {
        public static TemporalTrigger Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public (string trans, float trashold, bool isPres)  Info(FSMState state)
        {
            string trans = null;
            var isPres = false; //true se nella transizione è presente un trigger temporale
            var trashold = float.MaxValue;
            foreach (var transition in state.ListOfTransitions)
            {
                if (transition.SettingType == "TemporalTrigger") //se nello stato considerato è presente un trigger temporale
                {
                    trans = transition.Name;
                    isPres = true;
                    if (float.TryParse(transition.ExternalCondition, out float j)) //in externalCondition sarà scritto il trashold temporale a seguito del quale la transizione verrà triggerata
                    {
                        trashold = j;
                    }
                    break;
                }
            }
            return (trans, trashold, isPres);
        }
       

    }
}

