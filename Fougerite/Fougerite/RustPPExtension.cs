﻿
namespace Fougerite
{
    using System;
    using RustPP;
    using RustPP.Commands;
    using RustPP.Permissions;
    using RustPP.Social;
    using System.Collections.Generic;

    public class RustPPExtension
    {
        public FriendList FriendsOf(ulong steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[steamid];
            return list;
        }

        public FriendList FriendsOf(string steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[Convert.ToUInt64(steamid)];
            return list;
        }

        public FriendsCommand GetFriendsCommand
        {
            get
            {
                return (FriendsCommand) ChatCommand.GetCommand("friends");
            }
        }

        public bool HasPermission(ulong userID, string perm)
        {
            var admin = GetAdmin(userID);
            return admin != null && admin.HasPermission(perm);
        }

        public bool HasPermission(string name, string perm)
        {
            var admin = GetAdmin(name);
            return admin != null && admin.HasPermission(perm);
        }

        public bool IsAdmin(ulong uid)
        {
            return Administrator.IsAdmin(uid);
        }

        public bool IsAdmin(string name)
        {
            return Administrator.IsAdmin(name);
        }

        public Administrator GetAdmin(ulong userID)
        {
            return Administrator.GetAdmin(userID);
        }

        public Administrator GetAdmin(string name)
        {
            return Administrator.GetAdmin(name);
        }

        public Administrator Admin(ulong userID, string name, string flags)
        {
            return new Administrator(userID, name, flags);
        }

        public Administrator Admin(ulong userID, string name)
        {
            return new Administrator(userID, name);
        }

        public void RemoveInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        public void AddInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        public bool HasInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            return command.userIDs.Contains(userID);
        }

        public void RemoveGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        public void AddGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        public bool HasGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            return command.userIDs.Contains(userID);
        }

        public void RustPPSave()
        {
            Helper.CreateSaves();
        }

        public Dictionary<ulong, string> Cache
        {
            get
            {
                return RustPP.Core.userCache;
            }
        }
    }
}
