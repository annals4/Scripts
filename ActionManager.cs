using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AB.Manager.FSM.FSMManager; //per Untrigger
using AB.Manager.Button;
using AB.Controller.Hand;
using AB.Manager.Prova;
using static AB.Model.FSM.FSMModel;
using AB.Controller.Head;
using AB.Manager.Manipulable;

namespace AB.Manager.Action
{
    public class ActionManager : MonoBehaviour
    {

        private bool andCondition = true;
        private int ord = 0;

        private string firedTransition = null;

        //public ButtonsManager buttonManager;
        public HandController collisionManager;
        public ManipulableManager manipulableManager;
        public Prova1 prova1;

        public static ActionManager Instance { get; private set; }


        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            //buttonManager = ButtonsManager.Instance;
            manipulableManager = ManipulableManager.Instance;
            collisionManager = HandController.Instance;
            prova1 = Prova1.Instance;
        }


        //**********************************************BUTTONS/HAND/HEAD************************************************
        public string TransitionTrigger(GameObject obj, FSMState currentState, string flag)
        {
            firedTransition = null;
            foreach (var transition in currentState.ListOfTransitions)
            {
                SettingType setting;
                if (Enum.TryParse(transition.SettingType, out setting))
                {
                    InputSetting(setting, transition, obj, flag); //il flag deriva dal metodo che invoca il transitiontrigger

                }

            }
            return firedTransition;
        }

        /// <summary>
        /// AZIONI NON GRAB
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="transition"></param>
        /// <param name="obj"></param>
        public void InputSetting(SettingType setting, FSMTransition transition, GameObject obj, string flag)
        {
            string objId = obj.name;
            switch (setting)
            {
                case SettingType.AND:///////////tutte le azioni di una transizione devono aver avuto luogo

                    andCondition = true;

                    InputTrigger(transition, obj, flag, setting);

                    foreach (var input in transition.TransitionInput) //ciclo per verificare che tutte le azioni siano state triggerate
                    {
                        if (!input.Triggered) //condizione per verificare che tutte le azioni della transizione siano state eseguite
                        {
                            andCondition = false;
                            break;
                        }
                    }
                    if (andCondition)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition); //a tutte le azioni il campio input.trigger viene settato nuovamente a false
                        Reset();
                    }

                    break;

                case SettingType.OR: //solo un'azione qualsiasi della transizione deve essere stata fatta
                    InputTrigger(transition, obj, flag, setting);

                    foreach (var input in transition.TransitionInput)
                    {
                        if (input.Triggered)//me ne basta una
                        {
                            firedTransition = transition.Name;
                            UnTrigger(transition);
                            Reset();
                            break;
                        }
                    }
                    break;
                case SettingType.ORDERED:

                    InputTrigger(transition, obj, flag, setting);
                    
                    if (ord == transition.TransitionInput.Count)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition);
                        Reset();
                        ord = 0;
                        break;
                    }

                    break;
                default:
                    break;

            }

        }




        public void InputTrigger(FSMTransition transition, GameObject obj, string flag, SettingType setting)
        {
            foreach (var input in transition.TransitionInput) //ciclo per settare se un azione è stata 'triggered'
            {
                if (Enum.TryParse(input.FsmInput, out FsmInput fsmInput))
                {
                    switch ((flag, fsmInput)) //collego azione chiamante e azione effettuata
                    {
                        case ("Button", FsmInput.ButtonClick): //ho chiamato il metodo cliccando un bottone
                                                                //ButtonsManager.Instance.ButtonAction(action, transition, obj);
                            Prova1.Instance.ButtonAction(input, transition, obj);
                            break;
                        case ("Hand", FsmInput.TouchElement3D): //ho chiamato il metodo toccando un oggetto
                            HandController.Instance.CollisionAction(input, transition, obj);
                            break;
                        case ("InTrigger", FsmInput.EnterTrigger): //chiamo quando entro in un qualche zona trigger
                        case ("OutTrigger", FsmInput.ExitTrigger): //chiamo quando esco da qualche zona trigger
                            HeadController.Instance.InTrigger(input, transition, obj);
                            break;
                        //NB: Se voglio triggerare l'azione solo all'entrata allora come azione avrò TriggerCollision
                        case ("InTrigger", FsmInput.TriggerCollision):
                            HeadController.Instance.HeadAction(input, transition, obj);
                            break;
                        default:
                            break;
                    }
                }


                if (setting.Equals(SettingType.ORDERED))
                {
                    if (transition.TransitionInput[0].Triggered)
                    {
                        ord = 1;
                        for (int i = 1; i < transition.TransitionInput.Count; i++)
                        {
                            if (transition.TransitionInput[i].Triggered && transition.TransitionInput[i - 1].Triggered) //l'azione precedente è stata triggerata
                            {
                                ord++;
                            }
                            else
                            {
                                for (int j = i; j < transition.TransitionInput.Count; j++)
                                {
                                    transition.TransitionInput[j].Triggered = false; //annullo tutte le azioni dopo quella che non mi si è triggerata
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        input.Triggered = false;
                    }
                }

            }
        }










        /// *********************************************<summary>***********************************************************
        /// OGGETTI MANIPOLABILI (si comincia a manipolarli e poi si rilasciano)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="currentState"></param>
        /// <param name="startGrab"></param>
        /// <param name="rPoint"></param>
        /// <returns></returns>

        public string TransitionTrigger(GameObject obj, FSMState currentState, Dictionary<string, Vector3> startGrab, Vector3 rPoint, string flag)
        {
            firedTransition = null;
            foreach (var transition in currentState.ListOfTransitions)
            {
                //ConflictAnalyser(transition);
                if (Enum.TryParse(transition.SettingType, out SettingType setting))
                {
                    InputSetting(setting, transition, obj, startGrab, rPoint, flag);

                }

            }
            return firedTransition;
        }

        public void InputSetting(SettingType setting, FSMTransition transition, GameObject obj, Dictionary<string, Vector3> startGrab, Vector3 rPoint, string flag)
        {
            string objId = obj.name;
            switch (setting)
            {
                case SettingType.AND://tutte le azioni di una transizione devono aver avuto luogo

                    andCondition = true;
                    foreach (var input in transition.TransitionInput) //ciclo per settare se un azione è stata 'triggered'
                    {
                        if (Enum.TryParse(input.FsmInput, out FsmInput fsmInput))
                        {
                            //qui differenzio tra le varie azioni, e mi occupo di settare gli input.triggered a true
                            switch ((flag, fsmInput)) //collego azione chiamante e azione effettuata
                            {
                                case ("Grabbed", FsmInput.MoveElement3DDown):
                                case ("Grabbed", FsmInput.MoveElement3DUp):
                                    ManipulableManager.Instance.ManipulableAction(input, transition, obj, startGrab, rPoint);
                                    break;
                                default:
                                    break;
                            }
                        }
                        

                    }
                    foreach (var input in transition.TransitionInput) //ciclo per verificare che tutte le azioni siano state triggerate
                    {
                        if (!input.Triggered) //condizione per verificare che tutte le azioni della transizione siano state eseguite
                        {
                            andCondition = false;
                            break;
                        }
                    }
                    if (andCondition)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition); //a tutte le azioni il campio input.trigger viene settato nuovamente a false
                        Reset();

                    }
                    break;

                case SettingType.OR: ////////////solo un'azione qualsiasi della transizione deve essere stata fatta

                    foreach (var input in transition.TransitionInput) //ciclo per settare se un azione è stata 'triggered'
                    {
                        if (Enum.TryParse(input.FsmInput, out FsmInput fsmInput))
                        {
                            //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                            switch ((flag, fsmInput)) //collego azione chiamante e azione effettuata
                            {
                                case ("Grabbed", FsmInput.MoveElement3DDown):
                                case ("Grabbed", FsmInput.MoveElement3DUp):
                                    ManipulableManager.Instance.ManipulableAction(input, transition, obj, startGrab, rPoint);
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                    foreach (var input in transition.TransitionInput)
                    {
                        if (input.Triggered)//me ne basta una
                        {
                            firedTransition = transition.Name;
                            UnTrigger(transition);
                            Reset();
                            break;
                        }
                    }
                    break;

                case SettingType.ORDERED:

                    foreach (var input in transition.TransitionInput) //ciclo per settare se un azione è stata 'triggered'
                    {
                        if (Enum.TryParse(input.FsmInput, out FsmInput fsmInput))
                        {
                            //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                            switch ((flag, fsmInput)) //collego azione chiamante e azione effettuata
                            {
                                case ("Grabbed", FsmInput.MoveElement3DDown):
                                case ("Grabbed", FsmInput.MoveElement3DUp):
                                    ManipulableManager.Instance.ManipulableAction(input, transition, obj, startGrab, rPoint);
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (transition.TransitionInput[0].Triggered)
                        {
                            ord = 1;
                            for (int i = 1; i < transition.TransitionInput.Count; i++)
                            {
                                if (transition.TransitionInput[i].Triggered && transition.TransitionInput[i - 1].Triggered) //l'azione precedente è stata triggerata
                                {
                                    ord++;
                                }
                                else
                                {
                                    for (int j = i; j < transition.TransitionInput.Count; j++)
                                    {
                                        transition.TransitionInput[j].Triggered = false; //annullo tutte le azioni dopo quella che non mi si è triggerata
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            input.Triggered = false;
                        }
                    }
                    if (ord == transition.TransitionInput.Count)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition);
                        Reset();
                        ord = 0;
                        break;
                    }
                    break;

                default:
                    break;

            }

        }

        public void Reset()
        {
            //ButtonsManager.Instance.Reset();
            HandController.Instance.Reset();
            ManipulableManager.Instance.Reset();
            HeadController.Instance.Reset();
        }


    }
}

