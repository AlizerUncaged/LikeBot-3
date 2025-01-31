﻿using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GDTools.Database {
    public class Database {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Data Data = new();

        public static Cache DBCache {
            get { return Data.Cache; }
        }

        public static bool IsExists(int accountid) {
            return Accounts.Any(x => x.AccountID == accountid);
        }

        public static void ChangePassword(int accountid, string password, string gjp) {
            var account = Accounts.FirstOrDefault(x => x.AccountID == accountid);
            if (account == null) return;
            account.Password = password; account.GJP = gjp;
            Save();
        }

        /// <summary>
        /// Gets <b>all</b> accounts regardless of owner.
        /// </summary>
        public static IEnumerable<Account> Accounts {
            get {
                if (Data != null)
                    return Data.Owners.SelectMany(x => x.GDAccounts);
                else return null;
            }
        }

        /// <summary>
        /// Gets all owners.
        /// </summary>
        public static List<User> Owners {
            get {
                if (Data != null)
                    return Data.Owners;
                else return null;
            }
        }

        /// <summary>
        /// Gets the Owner of a Geometry Dash account with the corresponding account ID.
        /// </summary>
        public static User GetOwnerFromAccountID(int accountID) {
            foreach (var owner in Data.Owners) {
                foreach (var _account in owner.GDAccounts) {
                    if (_account.AccountID == accountID)
                        return owner;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a GD account to the owner via its owner ID, if the owner doesn't exist it will create a new one
        /// with the GD account's username as its username.
        /// </summary>
        public static Account AddAccount(Core.Boomlings_Networking.Account_Data_Result serverResponse, string username, string password, string gjp, string ownerID = null) {

            var account = new Account {
                AccountID = serverResponse.AccountID,
                PlayerID = serverResponse.PlayerID,
                UDID = serverResponse.UDID,
                UUID = serverResponse.UUID,
                Username = username,
                Password = password,
                GJP = gjp,
            };

            var owner = GetOwnerViaID(ownerID);
            if (owner == null)
                owner = GenerateNewOwner(username, ownerID);

            owner.AppendAccount(account);

            Logger.Debug($"{account.Username} - Account added: {account.Username}");

            _ = Save();

            return account;
        }

        /// <summary>
        /// Generates a new owner.
        /// </summary>
        public static User GenerateNewOwner(string username, string id = null) {
            const int ownerIDLength = 8;
            string userID = string.IsNullOrWhiteSpace(id) ? Utilities.Random_Generator.RandomString(ownerIDLength) : id;

            var newUser = new User(userID);
            newUser.Username = username;
            Data.Owners.Add(newUser);
            return newUser;
        }

        /// <summary>
        /// Gets an owner via its owner ID.
        /// </summary>
        public static User GetOwnerViaID(string ownerID) {
            if (ownerID == null) return null;
            return Data.Owners.FirstOrDefault(x => x.OwnerID == ownerID);
        }

        /// <summary>
        /// Removes an account from the owner.
        /// </summary>
        public static void RemoveAccount(Account account) {
            var accountOwner = Data.Owners.Where(x => x.GDAccounts.Any(y => y.AccountID == account.AccountID)).FirstOrDefault();
            if (accountOwner == null) return;

            accountOwner.GDAccounts.Remove(account);
            Logger.Debug($"{account.Username} - Removed from database.");
            Save();
        }

        /// <summary>
        /// Get a <b>valid</b> account from the database.
        /// </summary>
        public static User GetUserFromSessionKey(string sessionkey) {
            if (string.IsNullOrWhiteSpace(sessionkey)) return null;

            var account = Data.Owners.FirstOrDefault(x => x.CheckKey(sessionkey));
            // var account = Data.Owners.acc.FirstOrDefault(x => x.CheckKey(sessionkey) && x.IsValid().IsValid);
            if (account != null)
                Logger.Info($"{account.Username} - Account found with session key {sessionkey}");
            else
                Logger.Info($"Attempt to get non-existing account with key {sessionkey}");
            return account;
        }

        public static Account GetAccountFromCredentials(string username, string password = null) {
            if (password == null)
                return Accounts.FirstOrDefault(x =>
                x.Username.ToLower().Trim() == username.ToLower().Trim());
            else
                return Accounts.FirstOrDefault(x =>
                x.Username.ToLower().Trim() == username.ToLower().Trim() &&
                x.Password.Trim() == password.Trim());
        }


        public static Account GetAccountViaAccountID(int AccountID) {
            return Accounts.FirstOrDefault(x => x.AccountID == AccountID);
        }

        public static IEnumerable<Account> GetRandomAccounts(int howMany) {
            return Accounts.OrderBy(x => Utilities.Random_Generator.Random.Next()).Take(howMany);
        }

        public static bool IsUserAgentBanned(string ua) {
            return Data.BannedUserAgents.Contains(ua);
        }

        public static int BruteForcerCurrentID {
            get {
                return Data.BruteForcerID;
            }
        }
        public static int BruteForcerNextAccountID() {
            Data.BruteForcerID++;
            return Data.BruteForcerID;
        }

        // file streams to db
        private static FileStream _dbFileStream =
            File.Open(Constants.DatabaseFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        private static StreamReader _dbReadStream =
            new(_dbFileStream);
        private static StreamWriter _dbWriteStream =
            new(_dbFileStream);

        /// <summary>
        /// Refresh the database, reads the JSON file.
        /// </summary>
        public static async Task Read() {
            Logger.Debug("Reading database file...");
            var dbContents = await _dbReadStream.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(dbContents))
                Save();
            else
                Data = JsonConvert.DeserializeObject<Data>(dbContents);
            Logger.Info($"Account list loaded {Accounts.Count()} Accounts...");

            Logger.Debug("Fetching banned User-Agents...");

            const string botUserAgentsSource = "https://raw.githubusercontent.com/monperrus/crawler-user-agents/master/crawler-user-agents.json";
            var gitBotUserAgents = Utilities.Quick_TCP.ReadURL(botUserAgentsSource).Result;
            var enumerated = JsonConvert.DeserializeObject<Crawler_User_Agents[]>(gitBotUserAgents);

            Data.BannedUserAgents = enumerated.SelectMany(x => x.Instances).ToList();
            Logger.Info($"Banned User-Agent list loaded {Data.BannedUserAgents.Count()} User-Agents banned...");
        }

        /// <summary>
        /// Saves the current database.
        /// </summary>
        public static async Task Save() {

#if DEBUG
            Logger.Debug($"Debug mode detected, database will not be saved...");
            await Task.Delay(3000);
            return;
#endif

            Logger.Debug($"Saving database...");
            var defaultDb = JsonConvert.SerializeObject(Data, Formatting.Indented);
            // clear database contents
            _dbFileStream.SetLength(0);
            // rewrite database
            _dbWriteStream.Write(defaultDb);
            _dbWriteStream.Flush();
            Logger.Info($"Database saved...");
        }
    }
}
