using AB.Controller.Instatiator;
using AB.Manager.FSM;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AB.Model.FSM.FSMModel;

namespace AB.Controller.Hand
{
    public class HandController : MonoBehaviour
    {
        public static HandController Instance { get; private set; }

        
        private bool allCondition = true;
        List<string> tags;
        public List<GameObject> listOfManipulable = new List<GameObject>();
        public Dictionary<GameObject, bool> dictionaryOfManipulable;

        public InstanceController instatiator;
        public FSMManager fsm;

        public Action<GameObject> Col;
        public delegate void OtherObjectChangedEventHandler(GameObject obj); //define a delegate
        public static event OtherObjectChangedEventHandler HandTrigger; //define an event based on that delegate
        public bool isModified = false;

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            instatiator = InstanceController.Instance;
            fsm = FSMManager.Instance;
        }
        public void Initialize(List<string> tagsList, List<GameObject> listOfManipulable)
        {
            tags = tagsList;
            dictionaryOfManipulable = GetManipulable(listOfManipulable);
            this.listOfManipulable = listOfManipulable;
        }

        public Dictionary<GameObject,bool> GetManipulable(List<GameObject> listOfManipulable)
        {
            Dictionary<GameObject, bool> manipulable = new Dictionary<GameObject, bool>();
            foreach (var obj in listOfManipulable)
            {
                manipulable.Add(obj, false);
            }
            return manipulable;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (isModified)
            {

                isModified = false;
                return;
            }

            HandTrigger?.Invoke(other.gameObject); //this invokes our event

        }

        public void OnTriggerExit(Collider other)
        {
            //HandTrigger?.Invoke(other.gameObject); //this invokes our event

        }


        public void ResetManipulable(Dictionary<GameObject, bool> dictionaryOfManipulable)
        {
            foreach (var obj in dictionaryOfManipulable.ToList()) //scorro tutti i bottoni
            {
                dictionaryOfManipulable[obj.Key] = false;
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
            dictionaryOfManipulable[obj] = true; //diventa vero quando tocco un oggetto

            switch (target)
            {
                case "ALL": //in questo caso quando devo aver toccato tutti gli oggetti

                    allCondition = true;

                    if(excluded != null)
                    {
                        if (tags.Contains(excluded)) //ciò che è escluso è un gruppo di tag
                        {
                            foreach (var ex in GameObject.FindGameObjectsWithTag(excluded)) //scorro tutti i gameobject con quel tag
                            {
                                dictionaryOfManipulable[ex] = true;
                            }
                        }
                        else //l'escluso è un gameobject
                        {
                            foreach (var manipulableObj in listOfManipulable)
                            {
                                if (manipulableObj.name.Equals(excluded))
                                {
                                    dictionaryOfManipulable[manipulableObj] = true;
                                }

                            }
                        }
                    }
                    
                    foreach (var dictionaryObj in dictionaryOfManipulable.ToList()) //scorro tutti i bottoni
                    {
                        if (dictionaryObj.Key.activeSelf) //check se sia attivo o meno
                        {
                            if (!dictionaryObj.Value == true | !allCondition) //condizione per verificare se tutti i bottoni siano stati pigiati
                            {
                                allCondition = false;
                                break; //appena allCondition diventa falsa non devo più andare avanti
                            }
                        }
                    }
                    if (allCondition && !obj.tag.Equals(excluded))
                    {
                        action.Triggered = true;
                    }
                    break;

                case "ANY": //qui quando clicco un bottone qualsiasi (ANY/But3), ANY/Modificabile

                    allCondition = false;

                    foreach (var dictionaryObj in dictionaryOfManipulable.ToList()) //scorro tutti i bottoni
                    {
                        if (dictionaryObj.Value == true && !obj.name.Equals(excluded) && !obj.tag.Equals(excluded)) //entro se pigio un bottone qualsiasi che però sia diverso da quello escluso
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
                    allCondition = true;
                    foreach (var manipulableObj in listOfManipulable)
                    {
                        if (manipulableObj.name.Equals(excluded) || manipulableObj.activeSelf == false)
                        {
                            dictionaryOfManipulable[manipulableObj] = true;
                        }
                    }
                    foreach (var dictionaryObj in dictionaryOfManipulable)
                    {
                        if (dictionaryObj.Key.CompareTag(target) && dictionaryObj.Value == false) //l'oggetto ha il tag del target ed il suo valore è true (è stato toccato o è excluded)
                        {
                            allCondition = false;
                            break;
                        }
                    }
                    if (allCondition)
                    {
                        action.Triggered = true;
                    }
                    break;
            }


        }

        public void Reset()
        {
            ResetManipulable(dictionaryOfManipulable);
        }
    }
}

