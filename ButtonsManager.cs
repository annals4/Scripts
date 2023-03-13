using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AB.Model.FSM;
using static AB.Model.FSM.FSMModel;
using static AB.Manager.FSM.FSMManager;

namespace AB.Manager.Button
{
    public class ButtonsManager : MonoBehaviour
    {
        public static ButtonsManager Instance { get; private set; }

        private bool andCondition = true;
        private bool allCondition = true;

        private string firedTransition = null;

        public Dictionary<string, bool> buttons;
        List<string> tags;

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Initialize(FSMModel fsm, List<string> tagsList)
        {
            buttons = GetButtons(fsm.ListOfObjects);
            tags = tagsList;
        }

        

        public string ButtonTrigger(GameObject obj, FSMState currentState)
        {
            firedTransition = null;
            foreach (var transition in currentState.ListOfTransitions)
            {
                //ConflictAnalyser(transition);
                SettingType setting;
                if (Enum.TryParse(transition.SettingType, out setting))
                {
                    ActionSetting(setting, transition, obj);

                }

            }
            return firedTransition;
        }

        public void ActionSetting(SettingType setting, FSMTransition transition, GameObject obj)
        {
            string objId = obj.name;
            switch (setting)
            {
                case SettingType.AND:///////////tutte le azioni di una transizione devono aver avuto luogo

                    andCondition = true;
                    foreach (var action in transition.TransitionInput) //ciclo per settare se un azione è stata 'triggered'
                    {
                        var t = action.InputTarget.Split(':');
                        var tar = t[0];
                        var excluded = t.Length > 1 ? t[1] : null;
                        if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
                        {
                            TargetSetting(transition, action, obj, tar, excluded);
                        }
                        else //se il target è un oggetto qualsiasi
                        {
                            if (objId == action.InputTarget) //entro nell'if solo se l'oggetto chiamante la funzione è il target dell'azione voluta
                            {
                                action.Triggered = true;
                            }
                        }
                    }
                    foreach (var action in transition.TransitionInput) //ciclo per verificare che tutte le azioni siano state triggerate
                    {
                        if (!action.Triggered) //condizione per verificare che tutte le azioni della transizione siano state eseguite
                        {
                            andCondition = false;
                            break;
                        }
                    }
                    if (andCondition)
                    {
                        //this.fire
                        firedTransition = transition.Name;
                        UnTrigger(transition);
                        ResetButtons(buttons);
                        
                    }

                    break;
                case SettingType.OR: ////////////solo un'azione qualsiasi della transizione deve essere stata fatta
                    foreach (var action in transition.TransitionInput)
                    {
                        var t = action.InputTarget.Split(':');
                        var tar = t[0];
                        var excluded = t.Length > 1 ? t[1] : null;
                        if (tar.Equals("ALL") | tar.Equals("ANY") | tags.Contains(tar))//se il Target è una parola speciale
                        {
                            TargetSetting(transition, action, obj, tar, excluded);
                        }

                        if (objId == action.InputTarget)
                        {
                            action.Triggered = true;
                            break;
                        }
                    }
                    foreach (var action in transition.TransitionInput)
                    {
                        if (action.Triggered)//me ne basta una
                        {
                            //this.machine.Fire(transition.Name);
                            firedTransition = transition.Name;
                            UnTrigger(transition);
                            ResetButtons(buttons);  
                            break;
                        }
                    }
                    break;
                default:
                    break;

            }

        }

        

        public void ResetButtons(Dictionary<string, bool> buttons)
        {
            foreach (var button in buttons.ToList()) //scorro tutti i bottoni
            {
                buttons[button.Key] = false;
            }

        }



        public Dictionary<string, bool> GetButtons(List<FSMObj> objList) //restituisce la lista di tutti i bottoni in scena
        {
            Dictionary<string, bool> buttons = new Dictionary<string, bool>();
            foreach (var obj in objList)
            {
                if (obj.Type.Equals("ButtonObject"))
                {
                    buttons.Add(obj.ObjectName, false);
                }

            }
            return buttons;
        }

        ///////////////////////////////////////PARTE NUOVA******************************************

        public void ButtonAction(FSMInput action, FSMTransition transition, GameObject obj)
        {
            //chiamo il metodo passandogli l'azione, la transizione, l'oggetto target
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
        public void TargetSetting(FSMTransition transition, FSMInput action, GameObject obj, string target, string excluded)
        {
            string objId = obj.name;

            buttons[objId] = true; //diventa vero quando pigio un bottone

            switch (target)
            {
                case "ALL": //in questo caso quando clicco devo aver cliccato tutti i bottoni

                    allCondition = true;

                    if (excluded != null && !tags.Contains(excluded))
                    {
                        buttons[excluded] = true;
                    }
                    else if (tags.Contains(excluded))
                    {
                        foreach (var ex in GameObject.FindGameObjectsWithTag(excluded))
                        {
                            buttons[ex.name] = true;
                        }
                    }
                    foreach (var button in buttons.ToList()) //scorro tutti i bottoni
                    {
                        if (!button.Value == true | !allCondition) //condizione per verificare se tutti i bottoni siano stati pigiati
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

                    foreach (var button in buttons.ToList()) //scorro tutti i bottoni
                    {
                        if (button.Value == true && !objId.Equals(excluded) && !obj.tag.Equals(excluded)) //entro se pigio un bottone qualsiasi che però sia diverso da quello escluso
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


        /// <summary>
        /// provo il default: so che i bottoni con un certo tag devono essere tutti true, quindi
        /// allCondition = false;
        /// if(excluded!= null){ //caso excluded
        /// buttons[excluded] = true;
        /// }
        /// foreach (var button in buttons.ToList()){
        /// if(
        /// }
        /// if (obj.tag.Equals(target) && !objId.Equals(excluded)){
        /// button
        /// }
        /// </summary>

        public void Reset()
        {
            ResetButtons(buttons);
        }
    }
}


