using System;
using System.ComponentModel;
using System.IO;
using Facepunch.Clocks.Counters;
using Google.ProtocolBuffers.Serialization;
using RustProto;
using RustProto.Helpers;
using UnityEngine;
using Avatar = RustProto.Avatar;

namespace Fougerite
{
    /// <summary>
    /// This class uses MonoBehaviour's invoke method, and using
    /// BackGroundWorker to make sure that the server won't lagg when the server
    /// has a huge amount of objects, and doesn't cause thread problems.
    /// Based on Salva's method, tested with a 59843 object count map.
    /// This is due to legacy's shitty saving system that caused a lot of troubles
    /// back in the official server's day too.
    /// </summary>
    public class ServerSaveHandler : MonoBehaviour
    {
        /// <summary>
        /// Returns the current saving filepath.
        /// </summary>
        public static string ServerSavePath
        {
            get;
            internal set;
        }
        
        /// <summary>
        /// Tells if the server is saving the map.
        /// </summary>
        public static bool ServerIsSaving
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or Sets the Server's Save time in minutes.
        /// DO NOT Set this to 0.
        /// </summary>
        public static int ServerSaveTime
        {
            get;
            set;
        }
        
        /// <summary>
        /// Saves the server without BackgroundWorker.
        /// </summary>
        public void ManualSave()
        {
            SaveServer(null, null);
        }

        /// <summary>
        /// Saves the server with BackgroundWorker.
        /// </summary>
        public void ManualBackGroundSave()
        {
            StartBackGroundWorker();
        }

        /// <summary>
        /// Runs when the component loaded.
        /// </summary>
        void Start()
        {
            ServerSavePath = ServerSaveManager.autoSavePath;
            Invoke(nameof(StartBackGroundWorker), ServerSaveTime * 60);
        }
        
        private void StartBackGroundWorker()
        {
            BackgroundWorker BGW = new BackgroundWorker();
            BGW.DoWork += new DoWorkEventHandler(SaveServer);
            BGW.RunWorkerAsync();
        }

        private void SaveServer(object sender, DoWorkEventArgs e)
        {
            if (ServerIsSaving)
            {
                Logger.LogDebug("[Fougerite WorldSave] Server's thread is still saving. We are ignoring the save request.");
                return;
            }
            ServerIsSaving = true;
            string path = ServerSaveManager.autoSavePath;
            
            AvatarSaveProc.SaveAll();
            
            DataStore.GetInstance().Save();
            SystemTimestamp restart = SystemTimestamp.Restart;
            if (path == string.Empty)
            {
                path = "savedgame.sav";
            }
            if (!path.EndsWith(".sav"))
            {
                path = path + ".sav";
            }
            if (ServerSaveManager._loading)
            {
                Logger.LogError("[Fougerite WorldSave] Currently loading, aborting save to " + path);
            }
            else
            {
                SystemTimestamp timestamp2;
                SystemTimestamp timestamp3;
                SystemTimestamp timestamp4;
                WorldSave fsave;
                Debug.Log("Saving to '" + path + "'");
                if (!ServerSaveManager._loadedOnce)
                {
                    if (File.Exists(path))
                    {
                        string[] textArray1 = new string[]
                        {
                            path, ".", ServerSaveManager.DateTimeFileString(File.GetLastWriteTime(path)), ".",
                            ServerSaveManager.DateTimeFileString(DateTime.Now), ".bak"
                        };
                        string destFileName = string.Concat(textArray1);
                        File.Copy(path, destFileName);
                        Logger.LogError("A save file exists at target path, but it was never loaded!\n\tbacked up:" +
                                        Path.GetFullPath(destFileName));
                    }

                    ServerSaveManager._loadedOnce = true;
                }

                ServerSaveManager s;
                WorldSave.Builder builder;
                using (Recycler<WorldSave, WorldSave.Builder> recycler = WorldSave.Recycler())
                {
                    builder = recycler.OpenBuilder();
                    timestamp2 = SystemTimestamp.Restart;
                    s = ServerSaveManager.Get(false);
                }

                s.DoSave(ref builder);
                timestamp2.Stop();
                timestamp3 = SystemTimestamp.Restart;
                fsave = builder.Build();
                timestamp3.Stop();
                int num = fsave.SceneObjectCount + fsave.InstanceObjectCount;
                if (save.friendly)
                {
                    using (FileStream stream = File.Open(path + ".json", FileMode.Create, FileAccess.Write))
                    {
                        JsonFormatWriter writer = JsonFormatWriter.CreateInstance(stream);
                        writer.Formatted();
                        writer.WriteMessage(fsave);
                    }
                }

                SystemTimestamp timestamp5 = timestamp4 = SystemTimestamp.Restart;
                using (FileStream stream2 = File.Open(path + ".new", FileMode.Create, FileAccess.Write))
                {
                    fsave.WriteTo(stream2);
                    stream2.Flush();
                }

                timestamp4.Stop();
                if (File.Exists(path + ".old.5"))
                {
                    File.Delete(path + ".old.5");
                }

                for (int i = 4; i >= 0; i--)
                {
                    if (File.Exists(path + ".old." + i))
                    {
                        File.Move(path + ".old." + i, path + ".old." + (i + 1));
                    }
                }

                if (File.Exists(path))
                {
                    File.Move(path, path + ".old.0");
                }

                if (File.Exists(path + ".new"))
                {
                    File.Move(path + ".new", path);
                }

                timestamp5.Stop();
                restart.Stop();
                if (Hooks.IsShuttingDown)
                {
                    ServerIsSaving = false;
                    return;
                }
                Loom.QueueOnMainThread(() =>
                {
                    if (save.profile)
                    {
                        object[] args = new object[]
                        {
                            num, timestamp2.ElapsedSeconds,
                            timestamp2.ElapsedSeconds / restart.ElapsedSeconds, timestamp3.ElapsedSeconds,
                            timestamp3.ElapsedSeconds / restart.ElapsedSeconds, timestamp4.ElapsedSeconds,
                            timestamp4.ElapsedSeconds / restart.ElapsedSeconds, timestamp5.ElapsedSeconds,
                            timestamp5.ElapsedSeconds / restart.ElapsedSeconds, restart.ElapsedSeconds,
                            restart.ElapsedSeconds / restart.ElapsedSeconds
                        };
                        Logger.Log(string.Format(
                            " Saved {0} Object(s) [times below are in elapsed seconds]\r\n  Logic:\t{1,-16:0.000000}\t{2,7:0.00%}\r\n  Build:\t{3,-16:0.000000}\t{4,7:0.00%}\r\n  Stream:\t{5,-16:0.000000}\t{6,7:0.00%}\r\n  All IO:\t{7,-16:0.000000}\t{8,7:0.00%}\r\n  Total:\t{9,-16:0.000000}\t{10,7:0.00%}",
                            args));
                    }
                    else
                    {
                        Logger.Log(string.Concat(new object[]
                            {" Saved ", num, " Object(s). Took ", restart.ElapsedSeconds, " seconds."}));
                    }
                    if (e != null && sender != null)
                    {
                        Invoke(nameof(StartBackGroundWorker), ServerSaveTime * 60);
                    }

                    Hooks.OnServerSaveEvent();
                });
                ServerIsSaving = false;
            }
        }
    }
}