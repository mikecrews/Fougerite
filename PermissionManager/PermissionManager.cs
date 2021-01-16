using System;
using System.Collections.Generic;
using Fougerite;
using Fougerite.Permissions;

namespace PermissionManager
{
    public class PermissionManager : Fougerite.Module
    {
        public override string Name
        {
            get { return "PermissionManager"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "PermissionManager"; }
        }

        public override Version Version
        {
            get { return new Version("1.0"); }
        }

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
        }

        private string[] Merge(string[] array, int fromindex)
        {
            string[] newarr = new string[array.Length - fromindex];
            int fromstorage = fromindex;
            for (int i = 0; i < newarr.Length; i++)
            {
                newarr[i] = array[fromstorage];
                fromstorage++;
            }

            return newarr;
        }

        private void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            switch (cmd)
            {
                // For players
                case "pem":
                {
                    PermissionSystem permissionSystem = PermissionSystem.GetPermissionSystem();
                    if (!player.Admin && !permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        return;
                    }

                    string secondcommand = args[0];
                    switch (secondcommand)
                    {
                        case "newplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem newplayer playername");
                                return;
                            }
                            
                            string playername = string.Join(" ", Merge(args, 1));
                            Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(playername);
                            if (pl != null)
                            {
                                permissionSystem.CreatePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player!");
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", playername + " not found!");
                            }

                            break;
                        }
                        case "deleteplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem deleteplayer playername");
                                return;
                            }
                            
                            string playername = string.Join(" ", Merge(args, 1));
                            Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(playername);
                            if (pl != null)
                            {
                                permissionSystem.RemovePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player!");
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", playername + " not found!");
                            }

                            break;
                        }
                        case "deleteofflplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem deleteofflplayer steamid");
                                return;
                            }
                            
                            string steamid = string.Join("", Merge(args, 1));
                            ulong uid;
                            if (!ulong.TryParse(steamid, out uid))
                            {
                                player.MessageFrom("PermissionSystem", "Use a steamid (Yes, sorry)");
                                return;
                            }

                            bool success = permissionSystem.RemovePermissionPlayer(uid);
                            player.MessageFrom("PermissionSystem", success ? "Removed!" : "User doesn't exist!");

                            break;
                        }
                        case "addperm":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem addperm steamid/name permission");
                                return;
                            }
                            
                            string target = args[1];
                            string permission = args[2];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    bool success = permissionSystem.AddPermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Added for " + pl.Name + " permission: " + permission 
                                        : "User doesn't exist!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                bool success = permissionSystem.AddPermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Added to " + uid + " permission: " + permission 
                                    : "User doesn't exist!");
                            }
                            break;
                        }
                        case "delperm":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem delperm steamid/name permission");
                                return;
                            }
                            
                            string target = args[1];
                            string permission = args[2];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    bool success = permissionSystem.RemovePermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Removed for " + pl.Name + " permission: " + permission 
                                        : "User doesn't exist!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                bool success = permissionSystem.RemovePermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Removed for " + uid + " permission: " + permission 
                                    : "User doesn't exist!");
                            }
                            break;
                        }
                        case "listperms":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pem listperms steamid/name");
                                return;
                            }
                            
                            string target = args[1];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    var ppl = permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        var list = new List<string>(ppl.Permissions);
                                        player.MessageFrom("PermissionSystem", "Perms: " + string.Join(", ", list.ToArray()));
                                    }
                                    else
                                    {
                                        player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                    }
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                var ppl = permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    var list = new List<string>(ppl.Permissions);
                                    player.MessageFrom("PermissionSystem", "Perms: " + string.Join(", ", list.ToArray()));
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                }
                            }
                            break;
                        }
                        
                    }

                    break;
                }
                // For group management
                case "pemg":
                {
                    PermissionSystem permissionSystem = PermissionSystem.GetPermissionSystem();
                    if (!player.Admin && !permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        return;
                    }
                    
                    string secondcommand = args[0];

                    break;
                }
            }
        }
    }
}