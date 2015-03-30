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
            string queryName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, RustPP.Core.Name, "AddAdmin Usage:  /addadmin playerName");
                return;
            }

            var query = from entry in RustPP.Core.userCache
                        let sim = entry.Value.Similarity(queryName)
                        where sim > 0.333d
                        group new Administrator(entry.Key, entry.Value) by sim into matches
                        select matches.FirstOrDefault();

            if (query.FirstOrDefault() == null)
            {
                Util.sayUser(Arguments.argUser.networkPlayer, Core.Name, string.Format("Unsure about \"{0}\". Please be more specific.", queryName));
            }
            else
            {
                var newAdmin = query.First();
                var myAdmin = Arguments.argUser;
                if (newAdmin.UserID == myAdmin.userID)
                {
                    Util.sayUser(myAdmin.networkPlayer, Core.Name, "Seriously? You are already an admin...");
                }
                else if (Administrator.IsAdmin(newAdmin.UserID))
                {
                    Util.sayUser(myAdmin.networkPlayer, Core.Name, string.Format("{0} is already an administrator.", newAdmin.DisplayName));
                }
                else
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
}