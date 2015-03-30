namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Social;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    internal class AddFriendCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            string queryName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, Core.Name, "Friends Management Usage:  /addfriend playerName");
                return;
            }

            var query = from entry in Core.userCache
                        let sim = entry.Value.Similarity(queryName)
                        where sim > 0.333d
                        group new PList.Player(entry.Key, entry.Value) by sim into matches
                        select matches.FirstOrDefault();

            if (query.FirstOrDefault() == null)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, Core.Name, string.Format("Unsure about \"{0}\". Please be more specific.", queryName));
            }
            else
            {
                var friend = query.First();
                var friending = Arguments.argUser;
                if (friending.userID == friend.UserID)
                {
                    Util.sayUser(friending.networkPlayer, Core.Name, "You can't add yourself as a friend!");
                    return;
                }
                var command = (FriendsCommand)ChatCommand.GetCommand("friends");
                var list = (FriendList)command.GetFriendsLists()[friending.userID];
                if (list == null)
                {
                    list = new FriendList();
                }
                if (list.isFriendWith(friend.UserID))
                {
                    Util.sayUser(friending.networkPlayer, Core.Name, string.Format("You are already friends with {0}.", friend.DisplayName));
                    return;
                }
                list.AddFriend(friend.DisplayName, friend.UserID);
                command.GetFriendsLists()[friending.userID] = list;
                Util.sayUser(friending.networkPlayer, Core.Name, string.Format("You have added {0} to your friends list.", friend.DisplayName));
                PlayerClient client;
                if (PlayerClient.FindByUserID(friend.UserID, out client))
                    Util.sayUser(client.netUser.networkPlayer, Core.Name, string.Format("{0} has added you to their friends list.", friending.displayName));
            }            
        }
    }
}