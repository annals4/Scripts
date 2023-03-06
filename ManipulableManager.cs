using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static AB.Model.FSM.FSMModel;

namespace AB.Manager.Manipulable
{
    public class ManipulableManager : MonoBehaviour
    {
        public static ManipulableManager Instance { get; private set; } 

        private bool allCondition = true;

        private string firedTransition = null;

        List<string> tags;
        public Dictionary<string, bool> manipublableObjDictionary;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(List<string> tagsList, List<GameObject> manipulableObjList)
        {
            tags = tagsList;
            manipublableObjDictionary = GetManipulable(manipulableObjList);
        }

        public Dictionary<string, bool> GetManipulable(List<GameObject> manipulableObjList)
        {
            Dictionary<string, bool> manipulableObjDictionary = new Dictionary<string, bool>();
            foreach (var obj in manipulableObjList)
            {
                manipulableObjDictionary.Add(obj.name, false);
            }
            return manipulableObjDictionary;
        }

        public string ManipulationTrigger(GameObject obj, FSMState currentState, Dictionary<string, Vector3> objCoordBeforeGrabbing, Vector3 rPoint)
        {
            firedTransition = null;
            foreach (var transition in currentState.ListOfTransitions)
            {
                foreach(var action in transition.ActionsOnTransition)
                {
                    switch (action.fsmAction)
                    {
                        case "MoveSolidDown":
                            //da gestire i vari casi (attenzione al flip che fa in 0, dove passa a negativo)
                            if (obj.name == action.Target && (objCoordBeforeGrabbing[obj.name].y-rPoint.y > 0.1))
                            {
                                firedTransition = transition.Name;
                            }
                            break;
                        case "MoveSolidUp":
                            if (obj.name == action.Target && (rPoint.y-objCoordBeforeGrabbing[obj.name].y > 0.1))
                            {
                                firedTransition = transition.Name;
                            }
                            break;
                        default:
                            break;
                    }
                }

            }
            return firedTransition;
        }

        public void ManipulableAction(FSMAction action, FSMTransition transition, GameObject obj, Dictionary<string, Vector3> startGrab, Vector3 rPoint)
        {
            var t = action.Target.Split(':');
            var tar = t[0];
            var excluded = t.Length > 1 ? t[1] : null;

            //PASSO 1: DISTINGUO IL CASO IN BASE AL TARGET
            if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
            {
                switch (action.fsmAction)
                {
                    case "MoveSolidUp":
                        if (obj.name == action.Target && (rPoint.y - startGrab[obj.name].y > 0.1)) //se si verifica la condizione di grab detta dall'azione posso fare un check se triggerarla
                        {
                            TargetSetting(transition, action, obj, tar, excluded, startGrab, rPoint); //SETTA TRUE sull'azione triggerata
                        }
                        break;
                    case "MoveSolidDown":
                        if (obj.name == action.Target && (startGrab[obj.name].y - rPoint.y > 0.1))
                        {
                            TargetSetting(transition, action, obj, tar, excluded, startGrab, rPoint); //SETTA TRUE sull'azione triggerata
                        }
                        break;
                    default:
                        break;

                }
                
            }
            else //se il target è un oggetto qualsiasi
            {
                switch (action.fsmAction)
                {
                    case "MoveSolidUp":
                        if (obj.name == action.Target && (rPoint.y - startGrab[obj.name].y > 0.1))
                        {
                            action.Triggered = true;
                        }
                        break;
                    case "MoveSolidDown":
                        if (obj.name == action.Target && (startGrab[obj.name].y - rPoint.y > 0.1))
                        {
                            action.Triggered = true;
                        }
                        break;
                    default:
                        break;

                }
            }
        }

        public void TargetSetting(FSMTransition transition, FSMAction action, GameObject obj, string target, string excluded, Dictionary<string, Vector3> startGrab, Vector3 rPoint)
        {
            string objId = obj.name;

            manipublableObjDictionary[objId] = true; //diventa vero poiché la condizione dell'azione è stata verificata
            
            switch (target)
            {
                case "ALL": //in questo caso quando devo aver toccato tutti gli oggetti

                    allCondition = true;

                    if (excluded != null && !tags.Contains(excluded))
                    {
                        manipublableObjDictionary[excluded] = true; //vera su tutti gli oggetti esclusi (default)
                    }
                    else if (tags.Contains(excluded))
                    {
                        foreach (var ex in GameObject.FindGameObjectsWithTag(excluded))
                        {
                            manipublableObjDictionary[ex.name] = true;
                        }
                    }
                    foreach (var objm in manipublableObjDictionary.ToList()) //scorro tutti gli oggetti manipolabili
                    {
                        if (!objm.Value == true | !allCondition) //condizione per verificare se tutti gli oggetti siano stati toccati
                        {
                            allCondition = false;
                            break; //appena allCondition diventa falsa non devo più andare avanti
                        }

                    }
                    if (allCondition && !obj.CompareTag(excluded))
                    {
                        action.Triggered = true;
                    }
                    break;
                case "ANY": //qui quando clicco un bottone qualsiasi (ANY/But3), ANY/Modificabile

                    allCondition = false;

                    foreach (var man in manipublableObjDictionary.ToList()) //scorro tutti i bottoni
                    {
                        if (man.Value == true && !objId.Equals(excluded) && !obj.tag.Equals(excluded)) //entro se pigio un bottone qualsiasi che però sia diverso da quello escluso
                        {
                            allCondition = true;
                            break;
                        }

                    }
                    if (allCondition)
                    {
                        action.Triggered = true;
                    }
                    break;
                default: //NB: non ha senso escludere un tag da un altro tag (quindi tipo TAG1/TAG2) poiché i tag non si racchiudono
                    if (obj.tag.Equals(target) && !objId.Equals(excluded)) //se il tag dell'oggetto cliccato è quello desiderato e l'oggetto non è escluso
                    {
                        action.Triggered = true;
                    }
                    break;
            }


        }
        public void ResetManipulable(Dictionary<string, bool> manipul)
        {
            foreach (var obj in manipul.ToList()) //scorro tutti i bottoni
            {
                manipul[obj.Key] = false;
            }

        }

        public void Reset()
        {
            ResetManipulable(manipublableObjDictionary);
        }
    }
}

