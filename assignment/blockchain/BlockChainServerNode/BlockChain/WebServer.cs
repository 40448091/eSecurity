using Newtonsoft.Json;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;

namespace BlockChain
{
    public class WebServer
    {
        public WebServer(BlockChain chain)
        {
            //start the web-server
            var settings = ConfigurationManager.AppSettings;
            string host = settings["host"]?.Length > 1 ? settings["host"] : "localhost";
            string port = settings["port"]?.Length > 1 ? settings["port"] : "12345";

            Logger.Log($"Web Service Listening on port: {port}");

            var server = new TinyWebServer.WebServer(request =>
                {

                    string path = request.Url.PathAndQuery.ToLower();
                    string query = "";
                    string json = "";

                    Logger.Log($"Request:{path}");


                    if (path.Contains("?"))
                    {
                        string[] parts = path.Split('?');
                        path = parts[0];
                        query = parts[1];
                    }

                    switch (path)
                    {
                        //GET: http://localhost:12345/mine
                        case "/mine":
                            return chain.Mine(query);

                        //POST: http://localhost:12345/transactions/new
                        //{ "Amount":123, "Recipient":"ebeabf5cc1d54abdbca5a8fe9493b479", "Sender":"31de2e0ef1cb4937830fcfd5d2b3b24f" }
                        case "/transfer":
                            if (request.HttpMethod != HttpMethod.Post.Method)
                                return $"{new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)}";

                            json = new StreamReader(request.InputStream).ReadToEnd();
                            Transaction trx = JsonConvert.DeserializeObject<Transaction>(json);
                            try
                            {
                                int blockId = chain.CreateTransaction(trx,false);
                                return $"Your transaction will be included in block {blockId}";
                            } catch (System.Exception ex)
                            {
                                return ex.Message;
                            }

                        //GET: http://localhost:12345/chain
                        case "/chain":
                            return chain.GetFullChain();

                        //POST: http://localhost:12345/nodes/register
                        //{ "Urls": ["localhost:54321", "localhost:54345", "localhost:12321"] }
                        case "/nodes/register":
                            if (request.HttpMethod != HttpMethod.Post.Method)
                                return $"{new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)}";

                            json = new StreamReader(request.InputStream).ReadToEnd();
                            var urlList = new { Urls = new string[0] };
                            var obj = JsonConvert.DeserializeAnonymousType(json, urlList);
                            return chain.RegisterNodes(obj.Urls);

                        //GET: http://localhost:12345/nodes/resolve
                        case "/nodes/resolve":
                            return chain.Consensus(false);

                        case "/balance":
                            if (request.HttpMethod != HttpMethod.Post.Method)
                                return $"{new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)}";

                            json = new StreamReader(request.InputStream).ReadToEnd();

                            return chain.Balance(json);

                        case "/history":
                            List<string> history = chain.TransactionHistory(query);
                            return JsonConvert.SerializeObject(history);

                        case "/pending":
                            return JsonConvert.SerializeObject(chain.PendingTransactions());

                        case "/test/start":
                            Logger.Log($"Test {query} Start ----------------------------------------");
                            return $"Test {query} Start";

                        case "/test/end":
                            Logger.Log($"Test {query} End ------------------------------------------");
                            return $"Test {query} end";

                        case "/test/init":
                            chain.Init();
                            return $"BlockChain initialized";

                        case "/test/checkpoint":
                            return chain.CheckPoint();

                        case "/test/rollback":
                            return chain.Rollback();

                        case "/test/miner/start":
                            string[] cmdArgs = query.Split('&');
                            chain.Miner_Start(cmdArgs[0]);
                            return "Miner started";

                        case "/test/miner/stop":
                            chain.Miner_Stop();
                            return "Miner Stopped";

                    }

                    return "";
                },
                $"http://{host}:{port}/mine/",
                $"http://{host}:{port}/transfer/",
                $"http://{host}:{port}/chain/",
                $"http://{host}:{port}/nodes/register/",
                $"http://{host}:{port}/nodes/resolve/",
                $"http://{host}:{port}/balance/",
                $"http://{host}:{port}/history/",
                $"http://{host}:{port}/pending/",
                $"http://{host}:{port}/test/init/",
                $"http://{host}:{port}/test/start/",
                $"http://{host}:{port}/test/end/",
                $"http://{host}:{port}/test/checkpoint/",
                $"http://{host}:{port}/test/rollback/",
                $"http://{host}:{port}/test/miner/start/",
                $"http://{host}:{port}/test/miner/stop/"
            );

            server.Run();
        }
    }
}
