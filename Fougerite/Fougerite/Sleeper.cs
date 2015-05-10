using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fougerite
{
    public class Sleeper
    {
        private DeployableObject _sleeper;

        public Sleeper(DeployableObject obj)
        {
            this._sleeper = obj;
        }

        public DeployableObject Object
        {
            get { return this._sleeper; }
        }

        public string OwnerID
        {
            get { return Object.ownerID.ToString(); }
        }

        public string OwnerName
        {
            get { return Object.ownerName; }
        }

        public Vector3 Location
        {
            get { return Object.transform.position; }
        }

        public float X
        {
            get { return Object.transform.position.x; }
        }

        public float Y
        {
            get { return Object.transform.position.y; }
        }

        public float Z
        {
            get { return Object.transform.position.z; }
        }

        public int InstanceID
        {
            get
            {
                return Object.GetInstanceID();
            }
        }
    }
}
