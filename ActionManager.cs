using AB.ButtonsManager;
using AB.CollisionController;
using AB.Instatiator;
using AB.ManipulableManager;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AB.FSMManager.FSMManager;
using static AB.AnyAllModel.FSM;
using AB.TempTrigger;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace AB.ActionManager
{
    public class ActionManager : MonoBehaviour
    {

        private bool andCondition = true;
        private int ord = 0;

        private string firedTransition = null;

        public ButtonsManager.ButtonsManager buttonManager;
        public CollisionController.CollisionController collisionManager;
        public ManipulableManager.ManipulableManager manipulableManager;

        public static ActionManager Instance { get; private set; } = new ActionManager();

        // Start is called before the first frame update
        void Start()
        {
            buttonManager = ButtonsManager.ButtonsManager.Instance;
            manipulableManager = ManipulableManager.ManipulableManager.Instance;
            collisionManager = CollisionController.CollisionController.Instance;
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
                    ActionSetting(setting, transition, obj, flag); //il flag deriva dal metodo che invoca il transitiontrigger

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
        public void ActionSetting(SettingType setting, FSMTransition transition, GameObject obj, string flag)
        {
            string objId = obj.name;   
            switch (setting)
            {
                case SettingType.AND:///////////tutte le azioni di una transizione devono aver avuto luogo

                    andCondition = true;

                    
                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        switch ((flag,action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button","ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand","TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger","InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            default:
                                break;
                        }
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        /*
                        switch (action.fsmAction)
                        {
                            case "ButtonClick":
                                if (flag.Equals("Button"))
                                {
                                    ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                }
                                //chiamo procedura per i bottoni
                                //Prova() far� si che se l'azione � stata attivata l'action.triggered sar� settato a true
                                break;
                            case "TouchCube":
                                if (flag.Equals("Hand"))
                                {
                                    CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                }
                                //chiamo procedura per il tocco
                                break;
                            case "MoveCubeDown":
                            case "MoveCubeUp":
                                //chiamo procedura per il grab
                                break;
                            case "StartTrigger":
                                if (flag.Equals("Head"))
                                {
                                    HeadController.HeadController.Instance.HeadAction(action, transition, obj);
                                }
                                break;
                            case "InTrigger":
                            case "OutTrigger":
                                if (flag.Equals("EnterTrigger") | flag.Equals("ExitTrigger"))
                                {
                                    HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                }
                                break;
                            default:
                                break;
                        }
                        */

                    }
                    foreach (var action in transition.ActionsOnTransition) //ciclo per verificare che tutte le azioni siano state triggerate
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
                        UnTrigger(transition); //a tutte le azioni il campio action.trigger viene settato nuovamente a false
                        ButtonsManager.ButtonsManager.Instance.Reset();
                        CollisionController.CollisionController.Instance.Reset();
                        ManipulableManager.ManipulableManager.Instance.Reset();
                        HeadController.HeadController.Instance.GlobalReset();

                    }

                    break;
                case SettingType.OR: ////////////solo un'azione qualsiasi della transizione deve essere stata fatta
                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        switch ((flag, action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button", "ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand", "TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger", "InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            default:
                                break;
                        }
                        /*
                        switch (action.fsmAction)
                        {
                            case "ButtonClick":
                                if (flag.Equals("Button"))
                                {
                                    ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                }
                                //chiamo procedura per i bottoni
                                //Prova() far� si che se l'azione � stata attivata l'action.triggered sar� settato a true
                                break;
                            case "TouchCube":
                                if (flag.Equals("Hand"))
                                {
                                    CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                }
                                //chiamo procedura per il tocco
                                break;
                            case "MoveCubeDown":
                            case "MoveCubeUp":
                                //chiamo procedura per il grab
                                break;
                            case "StartTrigger":
                                if (flag.Equals("Head"))
                                {
                                    HeadController.HeadController.Instance.HeadAction(action, transition, obj);
                                }
                                break;
                            case "InTrigger":
                            case "OutTrigger":
                                if (flag.Equals("Head"))
                                {
                                    HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                }
                                break;
                            default:
                                break;
                        }*/

                    }
                    foreach (var action in transition.ActionsOnTransition)
                    {
                        if (action.Triggered)//me ne basta una
                        {
                            //this.machine.Fire(transition.Name);
                            firedTransition = transition.Name;
                            UnTrigger(transition);
                            ButtonsManager.ButtonsManager.Instance.Reset();
                            CollisionController.CollisionController.Instance.Reset();
                            ManipulableManager.ManipulableManager.Instance.Reset();
                            HeadController.HeadController.Instance.GlobalReset();
                            //ResetButtons(buttons);
                            break;
                        }
                    }
                    break;
                case SettingType.ORDERED:

                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        switch ((flag, action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button", "ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand", "TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger", "InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            default:
                                break;
                        }
                        /*
                        switch (action.fsmAction)
                        {
                            case "ButtonClick":
                                if (flag.Equals("Button"))
                                {
                                    ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                }
                                //chiamo procedura per i bottoni
                                //Prova() far� si che se l'azione � stata attivata l'action.triggered sar� settato a true
                                break;
                            case "TouchCube":
                                if (flag.Equals("Hand"))
                                {
                                    CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                }
                                //chiamo procedura per il tocco
                                break;
                            case "MoveCubeDown":
                            case "MoveCubeUp":
                                //chiamo procedura per il grab
                                break;
                            case "StartTrigger":
                                if (flag.Equals("Head"))
                                {
                                    HeadController.HeadController.Instance.HeadAction(action, transition, obj);
                                }
                                break;
                            case "InTrigger":
                            case "OutTrigger":
                                if (flag.Equals("Head"))
                                {
                                    HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                }
                                break;
                            default:
                                break;
                        }*/

                        if (transition.ActionsOnTransition[0].Triggered)
                        {
                            ord = 1;
                            for (int i = 1; i < transition.ActionsOnTransition.Count; i++)
                            {
                                if (transition.ActionsOnTransition[i].Triggered && transition.ActionsOnTransition[i - 1].Triggered) //l'azione precedente � stata triggerata
                                {
                                    ord++;
                                }
                                else
                                {
                                    for (int j = i; j < transition.ActionsOnTransition.Count; j++)
                                    {
                                        transition.ActionsOnTransition[j].Triggered = false; //annullo tutte le azioni dopo quella che non mi si � triggerata
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            action.Triggered=false; 
                        }
                    }
                    


                    if (ord == transition.ActionsOnTransition.Count)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition);
                        ButtonsManager.ButtonsManager.Instance.Reset();
                        CollisionController.CollisionController.Instance.Reset();
                        ManipulableManager.ManipulableManager.Instance.Reset();
                        HeadController.HeadController.Instance.GlobalReset();
                        //ResetButtons(buttons);
                        break;
                    }
                    
                    break;
                default:
                    break;

            }

        }

        /// <summary>
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
                SettingType setting;
                if (Enum.TryParse(transition.SettingType, out setting))
                {
                    ActionSetting(setting, transition, obj, startGrab, rPoint, flag);

                }

            }
            return firedTransition;
        }

        public void ActionSetting(SettingType setting, FSMTransition transition, GameObject obj, Dictionary<string, Vector3> startGrab, Vector3 rPoint, string flag)
        {
            string objId = obj.name;
            switch (setting)
            {
                case SettingType.AND:///////////tutte le azioni di una transizione devono aver avuto luogo

                    andCondition = true;
                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        switch ((flag, action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button", "ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand", "TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger", "InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            case ("Grabbed", "MoveSolidDown"):
                            case ("Grabbed", "MoveSolidUp"):
                                ManipulableManager.ManipulableManager.Instance.ManipulableAction(action, transition, obj, startGrab, rPoint);
                                //chiamo procedura per il grab
                                break;
                            default:
                                break;
                        }
                        /*
                        switch (action.fsmAction)
                        {
                            case "ButtonClick":
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                //chiamo procedura per i bottoni
                                //Prova() far� si che se l'azione � stata attivata l'action.triggered sar� settato a true
                                break;
                            case "TouchCube":
                                //chiamo procedura per il tocco
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case "MoveCubeDown":
                            case "MoveCubeUp":
                                ManipulableManager.ManipulableManager.Instance.ManipulableAction(action, transition, obj, startGrab, rPoint);
                                //chiamo procedura per il grab
                                break;
                            case "StartTrigger":
                                HeadController.HeadController.Instance.HeadAction(action, transition, obj);
                                break;
                            case "InTrigger":
                            case "OutTrigger":
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            default:
                                break;
                        }*/

                    }
                    foreach (var action in transition.ActionsOnTransition) //ciclo per verificare che tutte le azioni siano state triggerate
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
                        UnTrigger(transition); //a tutte le azioni il campio action.trigger viene settato nuovamente a false
                        ButtonsManager.ButtonsManager.Instance.Reset();
                        CollisionController.CollisionController.Instance.Reset();
                        ManipulableManager.ManipulableManager.Instance.Reset();
                        HeadController.HeadController.Instance.GlobalReset();
                        //ResetButtons(buttons); //da mettere magari una procedura che fa il reset di tutti i campi (quindi chiama la reset di tutte le classi coinvolte)

                    }

                    break;
                case SettingType.OR: ////////////solo un'azione qualsiasi della transizione deve essere stata fatta
                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        switch ((flag, action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button", "ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand", "TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger", "InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            case ("MoveCubeDown", "Grabbed"):
                            case ("MoveCubeUp", "Grabbed"):
                                ManipulableManager.ManipulableManager.Instance.ManipulableAction(action, transition, obj, startGrab, rPoint);
                                //chiamo procedura per il grab
                                break;
                            default:
                                break;
                        }
                        /*
                        switch (action.fsmAction)
                        {
                            case "ButtonClick":
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                //chiamo procedura per i bottoni
                                //Prova() far� si che se l'azione � stata attivata l'action.triggered sar� settato a true
                                break;
                            case "TouchCube":
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                //chiamo procedura per il tocco
                                break;
                            case "MoveCubeDown":
                            case "MoveCubeUp":
                                ManipulableManager.ManipulableManager.Instance.ManipulableAction(action, transition, obj, startGrab, rPoint);
                                //chiamo procedura per il grab
                                break;
                            case "StartTrigger":
                                HeadController.HeadController.Instance.HeadAction(action, transition, obj);
                                break;
                            case "InTrigger":
                            case "OutTrigger":
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            default:
                                break;
                        }*/

                    }
                    foreach (var action in transition.ActionsOnTransition)
                    {
                        if (action.Triggered)//me ne basta una
                        {
                            //this.machine.Fire(transition.Name);
                            firedTransition = transition.Name;
                            UnTrigger(transition);
                            ButtonsManager.ButtonsManager.Instance.Reset();
                            CollisionController.CollisionController.Instance.Reset();
                            ManipulableManager.ManipulableManager.Instance.Reset();
                            HeadController.HeadController.Instance.GlobalReset();
                            //ResetButtons(buttons);
                            break;
                        }
                    }
                    break;
                case SettingType.ORDERED:

                    foreach (var action in transition.ActionsOnTransition) //ciclo per settare se un azione � stata 'triggered'
                    {
                        //qui differenzio tra le varie azioni, e mi occupo di settare gli action.triggered a true
                        switch ((flag, action.fsmAction)) //collego azione chiamante e azione effettuata
                        {
                            //guarda appunti su docs
                            case ("Button", "ButtonClick"): //ho chiamato il metodo cliccando un bottone
                                ButtonsManager.ButtonsManager.Instance.ButtonAction(action, transition, obj);
                                break;
                            case ("Hand", "TouchCube"): //ho chiamato il metodo toccando un oggetto
                                CollisionController.CollisionController.Instance.CollisionAction(action, transition, obj);
                                break;
                            case ("EnterTrigger", "InTrigger"): //chiamo quando entro in un qualche zona trigger
                            case ("ExitTrigger", "OutTrigger"): //chiamo quando esco da qualche zona trigger
                                HeadController.HeadController.Instance.InTrigger(action, transition, obj);
                                break;
                            case ("MoveCubeDown", "Grabbed"):
                            case ("MoveCubeUp", "Grabbed"):
                                ManipulableManager.ManipulableManager.Instance.ManipulableAction(action, transition, obj, startGrab, rPoint);
                                //chiamo procedura per il grab
                                break;
                            default:
                                break;
                        }

                        if (transition.ActionsOnTransition[0].Triggered)
                        {
                            ord = 1;
                            for (int i = 1; i < transition.ActionsOnTransition.Count; i++)
                            {
                                if (transition.ActionsOnTransition[i].Triggered && transition.ActionsOnTransition[i - 1].Triggered) //l'azione precedente � stata triggerata
                                {
                                    ord++;
                                }
                                else
                                {
                                    for (int j = i; j < transition.ActionsOnTransition.Count; j++)
                                    {
                                        transition.ActionsOnTransition[j].Triggered = false; //annullo tutte le azioni dopo quella che non mi si � triggerata
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            action.Triggered = false;
                        }
                    }



                    if (ord == transition.ActionsOnTransition.Count)
                    {
                        firedTransition = transition.Name;
                        UnTrigger(transition);
                        ButtonsManager.ButtonsManager.Instance.Reset();
                        CollisionController.CollisionController.Instance.Reset();
                        ManipulableManager.ManipulableManager.Instance.Reset();
                        HeadController.HeadController.Instance.GlobalReset();
                        //ResetButtons(buttons);
                        break;
                    }

                    break;
                default:
                    break;

            }

        }


    }
}

