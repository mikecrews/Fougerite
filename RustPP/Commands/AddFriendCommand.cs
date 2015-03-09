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
            string queryName = Arguments.ArgsStr.Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "Friends Management Usage:  /addfriend playerName");
                return;
            }

            var query = from entry in RustPP.Core.userCache
                        let sim = entry.Value.Similarity(queryName)
                        where sim > 0.4d
                        group new PList.Player(entry.Key, entry.Value) by sim into matches
                        select matches.FirstOrDefault();

            if (query.Count() == 1)
            {
                AddFriend(query.First(), Arguments.argUser);
                return;
            }
            else
            {
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, string.Format("{0}  players match  {2}: ", query.Count(), queryName));
                for (int i = 1; i < query.Count(); i++)
                {
                    Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, string.Format("{0} - {1}", i, query.ElementAt(i).DisplayName));
                }
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "0 - Cancel");
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "Please enter the number matching the player to add as your friend.");
                RustPP.Core.friendWaitList[Arguments.argUser.userID] = query;
            }
        }

        public void PartialNameAddFriend(ref ConsoleSystem.Arg Arguments, int id)
        {
            if (id == 0)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "Canceled!");
                return;
            }
            var list = RustPP.Core.friendWaitList[Arguments.argUser.userID] as IEnumerable<PList.Player>;
            AddFriend(list.ElementAt(id), Arguments.argUser);
        }

        public void AddFriend(PList.Player friend, NetUser friending)
        {
            if (friending.userID == friend.UserID)
            {
                Util.sayUser(friending.networkPlayer, RustPP.Core.Name, "You can't add yourself as a friend!");
                return;
            }
            FriendsCommand command = (FriendsCommand)ChatCommand.GetCommand("friends");
            FriendList list = (FriendList)command.GetFriendsLists()[friending.userID];
            if (list == null)
            {
                list = new FriendList();
            }
            if (list.isFriendWith(friend.UserID))
            {
                Util.sayUser(friending.networkPlayer, RustPP.Core.Name, string.Format("You are already friends with {0}.", friend.DisplayName));
                return;
            }
            list.AddFriend(friend.DisplayName, friend.UserID);
            command.GetFriendsLists()[friending.userID] = list;
            Util.sayUser(friending.networkPlayer, RustPP.Core.Name, string.Format("You have added {0} to your friends list.", friend.DisplayName));
            PlayerClient client;
            if (PlayerClient.FindByUserID(friend.UserID, out client))
                Util.sayUser(client.netUser.networkPlayer, RustPP.Core.Name, string.Format("{0} has added you to their friends list.", friending.displayName));
        }
    }
}