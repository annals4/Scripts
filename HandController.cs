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
        public List<GameObject> listOfElement3D = new List<GameObject>();
        public Dictionary<GameObject, bool> dictionaryOfElement3D;

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
        public void Initialize(List<string> tagsList, List<GameObject> listOfElement3D)
        {
            tags = tagsList;
            dictionaryOfElement3D = CreateDictionary(listOfElement3D);
            this.listOfElement3D = listOfElement3D;
        }

        public Dictionary<GameObject,bool> CreateDictionary(List<GameObject> listOfElement3D)
        {
            Dictionary<GameObject, bool> element3D = new Dictionary<GameObject, bool>();
            foreach (var obj in listOfElement3D)
            {
                element3D.Add(obj, false);
            }
            return element3D;
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


        public void ResetDictionary(Dictionary<GameObject, bool> dictionaryOfElement3D)
        {
            foreach (var obj in dictionaryOfElement3D.ToList()) //scorro tutti i bottoni
            {
                dictionaryOfElement3D[obj.Key] = false;
            }

        }

        public void CollisionAction(FSMInput action, FSMTransition transition, GameObject obj)
        {
            var t = action.InputTarget.Split(':');
            var tar = t[0];
            var excluded = t.Length > 1 ? t[1] : null;

            //PASSO 1: DISTINGUO IL CASO IN BASE AL TARGET
            if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
            {
                TargetSetting(transition, action, obj, tar, excluded); //SETTA TRUE sull'azione triggerata
            }
            else //se il target è un oggetto qualsiasi
            {
                if (obj.name == action.InputTarget) //entro nell'if solo se l'oggetto chiamante la funzione è il target dell'azione voluta
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
        public void TargetSetting(FSMTransition transition, FSMInput action, GameObject obj, string target, string excluded)
        {
            dictionaryOfElement3D[obj] = true; //diventa vero quando tocco un oggetto

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
                                dictionaryOfElement3D[ex] = true;
                            }
                        }
                        else //l'escluso è un gameobject
                        {
                            foreach (var element3D in listOfElement3D)
                            {
                                if (element3D.name.Equals(excluded))
                                {
                                    dictionaryOfElement3D[element3D] = true;
                                }

                            }
                        }
                    }
                    
                    foreach (var dictionaryObj in dictionaryOfElement3D.ToList()) //scorro tutti i bottoni
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

                    foreach (var dictionaryObj in dictionaryOfElement3D.ToList()) //scorro tutti i bottoni
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
                    foreach (var element3D in listOfElement3D)
                    {
                        if (element3D.name.Equals(excluded) || element3D.activeSelf == false)
                        {
                            dictionaryOfElement3D[element3D] = true;
                        }
                    }
                    foreach (var dictionaryObj in dictionaryOfElement3D)
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
            ResetDictionary(dictionaryOfElement3D);
        }
    }
}

