using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static AB.Model.FSM.FSMModel;
using AB.Controller.Instatiator;

namespace AB.Manager.Prova
{
    public class Prova1 : MonoBehaviour
    {
        // Start is called before the first frame update
        public static Prova1 Instance { get; private set; } = new Prova1();

        private bool allCondition = true;

        public Dictionary<GameObject, bool> dictionaryOfButtons = new Dictionary<GameObject, bool>();
        List<string> tags = new List<string>();
        List<GameObject> listOfButtons = new List<GameObject>();

        public InstanceController instatiator;

        void Start()
        {
        }

        public void Initialize(List<string> tagsList, List<GameObject> listOfButtons)
        {
            tags = tagsList;
            dictionaryOfButtons = GetButtons(listOfButtons);
            this.listOfButtons = listOfButtons;
        }

        public Dictionary<GameObject, bool> GetButtons(List<GameObject> listOfButtons) //restituisce la lista di tutti i bottoni in scena
        {
            Dictionary<GameObject, bool> buttons = new Dictionary<GameObject, bool>();
            foreach (var obj in listOfButtons)
            {
                buttons.Add(obj, false);
            }
            return buttons;
        }



        public void ButtonAction(FSMAction action, FSMTransition transition, GameObject obj)
        {
            //chiamo il metodo passandogli l'azione, la transizione, l'oggetto target
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

            //PASSO 2: HO ACTION TRIGGERED SETTATO. VEDO SE SONO TUTTE TRIGGERED SU ACTIONMANAGER
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
            dictionaryOfButtons[obj] = true; //diventa vero quando pigio un bottone

            switch (target)
            {
                case "ALL": //in questo caso quando clicco devo aver cliccato tutti i bottoni

                    allCondition = true;

                    if(excluded != null) //ho qualcosa di escluso
                    {
                        if (tags.Contains(excluded)) //ciò che è escluso è un gruppo di tag
                        {
                            foreach (var ex in GameObject.FindGameObjectsWithTag(excluded)) //scorro tutti i gameobject con quel tag
                            {
                                dictionaryOfButtons[ex] = true;
                            }
                        }
                        else //l'escluso è un gameobject
                        {
                            foreach (var but in listOfButtons)
                            {
                                if (but.name.Equals(excluded))
                                {
                                    dictionaryOfButtons[but] = true;
                                }
                            }
                        }
                    }
                    foreach (var button in dictionaryOfButtons.ToList()) //scorro tutti i bottoni
                    {
                        if (!button.Value == true | !allCondition) //condizione per verificare se tutti i bottoni siano stati pigiati
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

                    foreach (var button in dictionaryOfButtons.ToList()) //scorro tutti i bottoni
                    {
                        if (button.Value == true && !obj.name.Equals(excluded) && !obj.CompareTag(excluded)) //entro se pigio un bottone qualsiasi che però sia diverso da quello escluso
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
                    
                    //provo altro caso 
                    allCondition = true;
                    foreach(var but in listOfButtons)
                    {
                        if (but.name.Equals(excluded))
                        {
                            dictionaryOfButtons[but] = true;
                        }
                    }
                    foreach(var but in dictionaryOfButtons)
                    {
                        if (but.Key.CompareTag(target) && but.Value == false) //l'oggetto ha il tag del target ed il suo valore è true (è stato toccato o è excluded)
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
            ResetButtons(dictionaryOfButtons);
        }

        public void ResetButtons(Dictionary<GameObject, bool> buttons)
        {
            foreach (var button in buttons.ToList()) //scorro tutti i bottoni
            {
                buttons[button.Key] = false;
            }

        }







    }
}


