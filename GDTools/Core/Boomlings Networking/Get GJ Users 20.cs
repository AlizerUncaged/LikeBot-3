﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDTools.Core.Boomlings_Networking {
    /// <summary>
    /// Gets the account ID and player ID from the username.
    /// </summary>
    public class Get_GJ_Users_20 : Boomlings_Request_Base {
        public Get_GJ_Users_20(string username) : base($"/database/getGJUsers20.php",
            $"gameVersion=21&binaryVersion=35&gdw=0&str={username}&total=0&page=0&secret=Wmfd2893gb7") {

        }

        public async Task<(int accountID, int playerID, bool success, string reason)> GetIDs() {
            var response = await SendAsync(ProxyType.PaidProxy);

            Dictionary<string, string> keyAndVal = new();
            var splitted = response.Split(':');

            for (int i = 0; i < splitted.Length; i++) {
                if (i % 2 == 0) {
                    // if even then its key else val
                } else {
                    var key = splitted[i - 1];
                    var val = splitted[i];
                    // it is val now
                    keyAndVal.Add(key, val);
                }
            }


            if (!keyAndVal.ContainsKey("16")) return (0, 0, false, "The player doesn't exist.");
            int accountID, playerID;
            int.TryParse(keyAndVal["16"], out accountID);
            int.TryParse(keyAndVal["2"], out playerID);

            return (accountID, playerID, true, string.Empty);
        }
    }
}
