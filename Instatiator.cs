using AB.AnyAllModel;
using AB.Interactor;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AB.FSMManager.FSMManager;

namespace AB.Instatiator
{
    public class Instatiator : MonoBehaviour
    {
        
        public GameObject RoundButtonPrefab;
        public GameObject SquareButtonPrefab;
        public GameObject CubePrefab;
        public GameObject SpherePrefab;

        private GameObject o;

        public List<GameObject> ButtonObject  = new List<GameObject>();
        public List<GameObject> ManipulableObject = new List<GameObject>();
        public List<GameObject> SceneObject = new List<GameObject>();
        public List<GameObject> TriggerObject = new List<GameObject>();
        public static Instatiator Instance { get; private set; }

        //TODO: capire se si possono aggiungere tag a runtime nel progetto

        private void Awake()
        {
            FSM json = ParseJson("/Resources/Json/Allocate.json");
            Instance = this;

            CheckMaterialCoherence(json);
            foreach (var obj in json.ListOfObjects)
            {
                switch (obj.Tag) //da modificare questa parte
                {
                    case "SquareButton":
                        o = Instantiate(SquareButtonPrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.identity); //istanzia l'oggetto creando una copia di ButtonPrefab, alla posizione indicata, e con rotazione data
                        break;
                    case "RoundButton":
                        o = Instantiate(RoundButtonPrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.identity);
                        break;
                    case "CubeObject":
                        o = Instantiate(CubePrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.identity);
                        break;
                    case "SphereObject":
                        o = Instantiate(SpherePrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.identity);
                        break;
                    default:
                        break;
                }
                o.name = obj.ObjectName;
                o.transform.localScale = new Vector3(obj.Scale.x, obj.Scale.y, obj.Scale.z);
                o.tag = obj.Tag;
                o.SetActive(false);

                switch (obj.Type)
                {
                    case "ManipulableObject":
                        o.GetComponent<Renderer>().material = Resources.Load<Material>("Material/BaseMaterial/" + obj.Material);
                        o.AddComponent<ObjectManipulator>();
                        o.AddComponent<NearInteractionGrabbable>();
                        o.GetComponent<Collider>().isTrigger = true;
                        o.AddComponent<Rigidbody>();
                        o.GetComponent<Rigidbody>().isKinematic = true;

                        ManipulableObject.Add(o);
                        break;
                    case "TriggerObject":
                        o.GetComponent<Renderer>().material = Resources.Load<Material>("Material/BaseMaterial/" + obj.Material);
                        o.GetComponent<Collider>().isTrigger = true;
                        o.AddComponent<Rigidbody>();
                        o.GetComponent<Rigidbody>().isKinematic = true;
                        TriggerObject.Add(o);
                        break;
                    case "ButtonObject":
                        ButtonObject.Add(o);
                        break;
                    default:
                        break;
                }
                SceneObject.Add(o); 
                

            }
        }

        public void CheckMaterialCoherence(FSM json) //verifica se i materiali degli oggetti esistono nella lista
        {

            foreach (var obj in json.ListOfObjects)
            {
                if (obj.Type == "ManipulableObject")
                {
                    if (!json.ListOfMaterials.Contains(obj.Material))
                    {
                        throw new InvalidOperationException("A ManipulableObject cannot have a Material that isn't on the ListOfMaterials ");
                    }
                    
                }
            }
        }

        
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

