namespace Fougerite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public class DataStore
    {
        public Hashtable datastore = new Hashtable();
        private static DataStore instance;
        public static string PATH = Path.Combine(Config.GetPublicFolder(), "FougeriteDatastore.ds");

        public static DataStore GetInstance()
        {
            if (instance == null)
            {
                instance = new DataStore();
            }
            return instance;
        }

        public void ToIni(string tablename, IniParser ini)
        {
            string nullref = "__NullReference__";
            Hashtable ht = (Hashtable)this.datastore[tablename];
            if (ht == null || ini == null)
                return;

            foreach (object key in ht.Keys)
            {
                string setting = key.ToString();
                string val = nullref;
                if (ht[setting] != null)
                {
                    var t = ht[setting].GetType();
                    if (t == typeof(Vector4)) 
                    {
                        val = string.Format("Vector4: {0}", ((Vector4)ht[setting]).ToString("R"));
                    } 
                    else if (t == typeof(Vector3))
                    {
                        val = string.Format("Vector3: {0}", ((Vector3)ht[setting]).ToString("R"));
                    }
                    else if (t == typeof(Vector2))
                    {
                        val = string.Format("Vector2: {0}", ((Vector2)ht[setting]).ToString("R"));
                    }
                    else if (t == typeof(Quaternion))
                    {
                        val = string.Format("Quaternion: {0}", ((Quaternion)ht[setting]).ToString("R"));
                    }
                    else if (t == typeof(Bounds))
                    {
                        val = string.Format("Bounds: {0}", ((Bounds)ht[setting]).ToString("R"));
                    }
                    else if (t == typeof(float))
                    {
                        val = ((float)ht[setting]).ToString("R");
                    }
                    else if (t == typeof(double))
                    {
                        val = ((double)ht[setting]).ToString("R");
                    }
                    else
                    {
                        val = ht[setting].ToString();
                    }
                }
                ini.AddSetting(tablename, setting, val);
            }
            ini.Save();
        }

        public void FromIni(IniParser ini)
        {
            foreach (string section in ini.Sections)
            {
                foreach (string key in ini.EnumSection(section))
                {
                    string setting = ini.GetSetting(section, key);
                    float valuef;
                    int valuei;
                    if (float.TryParse(setting, out valuef))
                    {
                        Add(section, key, valuef);
                    }
                    else if (int.TryParse(setting, out valuei))
                    {
                        Add(section, key, valuei);
                    }
                    else if (ini.GetBoolSetting(section, key))
                    {
                        Add(section, key, true);
                    }
                    else if (setting.Equals(bool.FalseString, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(section, key, false);
                    }
                    else if (setting == "__NullReference__")
                    {
                        Add(section, key, null);
                    }
                    else
                    {
                        int i = setting.IndexOf(':');
                        char[] rem = new char[] { '(', ')', ' ', ':', 'C', 'e', 'n', 't', 'r', 'E', 'x', 's' };
                        char[] split = new char[] { ',' };
                        string[] arr = setting.Remove(0, i).RemoveChars(rem).Split(split);
                        float x; float y; float z; float w; float xx; float yy; float zz;
                        switch (setting.Substring(0, i))
                        {
                            case "Vector2":
                                if (float.TryParse(arr[0], out x) && float.TryParse(arr[1], out y))
                                {
                                    Add(section, key, new Vector2(x, y));
                                }
                                else
                                {
                                    Add(section, key, Vector2.zero);
                                }
                                break;
                            case "Vector3":
                                if (float.TryParse(arr[0], out x) && float.TryParse(arr[1], out y) && float.TryParse(arr[2], out z))
                                {
                                    Add(section, key, new Vector3(x, y, z));
                                }
                                else
                                {
                                    Add(section, key, Vector3.zero);
                                }
                                break;
                            case "Vector4":
                                if (float.TryParse(arr[0], out x) && float.TryParse(arr[1], out y) && float.TryParse(arr[2], out z) && float.TryParse(arr[3], out w))
                                {
                                    Add(section, key, new Vector4(x, y, z, w));
                                }
                                else
                                {
                                    Add(section, key, Vector4.zero);
                                }
                                break;
                            case "Quaternion":
                                if (float.TryParse(arr[0], out x) && float.TryParse(arr[1], out y) && float.TryParse(arr[2], out z) && float.TryParse(arr[3], out w))
                                {
                                    Add(section, key, new Quaternion(x, y, z, w));
                                }
                                else
                                {
                                    Add(section, key, Quaternion.identity);
                                }
                                break;
                            case "Bounds":
                                if (float.TryParse(arr[0], out x) && float.TryParse(arr[1], out y) && float.TryParse(arr[2], out z)
                                    && float.TryParse(arr[3], out xx) && float.TryParse(arr[4], out yy) && float.TryParse(arr[5], out zz))
                                {
                                    Add(section, key, new Bounds(new Vector3(x, y, z), new Vector3(xx, yy, zz)));
                                }
                                else
                                {
                                    Add(section, key, new Bounds(Vector3.zero, Vector3.zero));
                                }
                                break;
                        }
                    }
                }
            }
        }

        public void Add(string tablename, object key, object val)
        {
            if (key == null)
                return;

            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
            {
                hashtable = new Hashtable();
                this.datastore.Add(tablename, hashtable);
            }
            hashtable[key] = val;
        }

        public bool ContainsKey(string tablename, object key)
        {
            if (key == null)
                return false;

            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return false;

            return hashtable.ContainsKey(key);
        }

        public bool ContainsValue(string tablename, object val)
        {
            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return false;

            return hashtable.ContainsValue(val);
        }

        public int Count(string tablename)
        {
            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
            {
                return 0;
            }
            return hashtable.Count;
        }

        public void Flush(string tablename)
        {
            if ((this.datastore[tablename] as Hashtable) != null)
            {
                this.datastore.Remove(tablename);
            }
        }

        public object Get(string tablename, object key)
        {
            if (key == null)
                return null;

            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            return hashtable[key];
        }

        public Hashtable GetTable(string tablename)
        {
            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            return hashtable;
        }

        public object[] Keys(string tablename)
        {
            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            object[] array = new object[hashtable.Keys.Count];
            hashtable.Keys.CopyTo(array, 0);
            return array;
        }

        public void Load()
        {
            if (File.Exists(PATH))
            {
                this.datastore = Util.HashtableFromFile(PATH); ;
                Util.GetUtil().ConsoleLog("Fougerite DataStore Loaded", false);
            }
        }

        public void Remove(string tablename, object key)
        {
            if (key == null)
                return;

            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable != null)
            {
                hashtable.Remove(key);
            }
        }

        public void Save()
        {
            if (this.datastore.Count != 0)
            {
                Util.HashtableToFile(this.datastore, PATH);
                Util.GetUtil().ConsoleLog("Fougerite DataStore Saved", false);
            }
        }

        public object[] Values(string tablename)
        {
            Hashtable hashtable = this.datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            object[] array = new object[hashtable.Values.Count];
            hashtable.Values.CopyTo(array, 0);
            return array;
        }
    }
}