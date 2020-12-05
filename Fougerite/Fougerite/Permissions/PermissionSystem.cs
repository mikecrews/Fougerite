using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fougerite.Permissions
{
    /// <summary>
    /// The heart of the permission system.
    /// I recommend using groups, and assigning players to them.
    /// TODO: Implement hooks?
    /// TODO: Support for x.*
    /// </summary>
    public class PermissionSystem
    {
        private static PermissionSystem _instance;
        private static readonly object _obj = new object();
        private static readonly object _obj2 = new object();
        private readonly PermissionHandler _handler;
        private readonly Dictionary<ulong, bool> _disabledpermissions = new Dictionary<ulong, bool>();

        /// <summary>
        /// PermissionSystem is a Singleton.
        /// </summary>
        private PermissionSystem()
        {
            _handler = new PermissionHandler();
            ReloadPermissions();
        }

        /// <summary>
        /// Temporarily remove all permissions of a player.
        /// Lasts until the server restarts, or removed manually.
        /// removeDefaultGroupPermissions true removes the default
        /// group's as well.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="removeDefaultGroupPermissions"></param>
        /// <returns></returns>
        public bool ForceOffPermissions(ulong steamid, bool removeDefaultGroupPermissions)
        {
            lock (_obj2)
            {
                if (!_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Add(steamid, removeDefaultGroupPermissions);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Removes the temporarily added effect.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool RemoveForceOffPermissions(ulong steamid)
        {
            lock (_obj2)
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Remove(steamid);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if the player has its permissions forced off.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool HasPermissionsForcedOff(ulong steamid)
        {
            lock (_obj2)
            {
                return _disabledpermissions.ContainsKey(steamid);
            }
        }

        /// <summary>
        /// Checks if a player has its permissions forced off and the
        /// default permissions as well.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool HasDefaultPermissionsForcedOff(ulong steamid)
        {
            lock (_obj2)
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    return _disabledpermissions[steamid];
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the dictionary of forced off permissions.
        /// </summary>
        public Dictionary<ulong, bool> DisabledPermissions
        {
            get
            {
                lock (_obj2)
                {
                    return _disabledpermissions;
                }
            }
        }

        /// <summary>
        /// Gets the unique identifier of a string based on MD5.
        /// This is used for group names.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetUniqueID(string value)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var hashed = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToInt32(hashed, 0);
            }
        }

        /// <summary>
        /// Reloads the permissions.
        /// </summary>
        public void ReloadPermissions()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                List<PermissionPlayer> emptyplayers = new List<PermissionPlayer>();
                List<PermissionGroup> emptygroups = new List<PermissionGroup>();

                if (!File.Exists(Util.GetRootFolder() + "\\Save\\GroupPermissions.json"))
                {
                    File.Create(Util.GetRootFolder() + "\\Save\\GroupPermissions.json").Dispose();
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Default",
                        GroupPermissions = new List<string>() {"DoNotDeleteTheDefaultGroup", "Something"},
                        NickName = "Default nick name"
                    });
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Group1", 
                        GroupPermissions = new List<string>() {"GroupPermission1"},
                        NickName = "Nice nick name"
                    });
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Group2",
                        GroupPermissions = new List<string>() {"GroupPermission2.gar", "GroupPermission2.something"},
                        NickName = "SomeNickname"
                    });

                    using (StreamWriter sw =
                        new StreamWriter(Util.GetRootFolder() + "\\Save\\GroupPermissions.json", false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptygroups);
                        }
                    }
                }

                if (!File.Exists(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json"))
                {
                    File.Create(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json").Dispose();

                    emptyplayers.Add(new PermissionPlayer()
                    {
                        SteamID = 76562531000,
                        Permissions = new List<string>()
                            {"*", "Permission1", "Permission2.something", "Permission3"},
                        Groups = new List<string>() {"Group1"}
                    });

                    using (StreamWriter sw =
                        new StreamWriter(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json", false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptyplayers);
                        }
                    }
                }

                lock (_obj)
                {
                    _handler.PermissionGroups =
                        JsonConvert.DeserializeObject<List<PermissionGroup>>(
                            File.ReadAllText(Util.GetRootFolder() + "\\Save\\GroupPermissions.json"));
                    _handler.PermissionPlayers =
                        JsonConvert.DeserializeObject<List<PermissionPlayer>>(
                            File.ReadAllText(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json"));
                }

                Logger.Log("[PermissionSystem] Loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError("[PermissionSystem] Error: " + ex);
            }
        }

        /// <summary>
        /// Returns the permission system class.
        /// </summary>
        /// <returns></returns>
        public static PermissionSystem GetPermissionSystem()
        {
            if (_instance == null)
            {
                _instance = new PermissionSystem();
            }

            return _instance;
        }

        /// <summary>
        /// Tries to save the data from the memory to disk.
        /// If any failure occurs It will revert to the current state.
        /// </summary>
        public void SaveToDisk()
        {
            string grouppermissions = "";
            string playerpermissions = "";

            try
            {
                if (!File.Exists(Util.GetRootFolder() + "\\Save\\GroupPermissions.json"))
                {
                    File.Create(Util.GetRootFolder() + "\\Save\\GroupPermissions.json").Dispose();
                }

                if (!File.Exists(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json"))
                {
                    File.Create(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json").Dispose();
                }

                // Backup the data from the current files.
                grouppermissions = File.ReadAllText(Util.GetRootFolder() + "\\Save\\GroupPermissions.json");
                playerpermissions = File.ReadAllText(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json");

                // Empty the files.
                if (File.Exists(Util.GetRootFolder() + "\\Save\\GroupPermissions.json"))
                {
                    File.WriteAllText(Util.GetRootFolder() + "\\Save\\GroupPermissions.json", string.Empty);
                }

                if (File.Exists(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json"))
                {
                    File.WriteAllText(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json", string.Empty);
                }

                // Initialize empty list just in case.
                List<PermissionGroup> PermissionGroups = new List<PermissionGroup>();
                List<PermissionPlayer> PermissionPlayers = new List<PermissionPlayer>();

                // Grab the data from the memory using lock.
                lock (_obj)
                {
                    PermissionGroups = _handler.PermissionGroups;
                    PermissionPlayers = _handler.PermissionPlayers;
                }

                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;

                using (StreamWriter sw =
                    new StreamWriter(Util.GetRootFolder() + "\\Save\\GroupPermissions.json", false,
                        Encoding.UTF8))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, PermissionGroups);
                    }
                }

                using (StreamWriter sw =
                    new StreamWriter(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json", false,
                        Encoding.UTF8))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, PermissionPlayers);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[PermissionSystem] SaveToDisk Error: " + ex);
                File.WriteAllText(Util.GetRootFolder() + "\\Save\\GroupPermissions.json", grouppermissions);
                File.WriteAllText(Util.GetRootFolder() + "\\Save\\PlayerPermissions.json", playerpermissions);
            }
        }

        /// <summary>
        /// Returns all the existing groups.
        /// </summary>
        /// <returns></returns>
        public List<PermissionGroup> GetPermissionGroups()
        {
            lock (_obj)
            {
                return _handler.PermissionGroups;
            }
        }
        
        /// <summary>
        /// Returns all the players that exist in the permission database.
        /// This might be a large list depending on how many players you have added to It.
        /// </summary>
        /// <returns></returns>
        public List<PermissionPlayer> GetPermissionPlayers()
        {
            lock (_obj)
            {
                return _handler.PermissionPlayers;
            }
        }

        /// <summary>
        /// Tries to find the group by name.
        /// Returns null if doesn't exist.
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public PermissionGroup GetGroupByName(string groupname)
        {
            groupname = groupname.Trim().ToLower();
            int uniqueid = GetUniqueID(groupname);
            lock (_obj)
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == uniqueid);
            }
        }
        
        /// <summary>
        /// Tries to find the group by name.
        /// Returns null if doesn't exist.
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public PermissionGroup GetGroupByID(int groupid)
        {
            lock (_obj)
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == groupid);
            }
        }

        /// <summary>
        /// Tries to find the player's permissions.
        /// Returns null if doesn't exist.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public PermissionPlayer GetPlayerBySteamID(ulong steamid)
        {
            lock (_obj)
            {
                return _handler.PermissionPlayers.FirstOrDefault(x => x.SteamID == steamid);
            }
        }
        
        /// <summary>
        /// Tries to find the player's permissions.
        /// Returns null if doesn't exist.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PermissionPlayer GetPlayerBySteamID(Fougerite.Player player)
        {
            if (player == null)
            {
                return null;
            }
            
            lock (_obj)
            {
                return _handler.PermissionPlayers.FirstOrDefault(x => x.SteamID == player.UID);
            }
        }

        public bool PlayerHasGroup(Fougerite.Player player, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            if (groupname == "default")
            {
                return true;
            }

            int id = GetUniqueID(groupname);
            
            if (player == null)
            {
                return false;
            }

            var permissionplayer = GetPlayerBySteamID(player);
            if (permissionplayer == null)
            {
                return false;
            }
            
            return permissionplayer.Groups.Any(x => GetUniqueID(x.Trim().ToLower()) == id);
        }
        
        public bool PlayerHasGroup(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            if (groupname == "default")
            {
                return true;
            }

            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                return false;
            }
            
            return permissionplayer.Groups.Any(x => x.Trim().ToLower() == groupname);
        }
        
        public bool PlayerHasGroup(PermissionPlayer permissionplayer, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            if (groupname == "default")
            {
                return true;
            }
            
            if (permissionplayer == null)
            {
                return false;
            }

            return permissionplayer.Groups.Any(x => x.Trim().ToLower() == groupname);
        }
        
        public bool PlayerHasPermission(Fougerite.Player player, string permission)
        {
            if (player == null)
            {
                return false;
            }

            // Check if permissions were revoked.
            if (HasDefaultPermissionsForcedOff(player.UID))
            {
                return false;
            }
            
            permission = permission.Trim().ToLower();

            var permissionplayer = GetPlayerBySteamID(player);
            // Player has no specific permissions, or groups. Check for the default group.
            // This is gonna apply to most of the players of the server.
            if (permissionplayer == null)
            {
                PermissionGroup defaul = GetGroupByName("Default");
                if (defaul != null)
                {
                    bool haspermission = defaul.GroupPermissions.Any(x => x.Trim() == "*" || x.Trim().ToLower() == permission);
                    if (haspermission) return true;
                }
                
                return false;
            }
            
            // Check if permissions were revoked, but without default permissions.
            if (HasPermissionsForcedOff(player.UID))
            {
                return false;
            }
            
            foreach (PermissionGroup @group in permissionplayer.Groups.Select(GetGroupByName))
            {
                bool haspermission = group.GroupPermissions.Any(x => x.Trim() == "*" || x.Trim().ToLower() == permission);
                if (haspermission) return true;
            }

            foreach (var x in permissionplayer.Permissions)
            {
                string pn = x.Trim().ToLower();
                if (pn == "*")
                {
                    return true;
                }
                
                if (pn == permission)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CreatePermissionPlayer(Fougerite.Player player)
        {
            if (player == null)
            {
                return false;
            }
            
            var permissionplayer = GetPlayerBySteamID(player);
            if (permissionplayer == null)
            {
                lock (_obj)
                {
                    _handler.PermissionPlayers.Add(new PermissionPlayer()
                    {
                        Groups = new List<string>(),
                        Permissions = new List<string>(),
                        SteamID = player.UID
                    });
                    return true;
                }
            }

            return false;
        }
        
        public bool CreatePermissionPlayer(ulong steamid)
        {
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                lock (_obj)
                {
                    _handler.PermissionPlayers.Add(new PermissionPlayer()
                    {
                        Groups = new List<string>(),
                        Permissions = new List<string>(),
                        SteamID = steamid
                    });
                    return true;
                }
            }

            return false;
        }

        public bool RemovePermissionPlayer(Fougerite.Player player)
        {
            if (player == null)
            {
                return false;
            }
            
            var permissionplayer = GetPlayerBySteamID(player.UID);
            if (permissionplayer != null)
            {
                lock (_obj)
                {
                    _handler.PermissionPlayers.Remove(permissionplayer);
                    return true;
                }
            }

            return false;
        }

        public bool RemovePermissionPlayer(PermissionPlayer permissionPlayer)
        {
            if (permissionPlayer == null)
            {
                return false;
            }
            
            lock (_obj)
            {
                if (_handler.PermissionPlayers.Contains(permissionPlayer))
                {
                    _handler.PermissionPlayers.Remove(permissionPlayer);
                    return true;
                }
            }

            return false;
        }

        public bool RemovePermissionPlayer(ulong steamid)
        {
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer != null)
            {
                lock (_obj)
                {
                    _handler.PermissionPlayers.Remove(permissionplayer);
                    return true;
                }
            }

            return false;
        }

        public bool AddPermission(Fougerite.Player player, string permission)
        {
            if (player == null)
            {
                return false;
            }
            permission = permission.Trim().ToLower();
            
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == player.UID))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        return true;
                    }
                    
                    x.Permissions.Add(permission);
                    return true;
                }

                return false;
            }
        }

        public bool AddGroupToPlayer(ulong steamid, string groupname)
        {
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == steamid))
                {
                    if (!x.Groups.Contains(groupname))
                    {
                        x.Groups.Add(groupname);
                    }
                    return true;
                }
            }

            return false;
        }

        public bool RemoveGroupFromPlayer(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();

            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(o => o.SteamID == steamid))
                {
                    string gname = x.Groups.FirstOrDefault(y => y.Trim().ToLower() == groupname);
                    if (gname != null)
                    {
                        x.Groups.Remove(gname);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CreateGroup(string groupname, List<string> permissions = null, string nickname = null)
        {
            if (permissions == null)
            {
                permissions = new List<string>();
            }

            if (nickname == null)
            {
                nickname = groupname + "NickName";
            }
            
            PermissionGroup group = GetGroupByName(groupname);
            
            lock (_obj)
            {
                if (group != null)
                {
                    return false;
                }
                
                _handler.PermissionGroups.Add(new PermissionGroup()
                {
                    GroupName = groupname,
                    GroupPermissions = permissions,
                    NickName = nickname
                });

                return true;
            }
        }

        public bool RemoveGroup(string groupname)
        {
            groupname = groupname.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            lock (_obj)
            {
                if (group != null)
                {
                    _handler.PermissionGroups.Remove(group);
                    
                    foreach (var x in _handler.PermissionPlayers)
                    {
                        string gname = x.Groups.FirstOrDefault(y => y.Trim().ToLower() == groupname);

                        if (gname != null)
                        {
                            x.Groups.Remove(gname);
                        }
                    }

                    return true;
                }
            }

            return false;
        }
        
        public bool AddPermission(PermissionPlayer permissionPlayer, string permission)
        {
            if (permissionPlayer == null)
            {
                return false;
            }
            permission = permission.Trim().ToLower();
            
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == permissionPlayer.SteamID))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        return true;
                    }
                    
                    x.Permissions.Add(permission);
                    return true;
                }

                return false;
            }
        }
        
        public bool AddPermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == steamid))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        return true;
                    }
                    
                    x.Permissions.Add(permission);
                    return true;
                }

                return false;
            }
        }

        public bool RemovePermission(Fougerite.Player player, string permission)
        {
            permission = permission.Trim().ToLower();
            
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == player.UID))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        x.Permissions.Remove(permission);
                    }

                    return true;
                }

                return false;
            }
        }
        
        public bool RemovePermission(PermissionPlayer permissionPlayer, string permission)
        {
            if (permissionPlayer == null)
            {
                return false;
            }
            permission = permission.Trim().ToLower();
            
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == permissionPlayer.SteamID))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        x.Permissions.Remove(permission);
                    }

                    return true;
                }

                return false;
            }
        }
        
        public bool RemovePermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            lock (_obj)
            {
                foreach (var x in _handler.PermissionPlayers.Where(x => x.SteamID == steamid))
                {
                    if (x.Permissions.Contains(permission))
                    {
                        x.Permissions.Remove(permission);
                    }

                    return true;
                }

                return false;
            }
        }
    }
}