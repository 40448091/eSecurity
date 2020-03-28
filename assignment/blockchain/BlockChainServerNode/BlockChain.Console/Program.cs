using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

/********************************************************************
 * BlockChain Server Console
 * Author: Paul Haines (March 2020)
 *   Provides a basic command line console interface to the BlockChain
 *   Server Node.
 *   Allows the user to query save and roll-back server state
 ********************************************************************/
namespace BlockChain.Console
{
    class Program
    {
        static WebServer server = null;
        static CryptoProvider.ICryptoProvider _cryptoProvider = null;
        static BlockChain chain = null;
        static string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        //command console entry point
        static void Main(string[] args)
        {

            string nodeId = System.Configuration.ConfigurationManager.AppSettings["nodeId"]; 
            //get the crypto provider to use from the App.config
            string cryptoProvider = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];


            string cryptoProviderFilename = Path.Combine(appDir, cryptoProvider + "_CryptoProvider.dll");
            string keyFilename = Path.Combine(appDir, cryptoProvider + ".key");

            //load the specified crypto provider
            if (File.Exists(cryptoProviderFilename))
                _cryptoProvider = LoadCryptoProvider(cryptoProviderFilename);
            else
                throw new Exception(string.Format("Crypto Provider not found : {0}", cryptoProviderFilename));

            //load the server node key pair (not currently used)
            if (File.Exists(keyFilename))
                _cryptoProvider.ImportKeyPairFromFile(keyFilename);
            else
            {
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPairToFile(keyFilename);
                Logger.Log("Key pair generated");
            }


            //create a new server node web-server instance and intialize the server node's block chain
            chain = new BlockChain();
            server = new WebServer(chain);

            System.Console.WriteLine("BlockChainServerNode initialized with an empty chain");

            //If a previous server node state was saved, load it
            chain.Rollback();

            //display the server node status
            chain.status();
            System.Console.WriteLine("enter help for a list of commands");

            bool exit = false;
            string cmd = "";
            //main command loop
            while (!exit)
            {
                System.Console.Write($"Server {nodeId}>");
                cmd = System.Console.ReadLine().Trim().ToLower();
                string[] cmdArgs = cmd.Split(' ');
                switch(cmdArgs[0])
                {
                    case "help" :
                        help(cmdArgs);
                        break;
                    case "checkpoint":
                        checkpoint(cmdArgs);
                        break;
                    case "rollback":
                        rollback(cmdArgs);
                        break;
                    case "exit":
                        checkpoint(cmdArgs);
                        exit = true;
                        break;
                    case "init":
                        init(cmdArgs);
                        break;
                    case "echo":
                        echo(cmdArgs);
                        break;
                    case "status":
                        status(cmdArgs);
                        break;
                    case "list":
                        list(cmdArgs);
                        break;
                    case "publickey":
                        publicKey(cmdArgs);
                        break;
                    case "mine":
                        Mine(cmdArgs);
                        break;
                    case "balance":
                        Balance(cmdArgs);
                        break;
                    case "validate":
                        validate();
                        break;
                    case "resolve":
                        Resolve(false);
                        break;
                    case "miner.start":
                        Miner_Start(cmdArgs);
                        break;
                    case "miner.stop":
                        Miner_Stop();
                        break;
                }
            }
        }

        static void help(string[] cmdArgs)
        {
            System.Console.WriteLine("  help              : This message ");
            System.Console.WriteLine("  status            : Current Server Node State ");
            System.Console.WriteLine("  exit              : Shut down the service ");
            System.Console.WriteLine("  checkpoint        : saves the BlockChain state to new checkpoint file");
            System.Console.WriteLine("  rollback          : rolls-back the blockChain to the last checkpoint");
            System.Console.WriteLine("  init              : Re-initialize with an empty BlockChain");
            System.Console.WriteLine("  list transactions : list pending transactions");
            System.Console.WriteLine("  list blocks       : list blocks");
            System.Console.WriteLine("  validate          : validate chain");
            System.Console.WriteLine("  resolve           : resolve chain with registered nodes");
            System.Console.WriteLine("  miner.start       : address [seconds] - starts miner thread");
            System.Console.WriteLine("  miner.stop        : stops miner thread");
        }

        //save the current server node state to a checkpoint file
        static void checkpoint(string[] cmdArgs)
        {
            string result = chain.CheckPoint();
            System.Console.WriteLine($"Checkpoint written: {result}");
        }

        //rollback the server node state to the last saved checkpoint
        static void rollback(string[] cmdArgs)
        {
            chain.Rollback();
        }

        //initialize the server node state (clear all BlockChain transactions)
        static void init(string[] cmdArgs)
        {
            System.Console.WriteLine("This Reinitialize with an empty BlockChain");
            System.Console.Write("Are you sure (Y/N)? ");
            string response = System.Console.ReadLine().Trim().ToLower();
            if(response.StartsWith("y"))
            {
                chain = new BlockChain();
                System.Console.WriteLine("Initialized with an empty BlockChain");
            }
        }

        //sets echoing of web-server requests and responses to the console (on or off)
        static void echo(string[] cmdArgs)
        {
            if (cmdArgs[1] == "on")
                chain.echo = true;
            else
                chain.echo = false;
        }

        //display the server node state on the console
        static void status(string[] cmdArgs)
        {
            chain.status();
        }

        //list blocks or uncommitted transactions to the console
        static void list(string[] cmdArgs)
        {
            switch(cmdArgs[1])
            {
                case "transactions":
                    chain.list_currentTransactions();
                    break;
                case "blocks":
                    chain.list_blocks();
                    break;
                default:
                    System.Console.WriteLine("list transactions|blocks");
                    break;
            }
        }

        //exports the server nodes public key
        static void publicKey(string[] cmdArgs)
        {
            chain.ExportPublicKey();
        }

        //Loads the specified crypto provider 
        private static CryptoProvider.ICryptoProvider LoadCryptoProvider(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);

            //use reflection to load the Crypto Provider assembly
            System.Reflection.Assembly ptrAssembly = System.Reflection.Assembly.LoadFile(assembly);
            foreach (Type item in ptrAssembly.GetTypes())
            {
                if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(CryptoProvider.ICryptoProvider)))
                {
                    return (CryptoProvider.ICryptoProvider)Activator.CreateInstance(item);
                }
            }
            throw new Exception("Invalid DLL, Interface not found!");
        }

        private static void Balance(string[] cmdArgs)
        {
            string[] addresses = cmdArgs[1].Split(',');
            List<Output> balances = chain.GetBalance(addresses.ToList());
            foreach(Output o in balances)
            {
                System.Console.WriteLine(o.address + " = " + o.amount);
            }
        }

        //send send mine request to the server node
        static void Mine(string[] cmdArgs)
        {
            chain.Mine(cmdArgs[1]);
        }

        //send validate BlockChain request to the server node
        static void validate()
        {
            System.Console.WriteLine(string.Format("Chain {0} valid",chain.Validate()?"is":"is not"));
        }

        //sends a Resolve request to the server node (communicates with registered server nodes to determine consensus)
        static void Resolve(bool fullChain=true)
        {
            System.Console.WriteLine(chain.Resolve(fullChain));
        }

        static void Miner_Start(string[] cmdArgs)
        {
            int seconds = 30;
            if(cmdArgs.Length > 2)
                int.TryParse(cmdArgs[2], out seconds);
            chain.Miner_Start(cmdArgs[1],seconds);
        }

        static void Miner_Stop()
        {
            chain.Miner_Stop();
        }

    }
}
