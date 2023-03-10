using Appccelerate.StateMachine;
using Appccelerate.StateMachine.Machine;
using UnityEngine;
using System.IO;
using System;
using AB.Controller.Interactor;
using AB.Controller.Hand;
using AB.Controller.Head;
using AB.Controller.Instatiator;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Random = System.Random;
using System.Collections;
using AB.Manager.Button;
using AB.Model.FSM;
using static AB.Model.FSM.FSMModel;
using AB.Manager.Manipulable;
using AB.Manager.Action;
using AB.Trigger.Temporal;
using AB.Manager.Prova;
using static UnityEngine.GraphicsBuffer;
using Microsoft.MixedReality.Toolkit.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem.Android;

namespace AB.Manager.FSM
{
    public class FSMManager : MonoBehaviour
    {
        private PassiveStateMachine<string, string> machine;
        private FSMModel fsm = new();

        public FSMState currentState;

        private float currentTime = 0f;

        public float initialTime = 0f; //utilizzato per registrare il tempo d'ingresso in uno stato per far partire il counter in caso di trigger temporale
        public bool tempTrigg = false;

        private int coroutinesOnExitCounter=0;
        private int coroutinesOnEnterCounter = 0;
        public bool animated = false;

        public Dictionary<string, Vector3> objCoordBeforeGrabbing = new Dictionary<string, Vector3>();
        public Dictionary<string, Vector3> endGrab = new Dictionary<string, Vector3>();
        List<string> tags = new List<string>();

        public InstanceController instatiator;
        public TemporalTrigger temporalTrigger;
        public HandController handController;
        public HeadController headController;
        public ManipulableManager manipulableManager;
        public ActionManager actionManager;
        public Prova1 prova1;

        public static FSMManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            instatiator = InstanceController.Instance;
            temporalTrigger = TemporalTrigger.Instance;
            handController = HandController.Instance;
            actionManager = ActionManager.Instance;
            manipulableManager = ManipulableManager.Instance;
            headController = HeadController.Instance;
            prova1 = Prova1.Instance;


            fsm = ParseJson("/Resources/Json/Tester4.json"); //Insert the path of the Json that you want to parse
            tags = GetTags(fsm.ListOfObjects);

            InitializeFSM(fsm);


            InteractController.Instance.ButtonClick += OnButtonClick;
            InteractController.Instance.GrabbedObj += OnGrabbedObj;
            InteractController.Instance.ReleasedGrabbedObj += OnReleaseGrabbedObj;

            HandController.HandTrigger += OnHandTrigger; //subscription to an event
            HeadController.HeadTriggerEnter += OnHeadTriggerEnter;
            HeadController.HeadTriggerExit += OnHeadTriggerExit;

            //ButtonsManager.Instance.Initialize(fsm, tags);
            handController.Initialize(tags, instatiator.ListOfElement3D);
            manipulableManager.Initialize(tags, instatiator.ListOfElement3D);
            headController.Initialize(tags, instatiator.ListOfTriggers);
            prova1.Initialize(tags, instatiator.ListOfButtons);

        }


        //*****************EVENTSUBSCRIBERS******************************************

        //MANIPULATION
        public void OnGrabbedObj(GameObject obj, Vector3 startCoord)
        { //startCoord mi restituisce il punto nel quale ho toccato l'oggetto

            if (!objCoordBeforeGrabbing.ContainsKey(obj.name)) 
            {
                objCoordBeforeGrabbing.Add(obj.name, startCoord); //objCoordBeforeGrabbing ? un dizionario che contiene il nome dell'oggetto come chiave e il punto da cui comincia la sua manipolazione come valore
            }
            else
            {
                objCoordBeforeGrabbing[obj.name] = startCoord;
            }
            Debug.Log("Moved " + obj.name + " from position " + startCoord);

        }

        public void OnReleaseGrabbedObj(GameObject obj, Vector3 releaseCoord)
        { //point mi restituisce il punto nel quale ho rilasciato l'oggetto

            string transitionFired = ActionManager.Instance.TransitionTrigger(obj, currentState, objCoordBeforeGrabbing, releaseCoord, "Grabbed");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
                Debug.Log("CurrentState: " + currentState.Name);
            }
            objCoordBeforeGrabbing[obj.name] = releaseCoord; //aggiorno la posizione in cui ? rilasciato
            Debug.Log("Moved " + obj.name + " to position " + releaseCoord);
        }

        //INTERACTION
        private void OnButtonClick(GameObject obj) //chiamato ogni volta che si clicca un bottone
        {
            string transitionFired = ActionManager.Instance.TransitionTrigger(obj, currentState, "Button");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
                Debug.Log("CurrentState: " + currentState.Name);
            }
            
        }

        public void OnHandTrigger(GameObject obj) //chiamato ogni volta che si tocca un oggetto 
        {
            
            string transitionFired = ActionManager.Instance.TransitionTrigger(obj, currentState, "Hand");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
                Debug.Log("CurrentState: " + currentState.Name);
            }
            
        }


        private void OnHeadTriggerEnter(GameObject obj) //chiamato ogni volta che la testa collide con qualcosa
        {
            string transitionFired = ActionManager.Instance.TransitionTrigger(obj, currentState, "InTrigger");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
                Debug.Log("CurrentState: " + currentState.Name);
            }
        }

        private void OnHeadTriggerExit(GameObject obj) //chiamato ogni volta che termina la collisione della testa
        {
            string transitionFired = ActionManager.Instance.TransitionTrigger(obj, currentState, "OutTrigger");
            if (transitionFired != null)
            {
                this.machine.Fire(transitionFired);
                Debug.Log("Transition " + transitionFired + " fired.");
                Debug.Log("CurrentState: " + currentState.Name);
            }
        }

        //***************************************END EVENTSUBSCRIBERS*************************************************************


        public void Update()
        {
            currentTime += 1 * Time.deltaTime; //time update
            
            if (tempTrigg) //tempTrigg ? true se nello stato corrente ? presente un trigger temporale
            {
                StartCoroutine(TemporalTriggerCoroutine());
            }
            

        }

        /// <summary>
        /// Coroutine che attiva una transizione quando ? passato un certo tempo
        /// Il metodo Info della classe temporal trigger restituisce la transizione in cui ? presente il trigger ed il trashold
        /// </summary>
        /// <returns></returns>
        IEnumerator TemporalTriggerCoroutine()
        {
            string transitionWithTemporalTrigger = temporalTrigger.Info(currentState).trans; //transizione in cui ? presente il trigger temporale
            float timeToTrigger = temporalTrigger.Info(currentState).trashold;
            if (currentTime - initialTime > timeToTrigger) //se ? trascorso un tempo maggiore del trashold da quando sono entrato nello stato 
            {
                this.machine.Fire(transitionWithTemporalTrigger);
                tempTrigg = false; //vado in un nuovo stato in cui ancora non so se ? presente un trigger temporale
            }
            yield return new WaitForSeconds(.5f);
        }


        //Json Parsing
        public static FSMModel ParseJson(string path) //path deve partire da dopo Assets
        {
            string fsmjsonPath = Application.dataPath + path;
            string fsmfileContent = File.ReadAllText(fsmjsonPath);
            FSMModel fsm = JsonUtility.FromJson<FSMModel>(fsmfileContent);
            return fsm;
        }
        //End parsing

        //FSM initialization
        public void InitializeFSM(FSMModel fsm)
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
                                    .ExecuteOnEntry(() => StartCoroutine(WaitForCoroutinesOnExit(actionEntry, state)));//.ExecuteOnEntry(() => StateActions(actionEntry, state));
                        }
                    }
                    if(state.ActionsOnExit.Count != 0)
                    {
                        foreach (var actionExit in state.ActionsOnExit)
                        {
                            builder.In(state.Name)
                                    .ExecuteOnExit(() => StateActions(actionExit, state));//inserito per includere gli effetti delle azioni che si effettuano in uscita dallo stato in cui ci si trova
                                     
                        }
                    }
                    if (state.ListOfTransitions.Count != 0)
                    {
                        foreach (var transition in state.ListOfTransitions)
                        {
                            builder.In(state.Name)
                            .On(transition.Name)
                                .If(() => CheckCoroutinesOnEnter(transition)).Goto(transition.NextState);
                                //.If(() => CheckActiveCoroutine(coroutineCounter, transition)).Goto(transition.NextState);
                        }//vedere bene la temporalit? delle esecuzioni e sfruttare quella
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

            tempTrigg = temporalTrigger.Info(currentState).isPres;//se isPres ? true significa che nello stato sono presenti delle transizioni che sono trigger temporali 

            if (action.FsmAction != null) 
            {
                var t = action.ActionTarget.Split(':');
                var target = t[0];
                var excluded = t.Length > 1 ? t[1] : null;
                switch (target)
                {
                    case "ALL": //tutti gli oggetti
                        switch (action.Group)
                        {
                            case "Element3D":
                                foreach (var obj in instatiator.ListOfElement3D)
                                {
                                    if (!obj.name.Equals(excluded) && !obj.tag.Equals(excluded)) //check se l'oggetto ? escluso o meno
                                    {
                                        SwitchAction(action, obj);
                                    }
                                }
                                break;
                            case "ButtonObject":
                                foreach (var obj in instatiator.ListOfButtons)
                                {
                                    if (!obj.name.Equals(excluded) && !obj.tag.Equals(excluded))
                                    {
                                        SwitchAction(action, obj);
                                    }
                                }
                                break;
                            case "TriggerObject":
                                foreach (var obj in instatiator.ListOfTriggers)
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
                        switch (action.Group)
                        {
                            case "Element3D":
                                int rand = R.Next(0, instatiator.ListOfElement3D.Count()); //rand ? un numero randomico tra gli indici della lista di oggetti Manipulable
                                GameObject mn = instatiator.ListOfElement3D.ElementAt(rand);
                                var time = currentTime;
                                while (mn.name.Equals(excluded) | mn.tag.Equals(excluded))
                                {
                                    rand = R.Next(0, instatiator.ListOfElement3D.Count());
                                    mn = instatiator.ListOfElement3D.ElementAt(rand);
                                    if (currentTime - time > 180f)
                                    {
                                        break; //timeout nel caso in cui il target sia un manipulableobject ma non ne trovo nessuno
                                    }
                                } 
                                SwitchAction(action, mn);
                                break;
                            case "ButtonObject":
                                int rand2 = R.Next(0, instatiator.ListOfButtons.Count());
                                GameObject bt = instatiator.ListOfButtons.ElementAt(rand2);
                                time = currentTime;
                                while (bt.name.Equals(excluded) | bt.tag.Equals(excluded))
                                {
                                    rand2 = R.Next(0, instatiator.ListOfButtons.Count());
                                    bt = instatiator.ListOfButtons.ElementAt(rand2);
                                    if (currentTime - time > 180f)
                                    {
                                        break; //timeout nel caso in cui il target sia un manipulableobject ma non ne trovo nessuno
                                    }
                                } 
                                SwitchAction(action, bt);
                                break;
                            case "TriggerObject":
                                int rand3 = R.Next(0, instatiator.ListOfTriggers.Count());
                                GameObject btr = instatiator.ListOfTriggers.ElementAt(rand3);
                                time = currentTime;
                                while (btr.name.Equals(excluded) | btr.tag.Equals(excluded))
                                {
                                    rand2 = R.Next(0, instatiator.ListOfTriggers.Count());
                                    bt = instatiator.ListOfTriggers.ElementAt(rand2);
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
                    default: //target ? o un insieme di oggetti con un dato Tag o un singolo Gameobject
                        if (tags.Contains(target))
                        {
                            foreach (var o in instatiator.ListOfSceneObj)
                            {
                                if (!o.name.Equals(excluded) && o.CompareTag(target))
                                {
                                    SwitchAction(action, o);
                                }

                            }
                        }
                        else //singolo gameobject
                        {
                            switch (action.Group)
                            {
                                case "Element3D":
                                    foreach (var o in instatiator.ListOfElement3D)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "ButtonObject":
                                    foreach (var o in instatiator.ListOfButtons)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "TriggerObject":
                                    foreach (var o in instatiator.ListOfTriggers)
                                    {
                                        if (o.name.Equals(target))
                                        {
                                            SwitchAction(action, o);
                                        }
                                    }
                                    break;
                                case "AnimationObject":
                                    foreach (var o in instatiator.dictionaryOfAnimations)
                                    {
                                        if (o.Key.name.Equals(target))
                                        {
                                            SwitchAction(action, o.Key);
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                }
                if (action.Group.Equals("Audio"))
                {
                    foreach(var obj in instatiator.ListOfAudioClips)
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
            FsmAction fsmAction;
            if (Enum.TryParse(action.FsmAction, out fsmAction))
            {
                switch (fsmAction) //azione che verr? effettuata su target
                {
                    case FsmAction.SetActive:
                        if (!target.activeSelf)
                        {
                            target.SetActive(true);
                            InteractController.Instance.AddListener(target);
                        }
                        break;
                    case FsmAction.SetInactive:
                        if (target.activeSelf)
                        {
                            if (target.GetComponent<PressableButtonHoloLens2>() == null) //non ho un bottone
                            {
                                handController.isModified = true;
                            }
                            target.SetActive(false);
                            InteractController.Instance.RemoveListener(target);
                        }
                        break;
                    case FsmAction.SlowlyAppear: //al posto di SetActive
                                         //TODO? non so se aggiungere il check per verificare che il materiale dell'oggetto sia 'Fade' o se lasciarlo come assunzione
                        if (!target.activeSelf)
                        {
                            StartCoroutine(FadeInOut(target, 1f)); //da aggiustare
                        }
                        break;
                    case FsmAction.SlowlyDisappear: //al posto di SetInactive
                        if (target.activeSelf)
                        {
                            StartCoroutine(FadeInOut(target, 0f));
                        }
                        break;
                    case FsmAction.TurnBlue:
                        if (target.activeSelf)
                        {
                            Renderer rend = target.GetComponent<Renderer>();
                            rend.material.color = new Color(0, 0, 1, rend.material.color.a);
                        }
                        break;
                    case FsmAction.TurnRed:
                        if (target.activeSelf)
                        {
                            Renderer rend = target.GetComponent<Renderer>();
                            rend.material.color = new Color(1, 0, 0, rend.material.color.a);
                        }
                        break;
                    case FsmAction.Translate:
                        if (target.activeSelf)
                            StartCoroutine(this.LERPtransl(target, action));
                        break;
                    case FsmAction.Rotate:
                        if (target.activeSelf)
                            StartCoroutine(this.LERProt(target, action));
                        break;
                    case FsmAction.Scaling:
                        if (target.activeSelf)
                            StartCoroutine(this.LERPscale(target, action));
                        break;
                    case FsmAction.StartAnimation:
                        foreach (var anim in instatiator.dictionaryOfAnimations)
                        {
                            if (target == anim.Key)
                            {
                                StartAnimation(target, anim.Value);
                                //StartCoroutine(WaitForAnimation());
                            }
                        }
                        break;
                    case FsmAction.PlayAnimation:
                        Animation animation = target.GetComponent<Animation>();
                        animation.Play(action.AnimationClip);
                        break;
                    case FsmAction.StopAnimation:
                        Animation animationToStop = target.GetComponent<Animation>();
                        animationToStop.Stop(); //stop all the animations
                        break;
                    default:
                        break;
                }

            }
            
        }

        public void StartAnimation(GameObject target, string path)
        {
            target.SetActive(true); //da aggiustare
            target.AddComponent<FSMAnimator.FSMAnimator>();
            animated = true;
            target.GetComponent<FSMAnimator.FSMAnimator>().LoadGltfBinaryFromMemory(path);
            animated = false;
        }

        IEnumerator WaitForAnimation()
        {
            yield return new WaitUntil(() => (animated == false));
        }



        IEnumerator LERPtransl(GameObject o, FSMAction action)
        {
            coroutinesOnEnterCounter++;
            float timeElapsed = 0f; //tempo da cui parto
            float duration = action.MovementParameters.MovementDuration; //durata del movimento
            Vector3 startPosition = o.transform.position; //posizione iniziale dell'oggetto
            Vector3 endPosition = new(action.MovementParameters.TargetCoord.x, action.MovementParameters.TargetCoord.y, action.MovementParameters.TargetCoord.z);
            float distanceToTarget = Vector3.Distance(startPosition, endPosition);
            //while (timeElapsed < duration)
            //NB: Nel caso sopra faccio relazione alla durata da me indicata (in questo caso per? devo aggiustare la velocit?, che non pu? pi? essere costante ma relazionata alla quantit? di spazio che devo percorrere
            //mentre nel caso sotto, a velocit? COSTANTE, l'oggetto continua a spostarsi linearmente fino a raggiungere una posizione target
            while (o.transform.position != endPosition)
            {
                float distanceCovered = timeElapsed * action.MovementParameters.MovementSpeed;
                float fractionOfJourney = distanceCovered / distanceToTarget;
                o.transform.position = Vector3.Lerp(startPosition, endPosition, fractionOfJourney);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            o.transform.position = endPosition; 
            coroutinesOnEnterCounter--;
        }

        IEnumerator LERProt(GameObject o, FSMAction action)
        {
            coroutinesOnEnterCounter++;
            float timeElapsed = 0f; //tempo da cui parto
            float duration = action.MovementParameters.MovementDuration; //durata del movimento
            Vector3 targetPos = new(action.MovementParameters.TargetCoord.x, action.MovementParameters.TargetCoord.y, action.MovementParameters.TargetCoord.z);
            Quaternion startRotation = o.transform.rotation;
            Quaternion targetRotation = o.transform.rotation * Quaternion.Euler(targetPos);

            //while (timeElapsed < duration)
            while(o.transform.rotation != targetRotation)
            {
                float distanceCovered = timeElapsed * action.MovementParameters.MovementSpeed;
                o.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, distanceCovered / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            o.transform.rotation = targetRotation;
            coroutinesOnEnterCounter--;

        }

        IEnumerator LERPscale(GameObject o, FSMAction action)
        {
            coroutinesOnEnterCounter++;
            float timeElapsed = 0f;
            float duration = action.MovementParameters.MovementDuration;
            Vector3 startScale = o.transform.localScale;
            Vector3 endScale = new(action.MovementParameters.TargetCoord.x, action.MovementParameters.TargetCoord.y, action.MovementParameters.TargetCoord.z);
            float scaleDifference = Vector3.Distance(startScale, endScale);

            //while (timeElapsed < duration)
            while(o.transform.localScale != endScale)
            {
                float distanceCovered = timeElapsed * action.MovementParameters.MovementSpeed;
                o.transform.localScale = Vector3.Lerp(startScale, endScale, distanceCovered / scaleDifference);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            o.transform.localScale = endScale;
            coroutinesOnEnterCounter--;

        }

        IEnumerator FadeInOut(GameObject target, float value)
        {   
            if (target.GetComponent<PressableButtonHoloLens2>() != null) //caso bottone
            {
                PressableButtonHoloLens2 button = target.GetComponent<PressableButtonHoloLens2>();
                yield return null;
            }

            Renderer rend = target.GetComponent<Renderer>();

            if (value == 0f) //SlowlyDisappear
            {
                coroutinesOnExitCounter++;
            }
            else if (value == 1f) //SlowlyAppear
            {
                coroutinesOnEnterCounter++;
                //faccio in modo che il gameobject sia completamente trasparente prima di attivarlo
                
                rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, 0f);

                target.SetActive(true);
                InteractController.Instance.AddListener(target);
            }

            // Get the initial alpha value of the object's material
            float alpha = rend.material.color.a;
            float fadeTime = 2f;
            float updatesPerSecond = 60f;
            float updateInterval = 1f / updatesPerSecond;
            
            // Gradually decrease the alpha value to 0 over time
            for (float t = 0.0f; t < fadeTime; t += updateInterval)
            {
                Color newColor = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, Mathf.Lerp(alpha, value, t / fadeTime));
                rend.material.color = newColor;
                yield return new WaitForSeconds(updateInterval);
            }
            
            if (value == 0f)
            {
                if (target.GetComponent<PressableButtonHoloLens2>() == null) //non ho un bottone
                {
                    handController.isModified = true;
                }
                target.SetActive(false);
                InteractController.Instance.RemoveListener(target);
                rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, 1f);
                coroutinesOnExitCounter--;
            }
            else if(value == 1f)
            {
                coroutinesOnEnterCounter--;
            }

            
        }

        IEnumerator WaitForCoroutinesOnExit(FSMAction actionEntry, FSMState state)
        {

            yield return new WaitUntil(()=> (coroutinesOnExitCounter == 0));
            //this.machine.Fire(transition);
            StateActions(actionEntry, state);
        }

        IEnumerator WaitForCoroutinesOnEnter(FSMTransition transition)
        {

            yield return new WaitUntil(() => (coroutinesOnEnterCounter == 0));
            this.machine.Fire(transition.Name);
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
                case "Trashold": //lo stato pu? triggerare azioni dopo un certo tempo 
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

        private bool CheckCoroutinesOnEnter(FSMTransition transition)
        {
            if (coroutinesOnEnterCounter > 0)
            {
                StartCoroutine(WaitForCoroutinesOnEnter(transition));
                return false;

            }
            else
            {
                return true;
            }
        }


        

        //end function section 

        /// <summary>
        /// Il seguente metodo verifica se i target delle transizioni sono coerenti tra loro. 
        /// Ci? significa che, a partire da un certo stato, le sue azioni devono avere tutti target diversi (sia le azioni che fanno parte delle transizioni, sia tra quelle effettuate in entrata ad uno stato) 
        /// </summary>
        /// <param name="state"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ConflictDetector(FSMState state) //chiamo ogni volta che vado in uno stato nuovo [non so quanto necessario]
        {
            //verificare se per ogni ogni azione di ciascuna transizione si ripetono i target 
            //gestire i casi target ANY/ALL/gruppo di target con eventuali esclusi 

            List<string> targets = new List<string>();
            targets.Clear();

            //check dei conflitti tra target delle azioni delle transizioni di uno stato 
            foreach (var transition in state.ListOfTransitions)
            {
                foreach (var action in transition.TransitionInput)
                {
                    var t = action.InputTarget.Split(':'); //':' viene utilizzato per le esclusioni 
                    var tar = t[0]; //tar contiene il target dell'azione
                    var excluded = t.Length > 1 ? t[1] : null;

                    if (tar.Equals("ALL") | tar.Equals("ANY"))
                    {
                        foreach (var obj in fsm.ListOfObjects)
                        {
                            if (!obj.ObjectName.Equals(excluded) && !obj.Tag.Equals(excluded))
                            {
                                targets.Add(obj.ObjectName); //i target saranno tutti gli oggetti ad eccezione degli esclusi
                            }
                        }
                    }
                    else if (tags.Contains(tar))
                    {
                        foreach (var obj in fsm.ListOfObjects)
                        {
                            if (!obj.ObjectName.Equals(excluded) && obj.Tag.Equals(tar))
                            {
                                targets.Add(obj.ObjectName); //i target saranno tutti gli oggetti con il tag considerato 
                            }
                        }
                    }
                    else //il target ? un oggetto
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
                var t = action.ActionTarget.Split(':');
                var tar = t[0];
                var excluded = t.Length > 1 ? t[1] : null;

                if (tar.Equals("ALL") | tar.Equals("ANY"))
                {
                    foreach (var obj in fsm.ListOfObjects)
                    {
                        if (!obj.ObjectName.Equals(excluded) && !obj.Tag.Equals(excluded))
                        {
                            targets.Add(obj.ObjectName); 
                        }
                    }
                }
                else if (tags.Contains(tar))
                {
                    foreach (var obj in fsm.ListOfObjects)
                    {
                        if (!obj.ObjectName.Equals(excluded) && obj.Tag.Equals(tar))
                        {
                            targets.Add(obj.ObjectName); 
                        }
                    }
                }
                else //il target ? un oggetto
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
            foreach (var action in transition.TransitionInput)
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

