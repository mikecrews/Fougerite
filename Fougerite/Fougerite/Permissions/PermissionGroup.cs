using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    public class PermissionGroup
    {
        private string _groupname;
        
        [JsonProperty]
        public int UniqueID
        {
            get;
            set;
        }
        
        [JsonProperty]
        public string GroupName
        {
            get
            {
                return _groupname;
            }
            set
            {
                _groupname = value.Trim();
                UniqueID = GetUniqueID(_groupname.ToLower());
            }
        }

        [JsonProperty]
        public string NickName
        {
            get;
            set;
        }

        [JsonProperty]
        public List<string> GroupPermissions
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets the unique identifier of a string based on MD5.
        /// This is used for group names.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int GetUniqueID(string value)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var hashed = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToInt32(hashed, 0);
            }
        }
    }
}