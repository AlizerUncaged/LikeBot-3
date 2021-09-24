﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MihaZupan;

namespace Geometry_Dash_LikeBot_3.Likebot_3.Boomlings_Networking
{
    public abstract class Boomlings_Request_Base
    {
        private static readonly HttpClient _client = new HttpClient();

        public string Endpoint { get; set; }

        public string Query { get; set; }

        public async Task<string> SendAsync()
        {
            try
            {
                var proxy = Proxies.NextProxy();

                var socks5 = new HttpToSocks5Proxy(proxy.IP, proxy.Port, proxy.Username, proxy.Password);
                socks5.ResolveHostnamesLocally = true;

                var handler = new HttpClientHandler { Proxy = socks5 };
                HttpClient httpClient = new HttpClient(handler, true);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                httpClient.DefaultRequestVersion = HttpVersion.Version11;

                var content = new StringContent(Query);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                // "http://194.233.71.142/s.php"
                var postResult = await httpClient.PostAsync(Endpoint, content);

                // var postResult = await _client.PostAsync(,Endpoint, new StringContent(Query));
                string dataResult = await postResult.Content.ReadAsStringAsync();

                Console.WriteLine("Response " + dataResult);
                return dataResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

            }
            return null;
        }

        public Boomlings_Request_Base(string endpoint, string payload)
        {
            Endpoint = endpoint; Query = payload;
        }
    }
}
