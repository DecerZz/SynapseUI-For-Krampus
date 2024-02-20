using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using RestSharp;
using Synapse_UI_WPF.Static;

namespace Synapse_UI_WPF.Interfaces
{
    public static class WebInterface
    {

        public static readonly Random Rnd = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Rnd.Next(s.Length)]).ToArray());
        }

        public static Data.WebSocketHolder GetWhitelistedDomains()
        {
            using (var WC = new WebClient { Proxy = null })
            {
                return JsonConvert.DeserializeObject<Data.WebSocketHolder>("{}");
            }
        }

    }
}