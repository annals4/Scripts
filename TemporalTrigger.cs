using AB.Interactor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AB.AnyAllModel.FSM;

namespace AB.TempTrigger
{
    public class TemporalTrigger : MonoBehaviour
    {
        public static TemporalTrigger Instance { get; private set; } = new TemporalTrigger();
        
        public (string trans, float trashold, bool isPres)  Info(FSMState state)
        {
            string trans = null;
            var isPres = false; //true se nella transizione � presente un trigger temporale
            var trashold = float.MaxValue;
            foreach (var transition in state.ListOfTransitions)
            {
                if (transition.SettingType == "TemporalTrigger") //se nello stato considerato � presente un trigger temporale
                {
                    trans = transition.Name;
                    isPres = true;
                    if (float.TryParse(transition.ExternalCondition, out float j)) //in externalCondition sar� scritto il trashold temporale a seguito del quale la transizione verr� triggerata
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
