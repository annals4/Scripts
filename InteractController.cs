using Microsoft.MixedReality.Toolkit.UI;
using AB.AnyAllModel;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AB.FSMManager.FSMManager;
using Microsoft.MixedReality.Toolkit.Input;
using AB.Instatiator;

namespace AB.Interactor
{
    public class InteractController : MonoBehaviour
    {
        public static InteractController Instance { get; private set; } = new InteractController();
        public Action<GameObject> ButtonClick;
        public Instatiator.Instatiator instatiator;
        public Action<GameObject, Vector3> GrabbedObj;
        public Action<GameObject, Vector3> ReleasedGrabbedObj;
        public List<string> interactables = new List<string>(); //lista degli id degli oggetti interactables
        public List<string> manipulable = new List<string>();

        // Start is called before the first frame update
        void Start()
        {
            Instance = this;
            FSM json = ParseJson("/Resources/Json/Allocate.json");
            instatiator = Instatiator.Instatiator.Instance;

            //popolo le varie liste
            /*
            foreach (var obj in json.ListOfObjects)
            {
                switch (obj.Type)
                {
                    case "ButtonObject":
                        interactables.Add(obj.ObjectName);
                        break;
                    case "ManipulableObject":
                        manipulable.Add(obj.ObjectName); 
                        break;
                    default:
                        break;
                }
            }*/

        }
            //problema in listeners: esiste un bottone ma non è attivo, quindi non si riesce a trovare sulla scena
            //trovata forse soluzione, ma mi sa che devo richiamare questa cosa ogni volta che entro in una nuova scena, e penso che quindi dovrò fare una nuova funzione e richiamarla mano a mano
            
            
         public void Listeners()
         {
            //**************************************LISTENERS******************************************

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
         }
          
            
            
        


    }

}
