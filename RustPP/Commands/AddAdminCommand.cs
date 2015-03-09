namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    internal class AddAdminCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            string queryName = Arguments.ArgsStr.Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "AddAdmin Usage:  /addadmin playerName");
                return;
            }

            var query = from entry in RustPP.Core.userCache
                        let sim = entry.Value.Similarity(queryName)
                        where sim > 0.4d
                        group new Administrator(entry.Key, entry.Value) by sim into matches
                        select matches.FirstOrDefault();

            if (query.Count() == 1)
            {
                NewAdmin(query.First(), Arguments.argUser);
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
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "Please enter the number matching the player to become administrator.");
                RustPP.Core.adminAddWaitList[Arguments.argUser.userID] = query;
            }
        }

        public void PartialNameNewAdmin(ref ConsoleSystem.Arg Arguments, int id)
        {
            if (id == 0)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, Core.Name, "Canceled!");
                return;
            }
            var list = Core.adminAddWaitList[Arguments.argUser.userID] as IEnumerable<Administrator>;
            NewAdmin(list.ElementAt(id), Arguments.argUser);
        }

        public void NewAdmin(Administrator newAdmin, NetUser myAdmin)
        {
            if (newAdmin.UserID == myAdmin.userID)
            {
                Util.sayUser(myAdmin.networkPlayer, Core.Name, "Seriously? You are already an admin...");
            } else if (Administrator.IsAdmin(newAdmin.UserID))
            {
                Util.sayUser(myAdmin.networkPlayer, Core.Name, string.Format("{0} is already an administrator.", newAdmin.DisplayName));
            } else
            {
                string flagstr = Core.config.GetSetting("Settings", "default_admin_flags");

                if (flagstr != null)
                {
                    List<string> flags = new List<string>(flagstr.Split(new char[] { '|' }));
                    newAdmin.Flags = flags;
                }
                Administrator.AddAdmin(newAdmin);
                Administrator.NotifyAdmins(string.Format("{0} has been made an administrator by {1}.", newAdmin.DisplayName, myAdmin.displayName));
            }
        }
    }
}