using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

/**********************************************************************
 * Block Chain command line client
 * Author: Paul Haines (March 2020)
 * Provides a very basic command line console interface to the 
 * BlockChain class library functions
 ***********************************************************************/
namespace BlockChainClient
{
    public class Program
    {
        //Current Test identifier
        static string TestId = "";
        static bool exit = false;


        //command line program entry point
        public static void Main(string[] args)
        {
            //retrieve the BlockChain server host address and port from the app.config file
            string host = System.Configuration.ConfigurationManager.AppSettings["host"];
            string port = System.Configuration.ConfigurationManager.AppSettings["port"];

            //Get cryptoProvider library to load (dynamically) from the app.config file  
            string cryptoProvider = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];

            //create an instance of the command processor in the BlockChain class library
            BlockChainClassLib.CommandProcessor cmdProc = new BlockChainClassLib.CommandProcessor(cryptoProvider, host, port);

            System.Console.WriteLine("BlockChain Client initialized");
            System.Console.WriteLine(string.Format("host={0}, port={1}, CryptoProvider {2}", host, port, cryptoProvider));
            System.Console.WriteLine("enter help for a list of commands");

            string cmd = "";

            if(args.Length > 1)
            {
                if(args[0].ToLower()=="run")
                    Run(args, cmdProc);
            }

            //main command loop
            while (!exit)
            {
                try
                {
                    string result = "";

                    //read command from the command line
                    System.Console.Write("Client>");
                    cmd = System.Console.ReadLine().Trim().ToLower();
                    string[] cmdArgs = cmd.ToLower().Split(' ');
                    switch (cmdArgs[0])
                    {
                        case "help":
                            help();
                            break;
                        case "transfer":
                            Transfer(cmdArgs, cmdProc);
                            break;
                        case "exit":
                            exit = true;
                            break;
                        case "chain":
                            result = cmdProc.chain();
                            Console.WriteLine(result);
                            break;
                        case "wallet":
                            Wallet(cmdArgs, cmdProc);
                            break;
                        case "history":
                            result = cmdProc.history(cmdArgs[1]);
                            Console.Write(result);
                            break;
                        case "pending":
                            result = cmdProc.PendingTransactions();
                            Console.Write(result);
                            break;
                        case "test":
                            Test(cmdArgs, cmdProc);
                            break;
                        case "run":
                            Run(cmdArgs, cmdProc);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
            }
        }


        static void help()
        {
            System.Console.WriteLine("help              : this message");
            System.Console.WriteLine("exit              : close the client");
            System.Console.WriteLine("transfer          : create a transaction : {enter} for interactive or");
            System.Console.WriteLine("                  :                        from=addr1 from=addrn to=addr amount=99 change=address");
            //System.Console.WriteLine("mine {address}    : mine a new block place coins in {address}");
            System.Console.WriteLine("chain             : get the chain");
            System.Console.WriteLine("transactions      : get the chain");
            System.Console.WriteLine("wallet add        : creates a new address and adds to the wallet");
            System.Console.WriteLine("wallet list       : lists addresses in the wallet");
            System.Console.WriteLine("wallet load       : load wallet file");
            System.Console.WriteLine("wallet save       : save wallet file");
            System.Console.WriteLine("wallet balance    : save wallet file");
            System.Console.WriteLine("history {address} : list transaction history for {address}");
            System.Console.WriteLine();
            System.Console.WriteLine("Test Server-side Commands:");
            System.Console.WriteLine("test miner start  address [seconds] : start miner thread");
            System.Console.WriteLine("test miner stop   : stop miner thread ");
            System.Console.WriteLine("test start        : add start test marker to server logs");
            System.Console.WriteLine("test end          : add end test marker to server logs");
            System.Console.WriteLine("test server_init  : initialize the server, clear the block chain");
            System.Console.WriteLine("test checkpoint   : save checkpoint");
            System.Console.WriteLine("test rollback     : rollback to last checkpoint");
            System.Console.WriteLine("test run {file}   : run test command file");
        }


        static void Transfer(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            if(cmdArgs.Length == 1)
                InteractiveCreateTransaction(cmdArgs, cmdProc);
            else
            {
                List<string> from = new List<string>();
                string to = "";
                int amount = 0;
                string change = "";

                foreach (string arg in cmdArgs)
                {
                    string[] a = arg.Split('=');
                    switch (a[0])
                    {
                        case "from":
                            from.Add(a[1]);
                            break;
                        case "to":
                            to = a[1];
                            break;
                        case "amount":
                            amount = int.Parse(a[1]);
                            break;
                        case "change":
                            change = a[1];
                            break;
                    }
                }

                CreateTransaction(from, to, amount, change, cmdProc);
            }

        }


        //Wallet sub functions
        static void Wallet(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            switch (cmdArgs[1])
            {
                case "list":
                    int i = 0;
                    System.Console.WriteLine("wallet entries:-");
                    List<WalletLib.WalletEntry> entries = cmdProc.Wallet_ListEntries();
                    foreach (WalletLib.WalletEntry wa in entries)
                    {
                        System.Console.WriteLine(i.ToString("000") + ") " + wa.address + " = " + wa.amount);
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
                case "balance":
                    cmdProc.Wallet_Balance();
                    break;

            }
        }

        //retrieves wallet entries and displays them in the console 
        static void ListWalletEntries(BlockChainClassLib.CommandProcessor cmdProc)
        {
            int i = 0;
            System.Console.WriteLine("wallet entries:-");
            List<WalletLib.WalletEntry> entries = cmdProc.Wallet_ListEntries();
            foreach (WalletLib.WalletEntry wa in entries)
            {
                System.Console.WriteLine(i.ToString("000") + ") " + wa.address + " = " + wa.amount);
                i++;
            }
            System.Console.WriteLine("");
        }

        //Provides an interactive mechanism for the user to create blockchain transactions
        static void InteractiveCreateTransaction(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            //1) ask the user for a list of input transactions:
            System.Console.WriteLine("Create Transaction:");
            System.Console.WriteLine("Enter Addresses from your Wallet (one on each line, blank to cancel entry):");

            WalletLib.WalletEntry we;

            //build a list of input addresses
            List<string> addressList = new List<string>();
            int i = 1;
            string line = "";
            do
            {
                System.Console.Write(string.Format("Enter Source Address ({0})=", i));
                line = System.Console.ReadLine().Trim();

                if (line == "")
                    break;

                we = cmdProc.Wallet_FindEntry(line);
                if(we != null)
                {
                    addressList.Add(line);
                    i++;
                } else
                {
                    System.Console.WriteLine("address not found");
                }
            } while (line != "");

            if (addressList.Count() == 0)
                return;

            //2) Enter primary payment address
            System.Console.Write("Enter payment address (blank line to cancel transaction)=");
            string paymentAddress = System.Console.ReadLine().Trim();

            if (paymentAddress == "")
                return;

            //3) Enter payment amount
            string paymentAmount = "";
            int amount = 0;
            do
            {
                System.Console.Write("Enter payment amount=");
                paymentAmount = System.Console.ReadLine().Trim();

                if (paymentAmount == "")
                    return;
            } while (!int.TryParse(paymentAmount, out amount));
                

            //Enter address to post any change to (enter "new" to create a new one and add to your wallet)
            System.Console.Write("Enter change address (new to create one)=");
            string changeAddress = System.Console.ReadLine().Trim();

            if (changeAddress == "")
                return;

            if(changeAddress.Trim() == "new")
            {
                changeAddress = cmdProc.Wallet_CreateAddress();
                cmdProc.Wallet_Save();
                System.Console.WriteLine("Address added to your wallet: " + changeAddress);
            }

            CreateTransaction(addressList, paymentAddress, amount, changeAddress, cmdProc);

        }

        static void CreateTransaction(List<string> from, string to, int amount, string change, BlockChainClassLib.CommandProcessor cmdProc)
        {
            WalletLib.WalletEntry we;

            //build the transaction:
            BlockChainClassLib.Transaction t = new BlockChainClassLib.Transaction();
            t.id = Guid.NewGuid();

            foreach (string address in from)
            {
                we = cmdProc.Wallet_FindEntry(address);
                string publicKey = we.publicKey;
                string signature = cmdProc.SignAddress(address, we.publicKey, we.privateKey);
                t.Inputs.Add(new BlockChainClassLib.Input(address, signature, publicKey));
            }

            //add the primary payment address
            t.Outputs.Add(new BlockChainClassLib.Output(to, amount));

            //add the address to collect change
            t.Outputs.Add(new BlockChainClassLib.Output(change, 0));

            //sign the transaction
            we = cmdProc.Wallet_FindEntry(from[0]);
            t.PublicKey = we.publicKey;
            cmdProc.SignTransaction(t, we.publicKey, we.privateKey);

            //Instruct the command processor to submit the transaction
            cmdProc.Transfer(t);
        }

        static void Test(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            switch (cmdArgs[1])
            {
                case "start":
                    TestId = cmdArgs[2];
                    cmdProc.Test_Start(TestId);
                    break;
                case "end":
                    cmdProc.Test_End();
                    break;
                case "server_init":
                    cmdProc.Test_Server_Init();
                    break;
                case "checkpoint":
                    cmdProc.Test_Server_Checkpoint();
                    break;
                case "rollback":
                    cmdProc.Test_Server_Rollback();
                    break;
                case "mine":
                    string result = cmdProc.mine(cmdArgs[2]);
                    Console.WriteLine(result);
                    break;
                case "miner":
                    if (cmdArgs[2] == "start")
                    {
                        cmdProc.Test_Server_Miner_Start(cmdArgs[3]);
                    } else
                        if (cmdArgs[2] == "stop")
                            cmdProc.Test_Server_Miner_Stop();
                    break;
            }
        }

        static void Run(string[] cmdArgs, BlockChainClassLib.CommandProcessor cmdProc)
        {
            if (!File.Exists(cmdArgs[1]))
                return;

            using (StreamReader sr = File.OpenText(cmdArgs[1]))
            {
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    InterpretBatchCommand(line, cmdProc);
                }
            }
        }


        static void InterpretBatchCommand(string cmd, BlockChainClassLib.CommandProcessor cmdProc)
        {
            string[] cmdArgs = cmd.ToLower().Split(' ');
            switch (cmdArgs[0])
            {
                case "transfer":
                    Transfer(cmdArgs, cmdProc);
                    break;
                case "history":
                    string txList = cmdProc.history(cmdArgs[1]);
                    Console.Write(txList);
                    break;
                case "balance":
                    cmdProc.Wallet_Balance();
                    break;
                case "test":
                    Test(cmdArgs, cmdProc);
                    break;
                case "exit":
                    exit = true;
                    break;
            }
        }
    }
}
