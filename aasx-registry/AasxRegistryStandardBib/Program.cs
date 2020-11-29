using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using AasxConnect;
using Grapevine.Core.Interfaces.Server;
using Grapevine.Core.Server;
using Grapevine.Core.Server.Attributes;
using Grapevine.Core.Shared;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
Copyright (c) 2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
*/

namespace AasxRegistryStandardBib
{
    [RestResource]
    public class ConnectResource
    {
        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = "^/directory(/|)$")]
        public IHttpContext EvalGetDirectory(IHttpContext context)
        {
            GetDirectory(context);
            return context;
        }

        public static void GetDirectory(IHttpContext context)
        {
            string responseJson = JsonConvert.SerializeObject(Program.aasDirectory, Formatting.Indented);

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentLength64 = responseJson.Length;
            context.Response.SendResponse(responseJson);
        }

        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = "^/refresh(/|)$")]
        public IHttpContext EvalRefresh(IHttpContext context)
        {
            Refresh(context);
            return context;
        }

        public static bool refresh = true;
        public static void Refresh(IHttpContext context)
        {
            refresh = true;

            context.Response.ContentType = ContentType.TEXT;
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.SendResponse("OK");
            Console.WriteLine("Get Refresh");
        }

        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = "^/server/listaas(/|)$")]
        public IHttpContext GetServerListAas(IHttpContext context)
        {
            GetListAAS(context);
            return context;
        }

        public static void GetListAAS(IHttpContext context)
        {
            dynamic res = new ExpandoObject();
            var aaslist = new List<string>();

            if (Program.aasDirectory.Count > 0)
            {
                int i = 0;

                foreach (var server in Program.aasDirectory)
                {
                    foreach (var aas in server.aasList)
                    {
                        aaslist.Add(
                            i++ + " : "
                            + server.source + " : "
                            + aas.index + " : "
                            + aas.idShort + " : "
                            + aas.identification + " : "
                            + aas.fileName + " : "
                            + aas.assetId);
                    }
                }
            }

            res.aaslist = aaslist;

            string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentLength64 = responseJson.Length;
            context.Response.SendResponse(responseJson);
        }

        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = @"^/getaasx2/([^/]+)/(\d+)(/|)$")]
        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = @"^/server/getaasx2/(\d+)(/|)$")]
        public IHttpContext GetAASX2(IHttpContext context)
        {
            GetAasx(context);
            return context;
        }

        public static string getAasxStatus = "";
        public static string getAasxServerName = "";
        public static int getAasxServerIndex = 0;
        public static string getAasxFileName = "";
        public static string getAasxFileData = "";

        public static void GetAasx(IHttpContext context)
        {
            string ret = "ERROR";
            dynamic res = new ExpandoObject();

            while (getAasxStatus != "") // earlier Download pending
            {
                System.Threading.Thread.Sleep(1000);
            }
            getAasxServerName = "";
            getAasxServerIndex = 0;
            getAasxFileName = "";
            getAasxFileData = "";

            string path = context.Request.PathInfo;
            string[] split = path.Split('/');
            string node = split[2];
            string aasIndex = split[3];

            if (Program.aasDirectory.Count > 0)
            {
                int i = 0;

                foreach (var server in Program.aasDirectory)
                {
                    foreach (var aas in server.aasList)
                    {
                        if (i++ == Convert.ToInt32(aasIndex))
                        {
                            getAasxServerName = server.source;
                            getAasxServerIndex = aas.index;
                        }
                    }
                }
            }

            if (getAasxServerName != "")
            {
                getAasxStatus = "start";
                while (getAasxStatus != "end") // wait for Download
                {
                    System.Threading.Thread.Sleep(1000);
                }

                getAasxStatus = "";
            }

            if (getAasxFileData != "")
            {
                res.fileName = getAasxFileName;
                res.fileData = getAasxFileData;
            }

            string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentLength64 = responseJson.Length;
            context.Response.SendResponse(responseJson);
        }

        [RestRoute(HttpMethod = Grapevine.Core.Shared.HttpMethod.GET, PathInfo = @"^/server/aasxbyasset/([^/]+)(/|)$")]
        public IHttpContext GetAASX2ByAssetId(IHttpContext context)
        {
            GetAasxByAssetId(context);
            return context;
        }

        public static void GetAasxByAssetId(IHttpContext context)
        {
            string ret = "ERROR";
            dynamic res = new ExpandoObject();

            string path = context.Request.PathInfo;
            string[] split = path.Split('/');
            string node = split[2];
            string assetId = split[3].ToUpper();

            getAasxServerName = "";
            getAasxServerIndex = 0;
            getAasxFileName = "";
            getAasxFileData = "";
            Program.aasListParameters aasFound = null;

            if (Program.aasDirectory.Count > 0)
            {
                foreach (var server in Program.aasDirectory)
                {
                    foreach (var aas in server.aasList)
                    {
                        string url = WebUtility.UrlEncode(aas.assetId).ToUpper();
                        if (assetId == url)
                        {
                            aasFound = aas;
                            getAasxServerName = server.source;
                            getAasxServerIndex = aas.index;
                        }
                    }
                }
            }

            string headers = context.Request.Headers.ToString();
            string token = context.Request.Headers.Get("accept");
            if (token == null || token != "application/aas")
            {
                // Human by browser
                string text = "";

                text += "<strong>" + "This is the human readable page for your asset" + "</strong><br><br>";

                text += "AssetID = " + assetId + "<br><br>";

                text += "JSON entry in AASX Registry:<br>" +
                    JsonConvert.SerializeObject(aasFound, Formatting.Indented) + "<br><br>";

                string link = "http://admin-shell-io.com:52001/directory";
                text += "View AASX Registry:<br>" +
                    "<a href= \"" + link + "\" target=\"_blank\">" + 
                    link + "</a>" + "<br><br>";

                link = "http://admin-shell-io.com:52001/server/aasxbyasset/" + assetId;
                text += "Please open AAS in AASX Package Explorer by: File / Connect / Connect via REST:<br>" +
                    "<a href= \"" + link + "\" target=\"_blank\">" +
                    link + "</a>" + "<br><br>";

                link = "http://admin-shell-io.com:52001/server/aasxbyasset/"+ assetId;
                text += "Please use Postman to get raw data:<br>GET " +
                    "<a href= \"" + link + "\" target=\"_blank\">" +
                    link + "</a>" + "<br>" +
                "and set Headers / Accept application/aas" + "<br><br>";

                /*
                res.text = "This is the human readable page for your asset";
                res.receivedID = "AssetID = " + assetId;

                res.aasFound = aasFound;

                res.hint = "Please open AAS in AASX Package Explorer by: File / Connect / Connect via REST: " + 
                    "http://admin-shell-io.com:52001/server/aasxbyasset/" + assetId;
                res.hint2 = "Please use Postman to get raw data: GET " +
                    "http://admin-shell-io.com:52001/server/aasxbyasset/" + assetId +
                    " and set Headers / Accept application/aas";

                string responseJson2 = JsonConvert.SerializeObject(res, Formatting.Indented);
                */

                // context.Response.ContentType = ContentType.JSON;
                context.Response.ContentType = ContentType.HTML;
                context.Response.ContentEncoding = System.Text.Encoding.UTF8;
                // context.Response.SendResponse(responseJson2);
                context.Response.SendResponse(text);
                return;
            }

            // I40 client
            while (getAasxStatus != "") // earlier Download pending
            {
                System.Threading.Thread.Sleep(1000);
            }


            if (getAasxServerName != "")
            {
                getAasxStatus = "start";
                while (getAasxStatus != "end") // wait for Download
                {
                    System.Threading.Thread.Sleep(1000);
                }

                getAasxStatus = "";
            }

            if (getAasxFileData != "")
            {
                res.fileName = getAasxFileName;
                res.fileData = getAasxFileData;
            }

            string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentLength64 = responseJson.Length;
            context.Response.SendResponse(responseJson);
        }
    }

    public class Program
    {
        public static string sourceName = "";
        public static string domainName = "";
        public static string parentDomain = "";
        public static string[] childDomains = new string[100];
        public static string[] childDomainsNames = new string[100];
        public static int childDomainsCount = 0;

        static public List<aasDirectoryParameters> aasDirectory = new List<aasDirectoryParameters> { };

        public class aasListParameters
        {
            public int index;
            public string idShort;
            public string identification;
            public string fileName;
            public string assetId;
            public string humanEndPoint;
            public string restEndPoint;
        }
        public class aasDirectoryParameters
        {
            public string source;
            public List<aasListParameters> aasList;
            public aasDirectoryParameters()
            {
                aasList = new List<aasListParameters> { };
            }
        }

        public static string connectServer = "http://admin-shell-io.com:52000";
        static string connectNodeName = "AasxRegistry";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;

        static int count = 0;

        public static void connectThreadLoop()
        {
            bool newConnectData = false;

            while (connectLoop)
            {
                Connect.TransmitFrame tf = new Connect.TransmitFrame
                {
                    source = connectNodeName
                };
                Connect.TransmitData td = new Connect.TransmitData
                {
                    source = connectNodeName
                };

                if (ConnectResource.getAasxStatus == "start")
                {
                    td.destination = ConnectResource.getAasxServerName;
                    td.type = "getaasx";
                    td.extensions = ConnectResource.getAasxServerIndex.ToString();
                    tf.data.Add(td);
                    ConnectResource.getAasxStatus = "send";
                    Console.WriteLine("Request file");
                }
                if (ConnectResource.refresh)
                {
                    aasDirectory = new List<aasDirectoryParameters> { };
                    td.destination = "";
                    td.type = "getDirectory";
                    td.extensions = "";
                    tf.data.Add(td);
                    ConnectResource.refresh = false;
                    Console.WriteLine("Refresh");
                }

                HttpClient httpClient;
                if (clientHandler != null)
                {
                    httpClient = new HttpClient(clientHandler);
                }
                else
                {
                    httpClient = new HttpClient();
                }

                string publish = JsonConvert.SerializeObject(tf, Formatting.Indented);
                var contentJson = new StringContent(publish, System.Text.Encoding.UTF8, "application/json");

                string content = "";
                try
                {
                    var result = httpClient.PostAsync(connectServer + "/publish", contentJson).Result;
                    content = Connect.ContentToString(result.Content);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Publish: " + e.Message);
                    content = "";
                }

                if (content != "")
                {
                    Console.WriteLine(count++ + " Received content");
                    newConnectData = false;
                    string node = "";

                    try
                    {
                        Connect.TransmitFrame tf2 = new Connect.TransmitFrame();
                        tf2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Connect.TransmitFrame>(content);

                        node = tf2.source;
                        foreach (Connect.TransmitData td2 in tf2.data)
                        {
                            switch (td2.type)
                            {
                                case "directory":
                                    if (td2.destination == connectNodeName)
                                    {
                                        aasDirectoryParameters adp = new aasDirectoryParameters();
                                        Console.WriteLine("Received directory: " + td2.source);

                                        try
                                        {
                                            adp = Newtonsoft.Json.JsonConvert.DeserializeObject<aasDirectoryParameters>(td2.publish[0]);
                                        }
                                        catch
                                        {
                                            adp = null;
                                            Console.WriteLine("Error receive directory");
                                        }
                                        if (adp != null)
                                            aasDirectory.Add(adp);
                                        tf.data.Remove(td2);
                                    }
                                    break;
                                case "getaasxFile":
                                    if (ConnectResource.getAasxStatus == "send" && td2.destination == connectNodeName && td2.source == ConnectResource.getAasxServerName)
                                    {
                                        var parsed3 = JObject.Parse(td2.publish[0]);

                                        string fileName = parsed3.SelectToken("fileName").Value<string>();
                                        string fileData = parsed3.SelectToken("fileData").Value<string>();

                                        ConnectResource.getAasxFileName = fileName;
                                        ConnectResource.getAasxFileData = fileData;

                                        ConnectResource.getAasxStatus = "end";

                                        Console.WriteLine("Received: " + fileName);
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error receive");
                    }
                    if (newConnectData)
                    {
                    }
                }

                Thread.Sleep(connectUpdateRate);
            }
        }

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static void Run(string[] args)
        {
            Console.WriteLine(
            "Copyright(c) 2020 PHOENIX CONTACT GmbH & Co.KG <opensource@phoenixcontact.com>, author: Andreas Orzelski\n" +
            "This software is licensed under the Apache License 2.0 (APL - 2.0)\n" +
            "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)\n" +
            "The Grapevine REST server framework is licensed under Apache License 2.0 (Apache - 2.0)\n" +
            "Jose-JWT is licensed under the MIT license (MIT)\n" +
            "This application is a sample application for demonstration of the features of the Administration Shell.\n" +
            "The implementation uses the concepts of the document Details of the Asset\n" +
            "Administration Shell published on www.plattform-i40.de which is licensed under Creative Commons CC BY-ND 3.0 DE."
            );
            Console.WriteLine("--help for available switches.");
            Console.WriteLine("");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            }

            // default command line options
            bool debugwait = false;
            Boolean help = false;

            int i = 0;
            while (i < args.Length)
            {
                var x = args[i].Trim().ToLower();

                if (x == "-debugwait")
                {
                    debugwait = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "--help")
                {
                    help = true;
                    break;
                }
            }

            if (help)
            {
                Console.WriteLine("-debugwait = wait for Debugger to attach");
                Console.WriteLine("Press ENTER");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("");

            // auf Debugger warten
            if (debugwait)
            {
                Console.WriteLine("Please attach debugger now!");
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            clientHandler = new HttpClientHandler();
            clientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(clientHandler);

            Console.WriteLine("Waiting for client on " + domainName);

            var serverSettings = new ServerSettings
            {
                // Host = "localhost",
                Host = "admin-shell-io.com",
                Port = "52001",
                UseHttps = false
            };
            RestServer rs = new RestServer(serverSettings);
            rs.Start();

            Byte[] barray = new byte[10];
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(barray);
            connectNodeName = "AasxRegistry_" + Convert.ToBase64String(barray);

            connectLoop = true;
            connectThread = new Thread(new ThreadStart(connectThreadLoop));
            connectThread.Start();

            Console.WriteLine("Press CTRL-C to STOPP");
            // Console.ReadLine();
            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }
            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(Timeout.Infinite);

            connectLoop = false;

            rs.Stop();
        }
    }
}
