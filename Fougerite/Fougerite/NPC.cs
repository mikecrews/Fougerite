namespace Fougerite
{
    using System;
    using UnityEngine;

    public class NPC
    {
        private Character _char;
        private string _name;

        public NPC(Character c)
        {
            this._char = c;
            var index = c.name.IndexOf("(Clone)");
            var clone = c.name.Substring(0, index);
            if (clone.EndsWith("_A"))
            {
                this._name = clone.Replace("_A", "");
            }
            else if (clone.StartsWith("Mutant"))
            {
                this._name = clone.Replace("Mutant", "Mutant ");
            }
            else
            {
                this._name = clone;
            }
        }

        public void Kill()
        {
            this.Character.Signal_ServerCharacterDeath();
            this.Character.SendMessage("OnKilled", new DamageEvent(), SendMessageOptions.DontRequireReceiver);
        }

        public Character Character
        {
            get
            {
                return this._char;
            }
            set
            {
                this._char = value;
            }
        }

        public float Health
        {
            get
            {
                return this._char.health;
            }
            set
            {
                this._char.takeDamage.health = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
        }

        public Vector3 Location
        {
            get { return this._char.transform.position; }
        }
        public float X
        {
            get { return this._char.transform.position.x; }
        }
        public float Y
        {
            get { return this._char.transform.position.y; }
        }
        public float Z
        {
            get { return this._char.transform.position.z; }
        }
    }
}