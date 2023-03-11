using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace AB.Model.FSM

{
    [Serializable]
    public class FSMModel
    {
        public string FSMName;
        public List<FSMObj> ListOfObjects;
        public string[] ListOfMaterials;
        public List<Media> ListOfMedia;
        public List<FSMState> ListOfStates;

        [Serializable]
        public class FSMObj
        {
            public string ObjectName; //equivalente a Gameobject.name
            public string Type;
            public Coord Position;
            public Coord Scale;
            public Coord Rotation;
            public string Material; 
            public string Tag; 
        }

        [Serializable]
        public class Coord
        {
            public float x;
            public float y;
            public float z;
        }


        [Serializable]
        public class Media
        {
            public string Name;
            public string Path;
            public string MediaType;
        }


        [Serializable]
        public class FSMState
        {
            public string Name;
            public List<FSMAction> ActionsOnEntry;
            public List<FSMAction> ActionsOnExit;
            public List<FSMTransition> ListOfTransitions;

            public bool InitialState;
            public bool FinalState;
        }

        //nota: non tutti i campi devono necessariamente comparire nel Json (se un campo è nullo posso anche non scriverlo) ad eccezione di External Condition
        [Serializable]
        public class FSMAction
        {
            public string FsmAction;
            public string Target; //struttura target: nome_target OPPURE parole chiave: any, all OPPURE nome_tag OPPURE una delle precedenti:una delle precedenti (':' significa execpt)
            public bool Triggered;
            public string Group; 
            public Parameters MovementParameters;
            public string AnimationClip; //passo il nome dell'animazione che voglio far partire
        }

        [Serializable]
        public class Parameters
        {
            public float MovementDuration;
            public float MovementSpeed;
            public Coord TargetCoord;
        }

        [Serializable]
        public class FSMTransition
        {
            public string Name;
            public string ExternalCondition;
            public List<FSMAction> ActionsOnTransition;
            public string NextState;
            public string SettingType;
        }


        public enum SettingType
        {
            AND,
            OR,
            ORDERED,
            TemporalTrigger
        }

        
        public enum Type
        {
            Animation,
            Element3D,
            Trigger,
            UI

        }

        public enum Group
        {
            Animation,
            Element3D,
            Trigger,
            UI
        }

        
        public enum FsmAction
        {
            //actionsOnEntry
            PlayAnimation,
            Rotate,
            Scaling,
            SetActive,
            SetInactive,
            SlowlyAppear,
            SlowlyDisappear,
            StartAnimation,
            Translate,
            TurnBlue,
            TurnRed,
            //ApplyTexture
            //StopAnimation
            //StopAudio
        
            //actionsOnTransition
            ButtonClick,  //click del bottone
            EnterTrigger, //entrata in una zona trigger (da accoppiare con exit trigger in cui si torna allo stato di partenza)
            ExitTrigger,
            MoveElement3DDown,
            MoveElement3DUp,
            TouchElement3D, //tocco di un oggetto 3D
            TriggerCollision //entrata in una zona trigger (effetto duraturo)
        }
        
        public enum MediaType
        {
            Animation,
            Audio
        }

    }
}