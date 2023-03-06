using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AB.Model.FSM.FSMModel;
using static AB.Manager.FSM.FSMManager;
using AB.Model.FSM;

namespace AB.Controller.Instatiator
{
    public class InstanceController : MonoBehaviour
    {
        
        public GameObject RoundButtonPrefab;
        public GameObject SquareButtonPrefab;
        public GameObject CubePrefab;
        public GameObject SpherePrefab;
        public GameObject EmptyPrefab;

        private GameObject o;
        private AudioClip a;

        public List<GameObject> ButtonObject  = new List<GameObject>();
        public List<GameObject> ManipulableObject = new List<GameObject>();
        public List<GameObject> SceneObject = new List<GameObject>();
        public List<GameObject> TriggerObject = new List<GameObject>();
        public List<GameObject> AnimationObject = new List<GameObject>();
        public List<AudioClip> AudioObject = new List<AudioClip>();
        public AudioSource audioSource = new AudioSource();
        public static InstanceController Instance { get; private set; }

        //TODO: capire se si possono aggiungere tag a runtime nel progetto

        private void Awake()
        {
            FSMModel json = ParseJson("/Resources/Json/Tester2.json");
            Instance = this;

            CheckMaterialCoherence(json);
            foreach (var obj in json.ListOfObjects)
            {
                switch (obj.Tag) //da modificare questa parte
                {
                    case "SquareButton":
                        o = Instantiate(SquareButtonPrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.Euler(obj.Rotation.x, obj.Rotation.y, obj.Rotation.z)); //istanzia l'oggetto creando una copia di ButtonPrefab, alla posizione indicata, e con rotazione data
                        break;
                    case "RoundButton":
                        o = Instantiate(RoundButtonPrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.Euler(obj.Rotation.x, obj.Rotation.y, obj.Rotation.z));
                        break;
                    case "CubeObject":
                        o = Instantiate(CubePrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.Euler(obj.Rotation.x, obj.Rotation.y, obj.Rotation.z));
                        break;
                    case "SphereObject":
                        o = Instantiate(SpherePrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.Euler(obj.Rotation.x, obj.Rotation.y, obj.Rotation.z));
                        break;
                    //con il caso "Animation" carico un gameobject vuoto che poi nel momento in cui lo devo attivare sar� chiamato anche LoadGltfBinaryFromMemory();
                    case "AnimationObject":
                        o = Instantiate(EmptyPrefab, new Vector3(obj.Position.x, obj.Position.y, obj.Position.z), Quaternion.Euler(obj.Rotation.x, obj.Rotation.y, obj.Rotation.z));
                        break;
                    default:
                        break;
                }
                //di default la rotazione ha x,y e z pari a 0 (ottenuti con la Quaternion.identity));
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
                    case "AnimationObject":
                        AnimationObject.Add(o);
                        break;
                    default:
                        break;
                }
                SceneObject.Add(o); 

                
            }
            //alloca AudioClip
            foreach(var aud in json.ListOfMedia)
            {
                //creo l'audioclip
                a = Resources.Load<AudioClip>(aud.Path); //aud � il path dell'audio che carico
                a.name = aud.Name;
                AudioObject.Add(a);
                //creo l'oggetto che mi contiene l'audio source
                o = Instantiate(EmptyPrefab, new Vector3(0,0,0), Quaternion.identity);
                o.AddComponent<AudioSource>();
                audioSource = o.GetComponent<AudioSource>();

            }


        }

        public void CheckMaterialCoherence(FSMModel json) //verifica se i materiali degli oggetti esistono nella lista
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
