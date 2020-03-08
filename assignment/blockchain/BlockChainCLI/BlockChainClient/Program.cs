using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BlockChainClient
{

    public class Program
    {
        public static void Main(string[] args)
        {
            string host = System.Configuration.ConfigurationManager.AppSettings["host"];
            string port = System.Configuration.ConfigurationManager.AppSettings["port"];
            string cryptoProvider = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];

            BlockChainClassLib.CommandProcessor cmdProc = new BlockChainClassLib.CommandProcessor(cryptoProvider, host,port);

            System.Console.WriteLine("BlockChain Client initialized");
            System.Console.WriteLine(string.Format("host={0}, port={1}, CryptoProvider {2}",host,port,cryptoProvider));
            System.Console.WriteLine("enter help for a list of commands");

            bool exit = false;
            string cmd = "";
            while (!exit)
            {
                System.Console.Write(">");
                cmd = System.Console.ReadLine().Trim().ToLower();
                string[] cmdArgs = cmd.Split(' ');
                switch (cmdArgs[0])
                {
                    case "help":
                        help();
                        break;
                    case "mine":
                        cmdProc.mine();
                        break;
                    case "tran":
                        cmdProc.transaction(cmdArgs[1],int.Parse(cmdArgs[2]));
                        break;
                    case "exit":
                        exit = true;
                        break;
                    case "chain":
                        cmdProc.chain();
                        break;
                    case "expub":
                        cmdProc.publicKey();
                        break;
                }
            }
        }

        static void help()
        {
            System.Console.WriteLine("help   : this message");
            System.Console.WriteLine("exit   : close the client");
            System.Console.WriteLine("tran   : create a new transaction ");
            System.Console.WriteLine("mine   : mine a new block");
            System.Console.WriteLine("chain  : get the chain");
            System.Console.WriteLine("expub  : Export the public key");
        }

    }
}
