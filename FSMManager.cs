using static AB.AnyAllModel.FSM;
using Appccelerate.StateMachine;
using Appccelerate.StateMachine.Machine;
using UnityEngine;
using System.IO;
using System;
using FSM = AB.AnyAllModel.FSM;
using AB.Interactor;
using AB.Instatiator;
using AB.FSMAnimator;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Random = System.Random;
using UnityEngine.UIElements;
using System.Collections;
using Microsoft.MixedReality.Toolkit;
using System.Threading;

namespace AB.FSMManager
{
    public class FSMManager : MonoBehaviour
    {
        private PassiveStateMachine<string, string> machine;
        private FSM fsm = new FSM();

        public FSMState currentState;

        private float currentTime = 0f;

        public float initialTime = 0f; //utilizzato per registrare il tempo d'ingresso in uno stato per far partire il counter in caso di trigger temporale
        public bool tempTrigg = false;

        public Dictionary<string, Vector3> startGrab = new Dictionary<string, Vector3>();
        public Dictionary<string, Vector3> endGrab = new Dictionary<string, Vector3>();
        List<string> tags = new List<string>();

        public Instatiator.Instatiator instatiator;
        public TempTrigger.TemporalTrigger temporalTrigger;
        public CollisionController.CollisionController collisionController;
        public HeadController.HeadController headController;
        public ManipulableManager.ManipulableManager manipulableManager;
        public ActionManager.ActionManager actionManager;

        public static FSMManager Instance { get; private set; }

        
        public void Start()
        {
            instatiator = Instatiator.Instatiator.Instance;
            temporalTrigger = TempTrigger.TemporalTrigger.Instance;
            collisionController = CollisionController.CollisionController.Instance;
            actionManager = ActionManager.ActionManager.Instance;
            manipulableManager = ManipulableManager.ManipulableManager.Instance;
            headController = HeadController.HeadController.Instance;


            fsm = ParseJson("/Resources/Json/Allocate.json"); //Insert the path of the Json that you want to parse
            tags = GetTags(fsm.ListOfObjects);

            InitializeFSM(fsm);


            InteractController.Instance.ButtonClick += OnButtonClick;
            InteractController.Instance.GrabbedObj += OnGrabbedObj;
            InteractController.Instance.ReleasedGrabbedObj += OnReleaseGrabbedObj;

            CollisionController.CollisionController.HandTrigger += OnHandTrigger; //subscription to an event
            HeadController.HeadController.HeadTriggerEnter += OnHeadTriggerEnter;
            HeadController.HeadController.HeadTriggerExit += OnHeadTriggerExit;

            ButtonsManager.ButtonsManager.Instance.Initialize(fsm, tags);
            collisionController.Initialize(tags, instatiator.ManipulableObject);
            manipulableManager.Initialize(tags, instatiator.ManipulableObject);
            headController.Initialize(tags, instatiator.TriggerObject);


        }


        //*****************EVENTSUBSCRIBERS******************************************

        //MANIPULATION
        public void OnGrabbedObj(GameObject obj, Vector3 point)
        { //point mi restituisce il punto nel quale ho toccato l'oggetto

            if (!startGrab.ContainsKey(obj.name))
            {
                startGrab.Add(obj.name, point); //startGrab è un dizionario che contiene il nome dell'oggetto come chiave e il punto da cui comincia la sua manipolazione come valore
            }
            else
            {
                startGrab[obj.name] = point;
            }
            Debug.Log("Moved " + obj.name + " from position " + point);

        }

        public void OnReleaseGrabbedObj(GameObject obj, Vector3 rPoint)
        { //point mi restituisce il punto nel quale ho rilasciato l'oggetto

            string transitionFired = ActionManager.ActionManager.Instance.TransitionTrigger(obj, currentState, startGrab, rPoint, "Grabbed");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
            }
            startGrab[obj.name] = rPoint; //aggiorno la posizione in cui è rilasciato
            Debug.Log("Moved " + obj.name + " to position " + rPoint);
        }

        //INTERACTION
        private void OnButtonClick(GameObject obj) //chiamato ogni volta che si clicca un bottone
        {
            string transitionFired = ActionManager.ActionManager.Instance.TransitionTrigger(obj, currentState, "Button");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
            }
            
        }

        public void OnHandTrigger(GameObject obj) //chiamato ogni volta che si tocca un oggetto 
        {
            
            string transitionFired = ActionManager.ActionManager.Instance.TransitionTrigger(obj, currentState, "Hand");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
            }
            
        }

        //TODO? Un OnHandTriggerExit

        private void OnHeadTriggerEnter(GameObject obj) //chiamato ogni volta che la testa collide con qualcosa
        {
            string transitionFired = ActionManager.ActionManager.Instance.TransitionTrigger(obj, currentState, "InTrigger");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
            }
        }

        private void OnHeadTriggerExit(GameObject obj) //chiamato ogni volta che termina la collisione della testa
        {
            string transitionFired = ActionManager.ActionManager.Instance.TransitionTrigger(obj, currentState, "OutTrigger");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
            }
        }

        //***************************************END EVENTSUBSCRIBERS*************************************************************


        public void Update()
        {
            currentTime += 1 * Time.deltaTime; //time update
            if (tempTrigg) //tempTrigg è true se nello stato corrente è presente un trigger temporale
            {
                StartCoroutine(Temp());
            }
            
        }

        /// <summary>
        /// Coroutine che attiva una transizione quando è passato un certo tempo
        /// Il metodo Info della classe temporal trigger restituisce la transizione in cui è presente il trigger ed il trashold
        /// </summary>
        /// <returns></returns>
        IEnumerator Temp()
        {
            string trans = temporalTrigger.Info(currentState).trans; //transizione in cui è presente il trigger temporale
            float trash = temporalTrigger.Info(currentState).trashold;
            if (currentTime - initialTime > trash) //se è trascorso un tempo maggiore del trashold da quando sono entrato nello stato 
            {
                this.machine.Fire(trans);
                tempTrigg = false; //vado in un nuovo stato in cui ancora non so se è presente un trigger temporale
            }
            yield return new WaitForSeconds(.5f);
        }


        //Json Parsing
        public static FSM ParseJson(string path) //path deve partire da dopo Assets
        {
            string fsmjsonPath = Application.dataPath + path;
            string fsmfileContent = File.ReadAllText(fsmjsonPath);
            FSM fsm = JsonUtility.FromJson<FSM>(fsmfileContent);
            return fsm;
        }
        //End parsing

        //FSM initialization
        public void InitializeFSM(FSM fsm)
        {

            var builder = new StateMachineDefinitionBuilder<string, string>();
            string initialState = null;
            try
            {
                foreach (var state in fsm.ListOfStates)
                {
                    if (state.ActionsOnEntry.Count != 0)
                    {
                        foreach (var actionEntry in state.ActionsOnEntry)
                        {
                            builder.In(state.Name)
                                    .ExecuteOnEntry(() => StateActions(actionEntry, state));
                                    //.ExecuteOnEntry(()=> InteractController.Instance.Listeners());
                        }
                    }
                    if(state.ActionsOnExit.Count != 0)
                    {
                        foreach (var actionExit in state.ActionsOnExit)
                        {
                            builder.In(state.Name)
                                    .ExecuteOnExit(() => StateActions(actionExit, state)); //inserito per includere gli effetti delle azioni che si effettuano in uscita dallo stato in cui ci si trova
                        }
                    }
                    if (state.ListOfTransitions.Count != 0)
                    {
                        foreach (var transition in state.ListOfTransitions)
                        {
                            builder.In(state.Name)
                            .On(transition.Name)
                                .If(() => CheckExternalCondition(transition.ExternalCondition)).Goto(transition.NextState);
                        }
                    }
                    if (state.InitialState == true)
                    {
                        initialState = state.Name;
                        currentState = state;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            builder.WithInitialState(initialState);
            machine = builder.Build().CreatePassiveStateMachine();


            try
            {
                machine.Start();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

        }


        //End initialization



        //begin function section 


        /// <summary>
        /// Funzione che si occupa di triggerare le azioni che si hanno nel momento in cui si entra in uno stato 
        /// </summary>
        /// <param name="actionEntry"></param>
        /// <param name="state"></param>
        public void StateActions(FSMAction action, FSMState state)
        {
            initialTime = currentTime; //istante in cui entro nello stato
            currentState = state;

            //ConflictDetector(state); //check se all'interno di uno stato ci sono stessi target per azioni diverse (conflitto)

            tempTrigg = temporalTrigger.Info(currentState).isPres;//se isPres è true significa che nello stato sono presenti delle transizioni che sono trigger temporali 

            if (action.fsmAction != null) 
            {
                var t = action.Target.Split(':');
                var target = t[0];
                var excluded = t.Length > 1 ? t[1] : null;
                switch (target)
                {
                    case "ALL": //tutti gli oggetti
                        switch (action.TargetType)
                        {
                            case "ManipulableObject":
                                foreach (var obj in instatiator.ManipulableObject)
                                {
                                    if (!obj.name.Equals(excluded) && !obj.tag.Equals(excluded)) //check se l'oggetto è escluso o meno
                                    {
                                        SwitchAction(action, obj);
                                    }
                                }
                                break;
                            case "ButtonObject":
                                foreach (var obj in instatiator.ButtonObject)
                                {
                                    if (!obj.name.Equals(excluded) && !obj.tag.Equals(excluded))
                                    {
                                        SwitchAction(action, obj);
                                    }
                                }
                                break;
                            case "TriggerObject":
                                foreach (var obj in instatiator.TriggerObject)
                                {
                                    if (!obj.name.Equals(excluded) && !obj.tag.Equals(excluded))
                                    {
                                        SwitchAction(action, obj);
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case "ANY"://un target qualsiasi
                        Random R = new();
                        switch (action.TargetType)
                        {
                            case "ManipulableObject":
                                int rand = R.Next(0, instatiator.ManipulableObject.Count()); //rand è un numero randomico tra gli indici della lista di oggetti Manipulable
                                GameObject mn = instatiator.ManipulableObject.ElementAt(rand);
                                var time = currentTime;
                                while (mn.name.Equals(excluded) | mn.tag.Equals(excluded))
                                {
                                    rand = R.Next(0, instatiator.ManipulableObject.Count());
                                    mn = instatiator.ManipulableObject.ElementAt(rand);
                                    if (currentTime - time > 180f)
                                    {
                                        break; //timeout nel caso in cui il target sia un manipulableobject ma non ne trovo nessuno
                                    }
                                } 
                                SwitchAction(action, mn);
                                break;
                            case "ButtonObject":
                                int rand2 = R.Next(0, instatiator.ButtonObject.Count());
                                GameObject bt = instatiator.ButtonObject.ElementAt(rand2);
                                time = currentTime;
                                while (bt.name.Equals(excluded) | bt.tag.Equals(excluded))
                                {
                                    rand2 = R.Next(0, instatiator.ButtonObject.Count());
                                    bt = instatiator.ButtonObject.ElementAt(rand2);
                                    if (currentTime - time > 180f)
                                    {
                                        break; //timeout nel caso in cui il target sia un manipulableobject ma non ne trovo nessuno
                                    }
                                } 
                                SwitchAction(action, bt);
                                break;
                            case "TriggerObject":
                                int rand3 = R.Next(0, instatiator.TriggerObject.Count());
                                GameObject btr = instatiator.TriggerObject.ElementAt(rand3);
                                time = currentTime;
                                while (btr.name.Equals(excluded) | btr.tag.Equals(excluded))
                                {
                                    rand2 = R.Next(0, instatiator.TriggerObject.Count());
                                    bt = instatiator.TriggerObject.ElementAt(rand2);
                                    if (currentTime - time > 180f)
                                    {
                                        break; //timeout nel caso in cui il target sia un manipulableobject ma non ne trovo nessuno
                                    }
                                }
                                SwitchAction(action, btr);
                                break;
                            default:
                                break;
                        }
                        break;
                    default: //target è o un insieme di oggetti con un dato Tag o un singolo Gameobject
                        if (tags.Contains(target))
                        {
                            foreach (var o in instatiator.SceneObject)
                            {
                                if (!o.name.Equals(excluded) && o.CompareTag(target))
                                {
                                    SwitchAction(action, o);
                                }

                            }
                        }
                        else //singolo gameobject
                        {
                            switch (action.TargetType)
                            {
                                case "ManipulableObject":
                                    foreach (var o in instatiator.ManipulableObject)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "ButtonObject":
                                    foreach (var o in instatiator.ButtonObject)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "TriggerObject":
                                    foreach (var o in instatiator.TriggerObject)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "AnimationObject":
                                    foreach (var o in instatiator.AnimationObject)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                }
                //questi if sotto li posso unire allo switch sopra
                if (action.TargetType.Equals("AnimationObject"))
                {   
                    
                    foreach(var obj in fsm.ListOfObjects)
                    {
                        if(obj.Type.Equals("AnimationObject") && obj.ObjectName.Equals(target))
                        {
                            foreach (var o in instatiator.AnimationObject)
                            {
                                if (o.name.Equals(target))
                                {
                                    StartAnimation(o, obj.Material);
                                }
                            }
                            break;
                        }
                    }
                    

                    
                }
                if (action.TargetType.Equals("Audio"))
                {
                    foreach(var obj in instatiator.AudioObject)
                    {
                        if (obj.name.Equals(target))
                        {
                            instatiator.audioSource.clip = obj;
                            
                            // Play the audio clip
                            instatiator.audioSource.Play();
                        }
                    }
                }
            }
        }

        public void SwitchAction(FSMAction action, GameObject target)
        {
            switch (action.fsmAction) //azione che verrà effettuata su target
            {
                case "TurnBlue":
                    target.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 1);
                    break;
                case "TurnRed":
                    target.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 1);
                    break;
                case "SetActive":
                    target.SetActive(true);
                    InteractController.Instance.AddListener(target);
                    break;
                case "SetInactive":
                    target.SetActive(false);
                    InteractController.Instance.RemoveListener(target);
                    break;
                default:
                    break;
            }
        }

        public void StartAnimation(GameObject target, string path)
        {
            target.AddComponent<FSMAnimator.FSMAnimator>();
            target.GetComponent<FSMAnimator.FSMAnimator>().LoadGltfBinaryFromMemory(path);
        }
        

        

        /// <summary>
        /// Metodo che serve a verificare se si sono avverate determinate condizioni esterne per poter passare allo stato successivo
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private bool CheckExternalCondition(string condition)
        {
            var t = condition.Split(':');
            var condName = t[0];
            var condValue = t.Length > 1 ? t[1] : null;

            switch (condName)
            {
                case "Trashold": //lo stato può triggerare azioni dopo un certo tempo 
                    float n = float.Parse(condValue, CultureInfo.InvariantCulture.NumberFormat);
                    if (currentTime > n)
                    {
                        return true;
                    }
                    else { return false; }
                default: //condition==null, non ho condizioni 
                    return true;
            }
        }

        //end function section 

        /// <summary>
        /// Il seguente metodo verifica se i target delle transizioni sono coerenti tra loro. 
        /// Ciò significa che, a partire da un certo stato, le sue azioni devono avere tutti target diversi (sia le azioni che fanno parte delle transizioni, sia tra quelle effettuate in entrata ad uno stato) 
        /// </summary>
        /// <param name="state"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ConflictDetector(FSMState state) //chiamo ogni volta che vado in uno stato nuovo
        {
            //verificare se per ogni ogni azione di ciascuna transizione si ripetono i target 
            //gestire i casi target ANY/ALL/gruppo di target con eventuali esclusi 

            List<string> targets = new List<string>();
            targets.Clear();

            //check dei conflitti tra target delle azioni delle transizioni di uno stato 
            foreach (var transition in state.ListOfTransitions)
            {
                foreach (var action in transition.ActionsOnTransition)
                {
                    var t = action.Target.Split(':'); //':' viene utilizzato per le esclusioni 
                    var tar = t[0]; //tar contiene il target dell'azione
                    var excluded = t.Length > 1 ? t[1] : null;

                    if (tar.Equals("ALL") | tar.Equals("ANY"))
                    {
                        foreach (var obj in fsm.ListOfObjects)
                        {
                            if (obj.Type == action.TargetType && !obj.ObjectName.Equals(excluded) && !obj.Tag.Equals(excluded))
                            {
                                targets.Add(obj.ObjectName); //i target saranno tutti gli oggetti ad eccezione degli esclusi
                            }
                        }
                    }
                    else if (tags.Contains(tar))
                    {
                        foreach (var obj in fsm.ListOfObjects)
                        {
                            if (obj.Type == action.TargetType && !obj.ObjectName.Equals(excluded) && obj.Tag.Equals(tar))
                            {
                                targets.Add(obj.ObjectName); //i target saranno tutti gli oggetti con il tag considerato 
                            }
                        }
                    }
                    else //il target è un oggetto
                    {
                        targets.Add(tar);
                    }

                }
            }
            if (targets.Count != targets.Distinct().Count()) //se esistono dei duplicati
            {
                throw new InvalidOperationException("Conflict occured. List of targets of transitions of state " + state.Name + ":\n" + String.Join(", ", targets));
            }

            targets.Clear();

            //check dei conflitti tra target delle azioni effettuate in entrata ad uno stato (come sopra)
            foreach (var action in state.ActionsOnEntry)
            {
                var t = action.Target.Split(':');
                var tar = t[0];
                var excluded = t.Length > 1 ? t[1] : null;

                if (tar.Equals("ALL") | tar.Equals("ANY"))
                {
                    foreach (var obj in fsm.ListOfObjects)
                    {
                        if (obj.Type == action.TargetType && !obj.ObjectName.Equals(excluded) && !obj.Tag.Equals(excluded))
                        {
                            targets.Add(obj.ObjectName); 
                        }
                    }
                }
                else if (tags.Contains(tar))
                {
                    foreach (var obj in fsm.ListOfObjects)
                    {
                        if (obj.Type == action.TargetType && !obj.ObjectName.Equals(excluded) && obj.Tag.Equals(tar))
                        {
                            targets.Add(obj.ObjectName); 
                        }
                    }
                }
                else //il target è un oggetto
                {
                    targets.Add(tar);
                }
            }
            if (targets.Count != targets.Distinct().Count()) //esistono dei duplicati
            {
                throw new InvalidOperationException("Conflict occured. List of targets of entry actions of state " + state.Name + ":\n" + String.Join(", ", targets));
            }
        }

        //****************************************************************************************************************************************************************

       
        public static void UnTrigger(FSMTransition transition)
        {
            foreach (var action in transition.ActionsOnTransition)
            {
                action.Triggered = false;
            }
        }


        public List<string> GetTags(List<FSMObj> objList)
        {
            foreach (var obj in objList)
            {
                if (obj.Tag != null && !tags.Contains(obj.Tag))
                {
                    tags.Add(obj.Tag);
                }

            }
            return tags;

        }

    }


}

