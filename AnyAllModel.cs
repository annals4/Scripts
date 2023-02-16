using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace AB.AnyAllModel

{
    [Serializable]
    public class FSM
    {
        public string FSMName;
        public List<FSMObj> ListOfObjects;
        public string[] ListOfMaterials;
        public List<FSMState> ListOfStates;

        [Serializable]
        public class FSMObj
        {
            public string ObjectName; //equivalente a Gameobject.name
            public string Type;
            public Position Position;
            public Scale Scale;
            //public Coord Position, Scale, Rotation
            public string Material; 
            public string Tag; 
        }

        [Serializable]
        public class Position
        {
            public float x;
            public float y;
            public float z;
        }

        /*
        public class Coord
        {
            public float x;
            public float y;
            public float z;
        }
        */


        [Serializable]
        public class Scale
        {
            public float x;
            public float y;
            public float z;
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

        [Serializable]
        public class FSMAction
        {
            public string fsmAction;
            public string Target; //struttura target: nome_target OPPURE parole chiave: any, all OPPURE nome_tag OPPURE nome_ObjType OPPURE una delle precedenti/una delle precedenti ('/' significa execpt)
            public bool Triggered;
            public string TargetType; //****new
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
        public enum ObjType
        {
            PhysicalObject,
            UIObject,
            Animation
        }



    }
}