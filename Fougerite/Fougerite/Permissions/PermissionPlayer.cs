using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    public class PermissionPlayer
    {
        [JsonProperty]
        public ulong SteamID
        {
            get;
            set;
        }

        [JsonProperty]
        public List<string> Permissions
        {
            get;
            set;
        }

        [JsonProperty]
        public List<string> Groups
        {
            get;
            set;
        }
    }
}