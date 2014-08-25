﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;

public class IniParser
{
    private readonly string iniFilePath;
    private readonly Hashtable keyPairs = new Hashtable();
    private readonly List<SectionPair> tmpList = new List<SectionPair>();

    public readonly string Name;

    [ContractInvariantMethod]
    private void Invariant()
    {
        Contract.Invariant(!string.IsNullOrEmpty(iniFilePath));
        Contract.Invariant(keyPairs != null);
        Contract.Invariant(tmpList != null);
        Contract.Invariant(!string.IsNullOrEmpty(Name));
    }

    public IniParser(string iniPath)
    {
		Contract.Requires(!string.IsNullOrEmpty(iniPath));
        string str2 = null;
        this.iniFilePath = iniPath;
        this.Name = Path.GetFileNameWithoutExtension(iniPath);

        if (!File.Exists(iniPath)) throw new FileNotFoundException("Unable to locate " + iniPath);

        using (TextReader reader = new StreamReader(iniPath))
        {
            for (string str = reader.ReadLine(); str != null; str = reader.ReadLine())
            {
                str = str.Trim();
                if (str == "") continue;

                if (str.StartsWith("[") && str.EndsWith("]"))
                    str2 = str.Substring(1, str.Length - 2);
                else
                {
                    SectionPair pair;

                    if (str.StartsWith(";"))
                        str = str.Replace("=", "%eq%") + @"=%comment%";

                    string[] strArray = str.Split(new char[] {'='}, 2);
                    string str3 = null;
                    if (str2 == null)
                    {
                        str2 = "ROOT";
                    }
                    pair.Section = str2;
                    pair.Key = strArray[0];
                    if (strArray.Length > 1)
                    {
                        str3 = strArray[1];
                    }
                    this.keyPairs.Add(pair, str3);
                    this.tmpList.Add(pair);
                }
            }
        }
    }

    public void AddSetting(string sectionName, string settingName)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));
        Contract.Requires(!string.IsNullOrEmpty(settingName));

        this.AddSetting(sectionName, settingName, null);
    }

    public void AddSetting(string sectionName, string settingName, string settingValue)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));
        Contract.Requires(!string.IsNullOrEmpty(settingName));

        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (this.keyPairs.ContainsKey(pair))
        {
            this.keyPairs.Remove(pair);
        }
        if (this.tmpList.Contains(pair))
        {
            this.tmpList.Remove(pair);
        }
        this.keyPairs.Add(pair, settingValue);
        this.tmpList.Add(pair);
    }

    public int Count()
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        foreach (SectionPair pair in this.tmpList)
        {
            if (pair.Key.StartsWith(";"))  // don't count comments
                continue;

            if (!list.Contains(pair.Section))
            {
                list.Add(pair.Section);
            }
        }
        return list.Count;
    }

    public void DeleteSetting(string sectionName, string settingName)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));
        Contract.Requires(!string.IsNullOrEmpty(settingName));

        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (this.keyPairs.ContainsKey(pair))
        {
            this.keyPairs.Remove(pair);
            this.tmpList.Remove(pair);
        }
    }

    public string[] EnumSection(string sectionName)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));

        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        foreach (SectionPair pair in this.tmpList)
        {
            if (pair.Key.StartsWith(";"))
                continue;

            if (pair.Section == sectionName)
            {
                list.Add(pair.Key);
            }
        }
        return list.ToArray();
    }

    public string GetSetting(string sectionName, string settingName)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));
        Contract.Requires(!string.IsNullOrEmpty(settingName));

        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        return (string)this.keyPairs[pair];
    }

    public bool isCommandOn(string cmdName)
    {
        Contract.Requires(!string.IsNullOrEmpty(cmdName));

        string setting = this.GetSetting("Commands", cmdName);
        return ((setting == null) || (setting == "true"));
    }

    public void Save()
    {
        this.SaveSettings(this.iniFilePath);
    }

    public void SaveSettings(string newFilePath)
    {
        Contract.Requires(!string.IsNullOrEmpty(newFilePath));

        ArrayList list = new ArrayList();
        string str = "";
        string str2 = "";
        foreach (SectionPair pair in this.tmpList)
        {
            if (!list.Contains(pair.Section))
            {
                list.Add(pair.Section);
            }
        }
        foreach (string str3 in list)
        {
            str2 = str2 + "[" + str3 + "]\r\n";
            foreach (SectionPair pair2 in this.tmpList)
            {
                if (pair2.Section == str3)
                {
                    str = (string)this.keyPairs[pair2];
                    if (str != null) {
                        if (str == "%comment%") {
                            str = "";
                        } else {
                            str = "=" + str;
                        }
                    }
                    str2 = str2 + pair2.Key.Replace("%eq%", "=") + str + "\r\n";
                }
            }
            str2 = str2 + "\r\n";
        }

        using (TextWriter writer = new StreamWriter(newFilePath))
            writer.Write(str2);
    }

    public void SetSetting(string sectionName, string settingName, string value)
    {
        Contract.Requires(!string.IsNullOrEmpty(sectionName));
        Contract.Requires(!string.IsNullOrEmpty(settingName));

        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (this.keyPairs.ContainsKey(pair))
        {
            this.keyPairs[pair] = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SectionPair
    {
        public string Section;
        public string Key;
    }
}