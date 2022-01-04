﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebserver;

namespace Geometry_Dash_LikeBot_3.Likebot_3.Login {
    public class Login_Route {
        [StaticRoute(HttpMethod.GET, "/")]
        public async Task LoginFormRoot(HttpContext ctx) {
            await LoginForm(ctx);
        }

        // just read it once
        public readonly byte[] LoginPage = File.ReadAllBytes("Likebot 3/Login/Login.html");
        [StaticRoute(HttpMethod.GET, "/login")]
        public async Task LoginForm(HttpContext ctx) {
            string cookies;
            var cookiesFound = ctx.Request.Headers.TryGetValue("Cookie", out cookies);

            if (cookiesFound) {
                var keysAndCookies = Utilities.Quick_TCP.ParseCookie(cookies);
                string sessionKey;
                var sessionKeyFound = keysAndCookies.TryGetValue("SessionsKey", out sessionKey);
                if (sessionKeyFound) {
                    var account = Database.Data.GetAccountFromSessionKey(sessionKey);
                    if (account != null) {
                        ctx.Response.StatusCode = 302;
                        ctx.Response.Headers["Location"] = "/dashboard";
                        await ctx.Response.Send();
                        return;
                    }

                }
            }
            await ctx.Response.Send(LoginPage);
            return;
        }


        public class AccountCheckResponse {
            public bool IsSuccess = false;
            public string Message { get; set; }
            public string SessionsKey { get; set; }
            public string Redirect { get; set; }
        }
        public async Task SkidDetected(HttpContext ctx) {
            await ctx.Response.Send(JsonConvert.SerializeObject(new AccountCheckResponse {
                IsSuccess = true,
                Message = "MySQL-00911: Invalid character error on table 'Skid' line 8 logging in as account id 0. Redirecting you to admin dashboard...",
                Redirect = "https://www.youtube.com/watch?v=xjS6Z8kztq8&ab_channel=Rickrollingnoads"
            }));
            return;
        }
        [StaticRoute(HttpMethod.GET, "/check")]
        public async Task CheckAccount(HttpContext ctx) {
            if (!ctx.Request.Query.Elements.ContainsKey("username") ||
                !ctx.Request.Query.Elements.ContainsKey("password")) {
                await SkidDetected(ctx);
                return;
            }
            string username = ctx.Request.Query.Elements["username"].Trim();
            string password = ctx.Request.Query.Elements["password"].Trim();
            if (username.Contains(">") || username.Contains("<") || password.Contains(">") || password.Contains("<")) {
                await SkidDetected(ctx);
                return;
            }

            Account_Checker checker = new(username, password,
                ctx.Request.Source.IpAddress);

            AccountCheckResponse response = new();

            if (checker.IsAlreadyLoggingIn()) {
                response.IsSuccess = false;
                response.Message = "You're already attempting to log in, please wait for the attempt to finish.";
                await ctx.Response.Send(JsonConvert.SerializeObject(response));
                return;
            }

            // now check account fr
            var result = await checker.Check();
            response.IsSuccess = result.IsSuccess;
            var serverResponses = result.ServerResult;
            response.Message = serverResponses.Message;
            var loggedInAccountID = serverResponses.AccountID;
            if (response.IsSuccess) {
                string sessionsKey = Utilities.Random_Generator.RandomString(64);
                string gjp = Utilities.Robcryptions.PasswordToGJP(password);

                response.Redirect = "/dashboard";
                response.SessionsKey = sessionsKey;

                if (Database.Data.IsExists(loggedInAccountID)) {
                    var accountInDB = Database.Data.GetAccountViaAccountID(loggedInAccountID);
                    var isValid = accountInDB.IsValid();
                    response.IsSuccess = isValid.IsValid;
                    response.Message = isValid.IsValid ? "Account already exists! Updating..." : isValid.Reason;
                    Database.Data.ChangePassword(serverResponses.AccountID, password, gjp, sessionsKey);
                } else
                    Database.Data.AddAccount(serverResponses, username, password, gjp, sessionsKey);

            } else if (!response.IsSuccess) {
                // if account with password exist, remove it
                var account = Database.Data.GetAccountFromCredentials(username, password);
                if (account != null)
                    Database.Data.RemoveAccount(account);
            }

            await ctx.Response.Send(JsonConvert.SerializeObject(response));
        }
    }
}
