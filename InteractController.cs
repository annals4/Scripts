using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using static AB.Model.FSM.FSMModel;
using static AB.Manager.FSM.FSMManager;
using AB.Model.FSM;
using AB.Controller.Instatiator;

namespace AB.Controller.Interactor
{
    public class InteractController : MonoBehaviour
    {
        public static InteractController Instance { get; private set; } = new InteractController();
        public Action<GameObject> ButtonClick;
        public InstanceController instatiator;
        public Action<GameObject, Vector3> GrabbedObj;
        public Action<GameObject, Vector3> ReleasedGrabbedObj;
        public List<string> interactables = new List<string>(); //lista degli id degli oggetti interactables
        public List<string> manipulable = new List<string>();

        // Start is called before the first frame update
        void Start()
        {
            Instance = this;
            FSMModel json = ParseJson("/Resources/Json/Tester2.json");
            instatiator = InstanceController.Instance;

            

        }
            //problema in listeners: esiste un bottone ma non è attivo, quindi non si riesce a trovare sulla scena
            //trovata forse soluzione, ma mi sa che devo richiamare questa cosa ogni volta che entro in una nuova scena, e penso che quindi dovrò fare una nuova funzione e richiamarla mano a mano
            
            
         public void AddListener(GameObject o)
         {
            //**************************************LISTENERS******************************************

            if(o.GetComponent<PressableButtonHoloLens2>()!=null) //quindi si tratta di un bottone
            {
                o.GetComponent<Interactable>().OnClick
                        .AddListener(() => {
                            ButtonClick?.Invoke(o);
                            //TODO MANDARE REMOTO
                        });
            }
            else if (o.GetComponent<ObjectManipulator>() != null && o.GetComponent<NearInteractionGrabbable>() != null)
            {
                o.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((ManipulationEventData args) =>
                {
                    GrabbedObj?.Invoke(o, args.PointerCentroid); //notifies when an object is grabbed
                                                                   //da vedere se args può essere utile

                });
                o.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((ManipulationEventData args) =>
                {
                    ReleasedGrabbedObj?.Invoke(o, args.PointerCentroid); //notifies when an object is released
                });
            }
            /*
            //Listener ButtonClick
            foreach (var I in instatiator.ButtonObject)
            {
                if (I.activeSelf)
                {
                    GameObject temp = GameObject.Find(I.name);
                    temp.GetComponent<Interactable>().OnClick
                        .AddListener(() => {
                            ButtonClick?.Invoke(temp);
                            //TODO MANDARE REMOTO
                        });
                }
            }
            
            //Listener Grab
            foreach (var M in instatiator.ManipulableObject)
            {
                if (M.activeSelf)
                {
                    GameObject man = GameObject.Find(M.name);
                    man.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((ManipulationEventData args) =>
                    {
                        GrabbedObj?.Invoke(man, args.PointerCentroid); //notifies when an object is grabbed
                                                                       //da vedere se args può essere utile

                    });
                    man.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((ManipulationEventData args) =>
                    {
                        ReleasedGrabbedObj?.Invoke(man, args.PointerCentroid); //notifies when an object is released
                    });
                }

            }
            */
         }

        public void RemoveListener(GameObject o)
        {
            if (o.GetComponent<PressableButtonHoloLens2>() != null) //quindi si tratta di un bottone
            {
                o.GetComponent<Interactable>().OnClick
                        .RemoveListener(() => {
                            ButtonClick?.Invoke(o);
                            //TODO MANDARE REMOTO
                        });
            }
            else if (o.GetComponent<ObjectManipulator>() != null && o.GetComponent<NearInteractionGrabbable>() != null)
            {
                o.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveListener((ManipulationEventData args) =>
                {
                    GrabbedObj?.Invoke(o, args.PointerCentroid); //notifies when an object is grabbed
                                                                 //da vedere se args può essere utile

                });
                o.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveListener((ManipulationEventData args) =>
                {
                    ReleasedGrabbedObj?.Invoke(o, args.PointerCentroid); //notifies when an object is released
                });
            }
        }
          
            
            
        


    }

}
