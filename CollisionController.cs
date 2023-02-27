using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AB.FSMModel.FSM;

namespace AB.CollisionController
{
    public class CollisionController : MonoBehaviour
    {
        public static CollisionController Instance { get; private set; } = new CollisionController();

        
        private bool allCondition = true;
        List<string> tags;
        public Dictionary<string, bool> manipulableObjects;

        public Instatiator.Instatiator instatiator;
        public FSMManager.FSMManager fsm;

        public Action<GameObject> Col;
        public delegate void OtherObjectChangedEventHandler(GameObject obj); //define a delegate
        public static event OtherObjectChangedEventHandler HandTrigger; //define an event based on that delegate


        // Start is called before the first frame update
        void Start()
        {
            instatiator = Instatiator.Instatiator.Instance;
            fsm = FSMManager.FSMManager.Instance;
        }
        public void Initialize(List<string> tagsList, List<GameObject> manObj)
        {
            tags = tagsList;
            manipulableObjects = GetManipulable(manObj);
        }

        public Dictionary<string,bool> GetManipulable(List<GameObject> man)
        {
            Dictionary<string, bool> ob = new Dictionary<string, bool>();
            foreach (var obj in man)
            {
                ob.Add(obj.name, false);
            }
            return ob;
        }

        public void OnTriggerEnter(Collider other)
         {
            HandTrigger?.Invoke(other.gameObject); //this invokes our event

        }
        /// <summary>
        /// Il target può essere:
        /// - un oggetto qualsiasi
        /// - le parole chiave ANY/ALL
        /// - un gruppo di oggetti identificato con il tag
        /// - ALL/tag meno un oggetto specifico o un tag specifico
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="action"></param>
        /// <param name="objId"></param>
        public void TargetSetting(FSMTransition transition, FSMAction action, GameObject obj, string target, string excluded)
        {
            string objId = obj.name;

            manipulableObjects[objId] = true; //diventa vero quando tocco un oggetto

            switch (target)
            {
                case "ALL": //in questo caso quando devo aver toccato tutti gli oggetti

                    allCondition = true;

                    if (excluded != null && !tags.Contains(excluded))
                    {
                        manipulableObjects[excluded] = true;
                    }
                    else if (tags.Contains(excluded))
                    {
                        foreach (var ex in GameObject.FindGameObjectsWithTag(excluded))
                        {
                            manipulableObjects[ex.name] = true;
                        }
                    }
                    foreach (var objm in manipulableObjects.ToList()) //scorro tutti gli oggetti manipolabili
                    {
                        if (!objm.Value == true | !allCondition) //condizione per verificare se tutti gli oggetti siano stati toccati
                        {
                            allCondition = false;
                            break; //appena allCondition diventa falsa non devo più andare avanti
                        }

                    }
                    if (allCondition && !obj.tag.Equals(excluded))
                    {
                        action.Triggered = true;
                    }
                    break;
                case "ANY": //qui quando clicco un bottone qualsiasi (ANY/But3), ANY/Modificabile

                    allCondition = false;

                    foreach (var man in manipulableObjects.ToList()) //scorro tutti i bottoni
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

        public void CollisionAction(FSMAction action, FSMTransition transition, GameObject obj)
        {
            var t = action.Target.Split(':');
            var tar = t[0];
            var excluded = t.Length > 1 ? t[1] : null;

            //PASSO 1: DISTINGUO IL CASO IN BASE AL TARGET
            if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
            {
                TargetSetting(transition, action, obj, tar, excluded); //SETTA TRUE sull'azione triggerata
            }
            else //se il target è un oggetto qualsiasi
            {
                if (obj.name == action.Target) //entro nell'if solo se l'oggetto chiamante la funzione è il target dell'azione voluta
                {
                    action.Triggered = true;
                }
            }
        }

        public void Reset()
        {
            ResetManipulable(manipulableObjects);
        }
    }
}

