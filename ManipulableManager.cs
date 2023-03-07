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
        public Dictionary<string, bool> dictionaryOfElement3D;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(List<string> tagsList, List<GameObject> listOfElement3D)
        {
            tags = tagsList;
            dictionaryOfElement3D = CreateDictionary(listOfElement3D);
        }

        public Dictionary<string, bool> CreateDictionary(List<GameObject> listOfElement3D)
        {
            Dictionary<string, bool> dictionaryOfElement3D = new Dictionary<string, bool>();
            foreach (var obj in listOfElement3D)
            {
                dictionaryOfElement3D.Add(obj.name, false);
            }
            return dictionaryOfElement3D;
        }

        public string ManipulationTrigger(GameObject obj, FSMState currentState, Dictionary<string, Vector3> startingCoord, Vector3 releasedCoord)
        {
            firedTransition = null;
            foreach (var transition in currentState.ListOfTransitions)
            {
                foreach(var action in transition.ActionsOnTransition)
                {
                    switch (action.FsmAction)
                    {
                        case "MoveElement3DDown":
                            //da gestire i vari casi (attenzione al flip che fa in 0, dove passa a negativo)
                            if (obj.name == action.Target && (startingCoord[obj.name].y-releasedCoord.y > 0.1))
                            {
                                firedTransition = transition.Name;
                            }
                            break;
                        case "MoveElement3DUp":
                            if (obj.name == action.Target && (releasedCoord.y-startingCoord[obj.name].y > 0.1))
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

        public void ManipulableAction(FSMAction action, FSMTransition transition, GameObject obj, Dictionary<string, Vector3> startingCoord, Vector3 releasedCoord)
        {
            var t = action.Target.Split(':');
            var tar = t[0];
            var excluded = t.Length > 1 ? t[1] : null;

            //PASSO 1: DISTINGUO IL CASO IN BASE AL TARGET
            if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
            {
                switch (action.FsmAction)
                {
                    case "MoveElement3DUp":
                        if (obj.name == action.Target && (releasedCoord.y - startingCoord[obj.name].y > 0.1)) //se si verifica la condizione di grab detta dall'azione posso fare un check se triggerarla
                        {
                            TargetSetting(transition, action, obj, tar, excluded); //SETTA TRUE sull'azione triggerata
                        }
                        break;
                    case "MoveElement3DDown":
                        if (obj.name == action.Target && (startingCoord[obj.name].y - releasedCoord.y > 0.1))
                        {
                            TargetSetting(transition, action, obj, tar, excluded); //SETTA TRUE sull'azione triggerata
                        }
                        break;
                    default:
                        break;

                }
                
            }
            else //se il target è un oggetto qualsiasi
            {
                switch (action.FsmAction)
                {
                    case "MoveElement3DUp":
                        if (obj.name == action.Target && (releasedCoord.y - startingCoord[obj.name].y > 0.1))
                        {
                            action.Triggered = true;
                        }
                        break;
                    case "MoveElement3DDown":
                        if (obj.name == action.Target && (startingCoord[obj.name].y - releasedCoord.y > 0.1))
                        {
                            action.Triggered = true;
                        }
                        break;
                    default:
                        break;

                }
            }
        }

        public void TargetSetting(FSMTransition transition, FSMAction action, GameObject obj, string target, string excluded)
        {
            string objId = obj.name;

            dictionaryOfElement3D[objId] = true; //diventa vero poiché la condizione dell'azione è stata verificata
            
            switch (target)
            {
                case "ALL": //in questo caso quando devo aver toccato tutti gli oggetti

                    allCondition = true;

                    if (excluded != null && !tags.Contains(excluded))
                    {
                        dictionaryOfElement3D[excluded] = true; //vera su tutti gli oggetti esclusi (default)
                    }
                    else if (tags.Contains(excluded))
                    {
                        foreach (var ex in GameObject.FindGameObjectsWithTag(excluded))
                        {
                            dictionaryOfElement3D[ex.name] = true;
                        }
                    }
                    foreach (var dictionaryObj in dictionaryOfElement3D.ToList()) //scorro tutti gli oggetti manipolabili
                    {
                        if (!dictionaryObj.Value == true | !allCondition) //condizione per verificare se tutti gli oggetti siano stati toccati
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

                    foreach (var dictionaryObj in dictionaryOfElement3D.ToList()) //scorro tutti i bottoni
                    {
                        if (dictionaryObj.Value == true && !objId.Equals(excluded) && !obj.tag.Equals(excluded)) //entro se pigio un bottone qualsiasi che però sia diverso da quello escluso
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
        public void ResetDictionary(Dictionary<string, bool> dictionaryOfElement3D)
        {
            foreach (var obj in dictionaryOfElement3D.ToList()) //scorro tutti i bottoni
            {
                dictionaryOfElement3D[obj.Key] = false;
            }

        }

        public void Reset()
        {
            ResetDictionary(dictionaryOfElement3D);
        }
    }
}

