using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AB.AnyAllModel.FSM;
using static AB.FSMManager.FSMManager;


namespace AB.HeadController
{
    public class HeadController : MonoBehaviour
    {
        public static HeadController Instance { get; private set; } = new HeadController();
        MixedRealityPose pose;

        public Action<GameObject> Col;

        private bool andCondition = true;
        private bool allCondition = true;

        private string firedTransition = null;

        List<string> tags;
        public Dictionary<string, bool> triggerObjects;

        public Instatiator.Instatiator instatiator;
        public FSMManager.FSMManager fsm;


        public delegate void HeadTriggerEnterEventHandler(GameObject obj); //define a delegate
        public static event HeadTriggerEnterEventHandler HeadTriggerEnter; //define an event based on that delegate

        public delegate void HeadTriggerExitEventHandler(GameObject obj); //define a delegate
        public static event HeadTriggerExitEventHandler HeadTriggerExit; //define an event based on that delegate


        // Start is called before the first frame update
        void Start()
        {
            instatiator = Instatiator.Instatiator.Instance;
            fsm = FSMManager.FSMManager.Instance;


        }
        public void Initialize(List<string> tagsList, List<GameObject> trig)
        {
            tags = tagsList;
            triggerObjects = GetTriggers(trig);
        }
        // Update is called once per frame
        void Update()
        {
        }

        public Dictionary<string, bool> GetTriggers(List<GameObject> tr)
        {
            Dictionary<string, bool> trigg = new Dictionary<string, bool>();
            foreach (var obj in tr)
            {
                trigg.Add(obj.name, false);
            }
            return trigg;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (HeadTriggerEnter != null) //make sure that there is at least one subscriber
            {
                HeadTriggerEnter(other.gameObject); //this invokes our event
            }

        }

        public void OnTriggerExit(Collider other)
        {
            if (HeadTriggerExit != null) //make sure that there is at least one subscriber
            {
                HeadTriggerExit(other.gameObject); //this invokes our event
            }
        }



        //CASO1: Entro in una zona trigger con la testa e si scatena la transizione (azione: TriggerCollision)
        public void HeadAction(FSMAction action, FSMTransition transition, GameObject obj)
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
            string objId = obj.name;

            triggerObjects[objId] = true; //diventa vero quando tocco un oggetto

            switch (target)
            {
                case "ALL": //in questo caso quando devo aver toccato tutti gli oggetti

                    allCondition = true;

                    if (excluded != null && !tags.Contains(excluded))
                    {
                        triggerObjects[excluded] = true;
                    }
                    else if (tags.Contains(excluded))
                    {
                        foreach (var ex in GameObject.FindGameObjectsWithTag(excluded))
                        {
                           triggerObjects[ex.name] = true;
                        }
                    }
                    foreach (var objm in triggerObjects.ToList()) //scorro tutti gli oggetti manipolabili
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

                    foreach (var man in triggerObjects.ToList()) //scorro tutti i bottoni
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
        

        //CASO2: in cui entro in una zona trigger e si scatena un azione sino a quando non esco (accoppiare le azioni di EnterTrigger con ExitTrigger)
        public void InTrigger(FSMAction action, FSMTransition transition, GameObject obj)
        {
            var t = action.Target.Split(':');
            var tar = t[0];
            var excluded = t.Length > 1 ? t[1] : null;

            //PASSO 1: DISTINGUO IL CASO IN BASE AL TARGET
            //non ha senso entrare in tutte le zone trigger, forse
            if (tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
            {
                InSetting(transition, action, obj, tar, excluded); //SETTA TRUE sull'azione triggerata
            }
            else //se il target è un oggetto qualsiasi
            {
                if (obj.name == action.Target) //entro nell'if se il target dell'azione corrisponde alla zona trigger in cui sono entrato 
                {
                    action.Triggered = true;
                }
            }
        }

        public void InSetting(FSMTransition transition, FSMAction action, GameObject obj, string target, string excluded)
        {
            string objId = obj.name; //Zona trigger in cui sono dentro 

            switch (target)
            {
                case "ANY": //qui quando posso entrare in una zona qualsiasi ANY/Cube1, ANY/TAG0

                    if(!objId.Equals(excluded) && !obj.tag.Equals(excluded)) //se la zona trigger non è tra quelle escluse 
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

        

        public void GlobalReset()
        {
            ResetTriggers(triggerObjects);
        }



        public void ResetTriggers(Dictionary<string, bool> trigg)
        {
            foreach (var obj in trigg.ToList()) //scorro tutti i bottoni
            {
                trigg[obj.Key] = false;
            }

        }
    }
}

