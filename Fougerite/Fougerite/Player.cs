namespace Fougerite
{
    using Facepunch.Utility;
    using Fougerite.Events;
    using Rust;
    using System;
    using System.Linq;
    using System.Timers;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using uLink;
    using UnityEngine;    

    public class Player
    {
        private long connectedAt;
        private PlayerInv inv;
        private bool invError;
        private bool justDied;
        private PlayerClient ourPlayer;
        private ulong uid;
        private string name;
        private string ipaddr;
        public static IDictionary<ulong, Fougerite.Player> Cache = new Dictionary<ulong, Fougerite.Player>();

        public Player()
        {
            this.justDied = true;
        }

        public Player(PlayerClient client)
        {
            this.justDied = true;
            this.ourPlayer = client;
            this.connectedAt = DateTime.UtcNow.Ticks;
            this.uid = client.netUser.userID;
            this.name = client.netUser.displayName;
            this.ipaddr = client.netPlayer.externalIP;
            this.FixInventoryRef();
        }

        public void OnConnect(NetUser user)
        {
            this.justDied = true;
            this.ourPlayer = user.playerClient;
            this.connectedAt = DateTime.UtcNow.Ticks;
            this.name = user.displayName;
            this.ipaddr = user.networkPlayer.externalIP;
            this.FixInventoryRef();
        }

        public void OnDisconnect()
        {
            this.justDied = false;
        }

        public void Disconnect()
        {
            if (this.IsOnline)
            {
                NetUser netUser = this.ourPlayer.netUser;
                if (netUser != null)
                {
                    if (netUser.connected)
                        netUser.Kick(NetError.NoError, true);
                }
            }
        }
        
        public void Damage(float dmg)
        {
            if (this.IsOnline)
            {
                TakeDamage.HurtSelf(this.PlayerClient.controllable.character, dmg);
            }
        }

        public bool IsOnline
        {
            get
            {
                if (this.ourPlayer != null)
                    if (this.ourPlayer.netUser != null)
                        return this.ourPlayer.netUser.connected == true;

                return false;
            }
        }

        public Fougerite.Player Find(string search)
        {
            return Search(search);
        }

        public static Fougerite.Player Search(string search)
        {
            IEnumerable<Fougerite.Player> query;
            if (search.StartsWith("7656119"))
            {
                ulong uid;
                if (ulong.TryParse(search, out uid))
                {
                    if (Cache.ContainsKey(uid))
                        return Cache[uid];
                }
                else
                {
                    query = from player in Cache.Values
                            group player by search.Similarity(player.SteamID) into match
                            orderby match.Key descending
                            select match.FirstOrDefault();

                    Logger.LogDebug(string.Format("[Player.Search] search={0} matches={1}", search, string.Join(", ", query.Select(p => p.SteamID).ToArray<string>())));
                    return query.FirstOrDefault();
                }
            }
            query = from player in Cache.Values
                    group player by search.Similarity(player.Name) into match
                    orderby match.Key descending
                    select match.FirstOrDefault();

            Logger.LogDebug(string.Format("[FindPlayer] search={0} matches={1}", search, string.Join(", ", query.Select(p => p.Name).ToArray<string>())));
            return query.FirstOrDefault();
        }

        public static Fougerite.Player FindBySteamID(string search)
        {
            return Search(search);
        }

        public static Fougerite.Player FindByGameID(string search)
        {
            return Search(search);
        }

        public static Fougerite.Player FindByName(string search)
        {
            return Search(search);
        }

        public static Fougerite.Player FindByNetworkPlayer(uLink.NetworkPlayer np)
        {
            var query = from player in Fougerite.Server.GetServer().Players
                                 where player.PlayerClient.netPlayer == np
                                 select player;
            return query.FirstOrDefault();
        }

        public static Fougerite.Player FindByPlayerClient(PlayerClient pc)
        {
            var query = from player in Fougerite.Server.GetServer().Players
                        where player.PlayerClient == pc
                        select player;
            return query.FirstOrDefault();
        }

        public void FixInventoryRef()
        {
            Hooks.OnPlayerKilled += new Hooks.KillHandlerDelegate(this.Hooks_OnPlayerKilled);
        }

        public bool HasBlueprint(BlueprintDataBlock dataBlock)
        {
            if (this.IsOnline)
            {
                PlayerInventory invent = this.Inventory.InternalInventory as PlayerInventory;
                if (invent.KnowsBP(dataBlock))
                    return true;
            }
            return false;
        }

        private void Hooks_OnPlayerKilled(DeathEvent de)
        {
            try
            {
                Fougerite.Player victim = de.Victim as Fougerite.Player;
                if (victim.UID == this.UID)
                {
                    this.justDied = true;
                }
            }
            catch
            {
                this.invError = true;
            }
        }

        public void InventoryNotice(string arg)
        {
            if (this.IsOnline)
                Rust.Notice.Inventory(this.ourPlayer.netPlayer, arg);
        }

        public void Kill()
        {
            if (this.IsOnline)
                TakeDamage.KillSelf(this.ourPlayer.controllable.character, null);
        }

        public void Message(string arg)
        {
            if (this.IsOnline)
                this.SendCommand(string.Format("chat.add {0} {1}", Fougerite.Server.GetServer().server_message_name.QuoteSafe(), arg.QuoteSafe()));
        }

        public void MessageFrom(string playername, string arg)
        {
            if (this.IsOnline)
                this.SendCommand(string.Format("chat.add {0} {1}", playername.QuoteSafe(), arg.QuoteSafe()));
        }

        public void Notice(string arg)
        {
            if (this.IsOnline)
                Rust.Notice.Popup(this.ourPlayer.netPlayer, "!", arg, 4f);
        }

        public void Notice(string icon, string text, float duration = 4f)
        {
            if (this.IsOnline)
                Rust.Notice.Popup(this.ourPlayer.netPlayer, icon, text, duration);
        }

        public void SendCommand(string cmd)
        {
            if (this.IsOnline)
                ConsoleNetworker.SendClientCommand(this.ourPlayer.netPlayer, cmd);
        }

        public bool TeleportTo(Fougerite.Player p)
        {
            if (this.IsOnline)
                return this.TeleportTo(p, 1.5f);

            return false;
        }

        public bool TeleportTo(Fougerite.Player p, float distance = 1.5f)
        {
            if (this.IsOnline)
            {
                if (this == p) // lol
                    return false;

                Transform transform = p.PlayerClient.controllable.transform;                                            // get the target player's transform
                Vector3 target = transform.TransformPoint(new Vector3(0f, 0f, (this.Admin ? -distance : distance)));    // rcon admin teleports behind target player
                return this.SafeTeleportTo(target);
            }
            return false;
        }

        public bool SafeTeleportTo(float x, float y, float z)
        {
            if (this.IsOnline)
                return this.SafeTeleportTo(new Vector3(x, y, z));

            return false;
        }

        public bool SafeTeleportTo(float x, float z)
        {
            if (this.IsOnline)
                return this.SafeTeleportTo(new Vector3(x, 0f, z));

            return false;
        }

        public bool SafeTeleportTo(Vector3 target)
        {
            if (this.IsOnline)
            {
                float maxSafeDistance = 360f;
                float seaLevel = 256f;
                double ms = 500d;
                string me = "SafeTeleport";

                float bumpConst = 0.75f;
                Vector3 bump = Vector3.up * bumpConst;
                Vector3 terrain = new Vector3(target.x, Terrain.activeTerrain.SampleHeight(target), target.z);
                RaycastHit hit;
                IEnumerable<StructureMaster> structures = from s in StructureMaster.AllStructures
                                                          where s.containedBounds.Contains(terrain)
                                                          select s;
                if (terrain.y > target.y)
                    target = terrain + bump * 2;

                if (structures.Count() == 1)
                {
                    if (Physics.Raycast(target, Vector3.down, out hit))
                    {
                        if (hit.collider.name == "HB Hit")
                        {
                            // this.Message("There you are.");
                            return false;
                        }
                    }
                    StructureMaster structure = structures.FirstOrDefault<StructureMaster>();
                    if (!structure.containedBounds.Contains(target) || hit.distance > 8f)
                        target = hit.point + bump;

                    float distance = Vector3.Distance(this.Location, target);

                    if (distance < maxSafeDistance)
                    {
                        return this.TeleportTo(target);
                    }
                    else
                    {
                        if (this.TeleportTo(terrain + bump * 2))
                        {
                            System.Timers.Timer timer = new System.Timers.Timer();
                            timer.Interval = ms;
                            timer.AutoReset = false;
                            timer.Elapsed += delegate(object x, ElapsedEventArgs y)
                            {
                                this.TeleportTo(target);
                            };
                            timer.Start();
                            return true;
                        }
                        return false;
                    }
                }
                else if (structures.Count() == 0)
                {
                    if (terrain.y < seaLevel)
                    {
                        this.Message("That would put you in the ocean.");
                        return false;
                    }

                    if (Physics.Raycast(terrain + Vector3.up * 300f, Vector3.down, out hit))
                    {
                        if (hit.collider.name == "HB Hit")
                        {
                            this.Message("There you are.");
                            return false;
                        }
                        Vector3 worldPos = target - Terrain.activeTerrain.transform.position;
                        Vector3 tnPos = new Vector3(Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.x, worldPos.x), 0, Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.z, worldPos.z));
                        float gradient = Terrain.activeTerrain.terrainData.GetSteepness(tnPos.x, tnPos.z);
                        if (gradient > 50f)
                        {
                            this.Message("It's too steep there.");
                            return false;
                        }
                        target = hit.point + bump * 2;
                    }
                    float distance = Vector3.Distance(this.Location, target);
                    Logger.LogDebug(string.Format("[{0}] player={1}({2}) from={3} to={4} distance={5} terrain={6}", me, this.Name, this.GameID,
                        this.Location.ToString(), target.ToString(), distance.ToString("F2"), terrain.ToString()));

                    return this.TeleportTo(target);
                }
                else
                {
                    Logger.LogDebug(string.Format("[{0}] structures.Count is {1}. Weird.", me, structures.Count().ToString()));
                    Logger.LogDebug(string.Format("[{0}] target={1} terrain{2}", me, target.ToString(), terrain.ToString()));
                    this.Message("Cannot execute safely with the parameters supplied.");
                    return false;
                }
            }
            return false;
        }

        public bool TeleportTo(float x, float y, float z)
        {
            if (this.IsOnline)
                return this.TeleportTo(new Vector3(x, y, z));

            return false;
        }

        public bool TeleportTo(Vector3 target)
        {
            if (this.IsOnline)
                return RustServerManagement.Get().TeleportPlayerToWorld(this.ourPlayer.netPlayer, target);

            return false;
        }

        public bool Admin
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.netUser.admin;

                return false;
            }
        }

        public ulong UID
        {
            get
            {
                return this.uid;
            }
        }

        public string GameID
        {
            get
            {
                return this.uid.ToString();
            }
        }

        public string SteamID
        {
            get
            {
                return this.uid.ToString();
            }
        }

        public float Health
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.controllable.health;

                return 0f;
            }
            set
            {
                if (!this.IsOnline)
                    return;

                if (value < 0f)
                {
                    this.ourPlayer.controllable.takeDamage.health = 0f;
                }
                else
                {
                    this.ourPlayer.controllable.takeDamage.health = value;
                }
                this.ourPlayer.controllable.takeDamage.Heal(this.ourPlayer.controllable, 0f);
            }
        }

        public PlayerInv Inventory
        {
            get
            {
                if (!this.IsOnline)
                    return (PlayerInv)null;

                if (this.invError || this.justDied)
                {
                    this.inv = new PlayerInv(this);
                    this.invError = false;
                    this.justDied = false;
                }
                return this.inv;
            }
        }

        public string IP
        {
            get
            {
                return this.ipaddr;
            }
        }

        public bool IsBleeding
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.controllable.GetComponent<HumanBodyTakeDamage>().IsBleeding();

                return false;
            }
        }

        public bool IsCold
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.controllable.GetComponent<Metabolism>().IsCold();

                return false;
            }
        }

        public bool IsInjured
        {
            get
            {
                if (this.IsOnline)
                    return (this.ourPlayer.controllable.GetComponent<FallDamage>().GetLegInjury() != 0f);

                return false;
            }
        }

        public bool IsRadPoisoned
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().HasRadiationPoisoning();

                return false;
            }
        }

        public bool IsWarm
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().IsWarm();

                return false;
            }
        }

        public bool IsPoisoned
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().IsPoisoned();

                return false;
            }
        }

        public bool IsStarving
        {
            get
            {
                if (this.IsOnline)
                    return this.CalorieLevel <= 0.0;

                return false;
            }
        }

        public bool IsHungry
        {
            get
            {
                if (this.IsOnline)
                    return this.CalorieLevel < 500.0;

                return false;
            }
        }

        public float CoreTemperature
        {
            get 
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature;

                return 0f;
            }
            set
            {
                if (this.IsOnline)
                    this.PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature = value;
            }
        }

        public float BleedingLevel
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<HumanBodyTakeDamage>()._bleedingLevel;

                return 0f;
            }
        }

        public float CalorieLevel
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().GetCalorieLevel();

                return 0f;
            }
        }

        public void AdjustCalorieLevel(float amount)
        {
            if (!this.IsOnline)
                return;

            if (amount < 0)
                this.PlayerClient.controllable.GetComponent<Metabolism>().SubtractCalories(Math.Abs(amount));

            if (amount > 0)
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddCalories(amount);
        }

        public float RadLevel
        {
            get
            {
                if (this.IsOnline)
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().GetRadLevel();

                return 0f;
            }
        }

        public void AddRads(float amount)
        {
            if (this.IsOnline)
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddRads(amount);
        }

        public void AddAntiRad(float amount)
        {
            if (this.IsOnline)
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddAntiRad(amount);
        }

        public void AddWater(float litres)
        {
            if (this.IsOnline)
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddWater(litres);
        }

        public void AdjustPoisonLevel(float amount)
        {
            if (!this.IsOnline)
                return;

            if (amount < 0)
                this.PlayerClient.controllable.GetComponent<Metabolism>().SubtractPosion(Math.Abs(amount));

            if (amount > 0)
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddPoison(amount);
        }

        public Vector3 Location
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.lastKnownPosition;

                return Vector3.zero;
            }
        }

        public string Name
        {
            get
            {
                return this.name; // displayName
            }
            set
            {
                this.name = value;
                if (this.IsOnline)
                {
                    this.ourPlayer.netUser.user.displayname_ = value; // displayName
                    this.ourPlayer.userName = value; // displayName
                }
            }
        }

        public Entity Sleeper
        {
            get
            {
                if (this.IsOnline)
                    return (Entity)null;

                var query = from sleeper in UnityEngine.Object.FindObjectsOfType<SleepingAvatar>()
                            let deployable = sleeper.GetComponent<DeployableObject>()
                            where deployable.ownerID == this.uid
                            select new Entity(deployable);

                return query.FirstOrDefault();
            }
        }

        public bool AtHome
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.Structures.Any(e => (e.Object as StructureMaster).containedBounds.Contains(this.Location));
                }
                else if (this.Sleeper != null)
                {
                    return this.Structures.Any(e => (e.Object as StructureMaster).containedBounds.Contains(this.Sleeper.Location));
                }
                return false;
            }
        }

        public int Ping
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer.netPlayer.averagePing;

                return int.MaxValue;
            }
        }

        public PlayerClient PlayerClient
        {
            get
            {
                if (this.IsOnline)
                    return this.ourPlayer;

                return (PlayerClient)null;
            }
        }

        public long TimeOnline
        {
            get
            {
                if (this.IsOnline)
                    return ((DateTime.UtcNow.Ticks - this.connectedAt) / 0x2710L);

                return 0L;
            }
        }

        public float X
        {
            get
            {
                return this.Location.x;
            }
            set
            {
                if (this.IsOnline)
                    this.ourPlayer.transform.position.Set(value, this.Y, this.Z);
            }
        }

        public float Y
        {
            get
            {
                return this.Location.y;
            }
            set
            {
                if (this.IsOnline) 
                    this.ourPlayer.transform.position.Set(this.X, value, this.Z);
            }
        }

        public float Z
        {
            get
            {
                return this.Location.z;
            }
            set
            {
                if (this.IsOnline) 
                    this.ourPlayer.transform.position.Set(this.X, this.Y, value);
            }
        }

        private static Fougerite.Entity[] QueryToEntity<T>(IEnumerable<T> query)
        {
            Fougerite.Entity[] these = new Fougerite.Entity[query.Count<T>()];
            for (int i = 0; i < these.Length; i++)
            {
                these[i] = new Fougerite.Entity((query.ElementAtOrDefault<T>(i) as UnityEngine.Component).GetComponent<DeployableObject>());
            }
            return these;
        }

        public Fougerite.Entity[] Structures
        {
            get
            {
                var query = from s in StructureMaster.AllStructures
                            where this.UID == s.ownerID
                            select s;
                Fougerite.Entity[] these = new Fougerite.Entity[query.Count()];
                for (int i = 0; i < these.Length; i++)
                {
                    these[i] = new Fougerite.Entity(query.ElementAtOrDefault(i));
                }
                return these;
            }
        }

        public Fougerite.Entity[] Deployables
        {
            get
            {
                var query = from d in UnityEngine.Object.FindObjectsOfType(typeof(DeployableObject)) as DeployableObject[]
                            where this.UID == d.ownerID
                            select d;
                return QueryToEntity<DeployableObject>(query);
            }
        }

        public Fougerite.Entity[] Shelters
        {
            get
            {
                var query = from d in UnityEngine.Object.FindObjectsOfType(typeof(DeployableObject)) as DeployableObject[]
                            where d.name.Contains("Shelter") && this.UID == d.ownerID
                            select d;
                return QueryToEntity<DeployableObject>(query);
            }
        }

        public Fougerite.Entity[] Storage
        {
            get
            {
                var query = from s in UnityEngine.Object.FindObjectsOfType(typeof(SaveableInventory)) as SaveableInventory[]
                            where this.UID == (s.GetComponent<DeployableObject>() as DeployableObject).ownerID
                            select s;
                return QueryToEntity<SaveableInventory>(query);
            }
        }

        public Fougerite.Entity[] Fires
        {
            get
            {
                var query = from f in UnityEngine.Object.FindObjectsOfType(typeof(FireBarrel)) as FireBarrel[]
                            where this.UID == (f.GetComponent<DeployableObject>() as DeployableObject).ownerID
                            select f;
                return QueryToEntity<FireBarrel>(query);
            }
        }
    }
}
