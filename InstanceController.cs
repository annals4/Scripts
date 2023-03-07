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

        public List<GameObject> ListOfButtons  = new List<GameObject>();
        public List<GameObject> ListOfElement3D = new List<GameObject>();
        public List<GameObject> ListOfSceneObj = new List<GameObject>();
        public List<GameObject> ListOfTriggers = new List<GameObject>();
        public List<GameObject> ListOfAnimationObj = new List<GameObject>();
        public List<AudioClip> ListOfAudioClips = new List<AudioClip>();
        public AudioSource audioSource = new AudioSource();
        public static InstanceController Instance { get; private set; }

        //TODO: capire se si possono aggiungere tag a runtime nel progetto

        private void Awake()
        {
            FSMModel json = ParseJson("/Resources/Json/Tester2.json");
            Instance = this;

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
                    //con il caso "Animation" carico un gameobject vuoto che poi nel momento in cui lo devo attivare sarà chiamato anche LoadGltfBinaryFromMemory();
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
                    case "Element3D":
                        o.GetComponent<Renderer>().material = Resources.Load<Material>("Material/BaseMaterial/" + obj.Material);
                        o.AddComponent<ObjectManipulator>();
                        o.AddComponent<NearInteractionGrabbable>();
                        o.GetComponent<Collider>().isTrigger = true;
                        o.AddComponent<Rigidbody>();
                        o.GetComponent<Rigidbody>().isKinematic = true;

                        ListOfElement3D.Add(o);
                        break;
                    case "TriggerObject":
                        o.GetComponent<Renderer>().material = Resources.Load<Material>("Material/BaseMaterial/" + obj.Material);
                        o.GetComponent<Collider>().isTrigger = true;
                        o.AddComponent<Rigidbody>();
                        o.GetComponent<Rigidbody>().isKinematic = true;
                        ListOfTriggers.Add(o);
                        break;
                    case "ButtonObject":
                        ListOfButtons.Add(o);
                        break;
                    case "AnimationObject":
                        ListOfAnimationObj.Add(o);
                        break;
                    default:
                        break;
                }
                ListOfSceneObj.Add(o); 

                
            }
            //alloca AudioClip
            foreach(var aud in json.ListOfMedia)
            {
                //creo l'audioclip
                a = Resources.Load<AudioClip>(aud.Path); //aud è il path dell'audio che carico
                a.name = aud.Name;
                ListOfAudioClips.Add(a);
                //creo l'oggetto che mi contiene l'audio source
                o = Instantiate(EmptyPrefab, new Vector3(0,0,0), Quaternion.identity);
                o.AddComponent<AudioSource>();
                audioSource = o.GetComponent<AudioSource>();

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

