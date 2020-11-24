using System;
using System.Net;
using System.Text;
using Grapevine.Core;
using Grapevine.Core.Server.Attributes;
using Grapevine.Core.Shared;
using Grapevine.Core.Interfaces.Server;
using Grapevine.Core.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Threading;
using System.Runtime.InteropServices;
using Jose;

/*
Copyright(c) 2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
AASX Connect is licensed under the Apache License 2.0 (Apache-2.0, see below).
*/

namespace AasxConnect
{
    static public class Connect
    {
        [RestResource]
        public class ConnectResource
        {
            [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.POST, PathInfo = "^/publish(/|)$")]
            public IHttpContext EvalPostPublish(IHttpContext context)
            {
                PostPublish(context);
                return context;
            }
        }

        // Just publish, no /connect before needed
        public static void PostPublish(IHttpContext context)
        {
            string source = "";
            string responseJson = "";

            try
            {
                TransmitFrame tf = new TransmitFrame();
                tf = Newtonsoft.Json.JsonConvert.DeserializeObject<TransmitFrame>(context.Request.Payload);
                source = tf.source;

                if (!childs.Contains(source))
                    childs.Add(source);

                // responseJson = executeTransmitFrames(tf);

                DateTime now = DateTime.UtcNow;
                if (!childsTimeStamps.ContainsKey(source))
                {
                    childsTimeStamps.Add(source, now);
                }
                else
                {
                    childsTimeStamps[source] = now;
                }

                Console.WriteLine(countWriteLine++ + " PostPublish " + source + " " + now);
            }
            catch
            {
            }

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = responseJson.Length;
            context.Response.SendResponse(responseJson);
        }

        public static void checkChildsTimeStamps()
        {
            while (true)
            {
                foreach (string c in childs)
                {
                    if (childsTimeStamps.ContainsKey(c))
                    {
                        DateTime now = DateTime.UtcNow;
                        DateTime last = childsTimeStamps[c];
                        TimeSpan difference = now.Subtract(last);
                        if (difference.TotalSeconds > 20)
                        {
                            Console.WriteLine(countWriteLine++ + " Remove Child " + c + " " + now + "," + last + "," + difference);
                            childs.Remove(c);
                            break;
                        }
                    }
                }

                Thread.Sleep(30000); // remove after 30 seconds without publish
            }
        }

        public static string secretString = "Industrie4.0-Asset-Administration-Shell";

        public static string ContentToString(this HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        public class TransmitData
        {
            public string source;
            public string destination;
            public string type;
            public string encrypt;
            public string extensions;
            public List<string> publish;
            public TransmitData()
            {
                publish = new List<string> { };
            }
        }

        public class TransmitFrame
        {
            public string source;
            public List<TransmitData> data;
            public TransmitFrame()
            {
                data = new List<TransmitData> { };
            }
        }

        static List<string> childs = new List<string> { };
        static Dictionary<string, DateTime> childsTimeStamps = new Dictionary<string, DateTime>();

        public static List<TransmitData> publishRequest = new List<TransmitData> { };
        public static List<TransmitData>[] publishResponse = new List<TransmitData>[1000];
        public static List<string>[] publishResponseChilds = new List<string>[1000];

        static bool loop = true;
        static long count = 1;
        static long countWriteLine = 0;

        static bool test = false;
        static int newData = 0;

        public static string sourceName = "";
        public static string domainName = "";
        public static string parentDomain = "";
        public static string[] childDomains = new string[100];
        public static string[] childDomainsNames = new string[100];
        public static int childDomainsCount = 0;
    }
}
