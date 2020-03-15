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

            if (string.IsNullOrEmpty(cryptoProvider))
                cryptoProvider = args[0];

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
                        cmdProc.mine(cmdArgs[1]);
                        break;
                    case "tran":
                        cmdProc.transaction(null);
                        break;
                    case "exit":
                        exit = true;
                        break;
                    case "chain":
                        cmdProc.chain();
                        break;
                    case "wallet":
                        Wallet(cmdArgs, cmdProc);
                        break;
                    case "address":
                        string address = cmdProc.SelectedAddress();
                        System.Console.WriteLine("Selected Address = " + address);
                        break;
                }
            }
        }

        static void help()
        {
            System.Console.WriteLine("help        : this message");
            System.Console.WriteLine("exit        : close the client");
            System.Console.WriteLine("tran        : create a new transaction ");
            System.Console.WriteLine("mine        : mine a new block");
            System.Console.WriteLine("chain       : get the chain");
            System.Console.WriteLine("address     : display selected address");
            System.Console.WriteLine("wallet add  : creates a new address and adds to the wallet");
            System.Console.WriteLine("wallet list : lists addresses in the wallet");
            System.Console.WriteLine("wallet load : load wallet file");
            System.Console.WriteLine("wallet save : save wallet file");
        }

        static void Wallet(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            switch(cmdArgs[1])
            {
                case "list":
                    int i = 0;
                    System.Console.WriteLine("wallet entries:-");
                    List<WalletLib.WalletEntry> entries = cmdProc.Wallet_ListEntries();
                    foreach (WalletLib.WalletEntry wa in entries)
                    {
                        System.Console.WriteLine(i.ToString("000") + ") " + wa.address + ", " + wa.amount);
                        i++;
                    }
                    System.Console.WriteLine("");
                    break;
                case "select":
                    i = int.Parse(cmdArgs[2]);
                    cmdProc.Wallet_SelectAddress(i);
                    break;
                case "add":
                    cmdProc.Wallet_CreateAddress();
                    break;
                case "load":
                    cmdProc.Wallet_Load();
                    break;
                case "save":
                    cmdProc.Wallet_Save();
                    break;
            }
        }

    }
}
