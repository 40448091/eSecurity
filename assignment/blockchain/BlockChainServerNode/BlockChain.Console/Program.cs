namespace BlockChain.Console
{
    class Program
    {
        static WebServer server = null;
        static CryptoProvider.ICryptoProvider cryptoProvider = null;
        static BlockChain chain = null;

        static void Main(string[] args)
        {
            //CryptoProvider.ICryptoProvider cryptoProvider = new CryptoProvider.ED25519_Provider();
            cryptoProvider = new CryptoProvider.RLWE_Provider();

            chain = new BlockChain(cryptoProvider);
            server = new WebServer(chain);

            System.Console.WriteLine("BlockChainServerNode initialized with an empty chain");
            chain.Rollback();
            chain.status();
            System.Console.WriteLine("enter help for a list of commands");

            bool exit = false;
            string cmd = "";
            while (!exit)
            {
                System.Console.Write(">");
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
                }
            }
        }

        static void help(string[] cmdArgs)
        {
            System.Console.WriteLine("  help            : This message ");
            System.Console.WriteLine("  exit            : Shut down the service ");
            System.Console.WriteLine("  checkpoint      : saves the BlockChain to the specified file");
            System.Console.WriteLine("  load {filename} : loads the BlockChain from the specified file");
            System.Console.WriteLine("  init            : Re-initialize with an empty BlockChain");
            System.Console.WriteLine("  list tran       : list pending transactions");
            System.Console.WriteLine("  list blocks     : list blocks");
        }

        static void checkpoint(string[] cmdArgs)
        {
            string result = chain.CheckPoint();
            System.Console.WriteLine($"Checkpoint written: {result}");
        }

        static void rollback(string[] cmdArgs)
        {
            chain.Rollback();
        }
        static void init(string[] cmdArgs)
        {
            System.Console.WriteLine("This Reinitialize with an empty BlockChain");
            System.Console.Write("Are you sure (Y/N)? ");
            string response = System.Console.ReadLine().Trim().ToLower();
            if(response.StartsWith("y"))
            {
                chain = new BlockChain(cryptoProvider);
                System.Console.WriteLine("Initialized with an empty BlockChain");
            }
        }

        static void echo(string[] cmdArgs)
        {
            if (cmdArgs[1] == "on")
                chain.echo = true;
            else
                chain.echo = false;
        }

        static void status(string[] cmdArgs)
        {
            chain.status();
        }

        static void list(string[] cmdArgs)
        {
            switch(cmdArgs[1])
            {
                case "tran":
                    chain.list_currentTransactions();
                    break;
                case "blocks":
                    chain.list_blocks();
                    break;
                default:
                    System.Console.WriteLine("list tran|blocks");
                    break;
            }
        }

        static void publicKey(string[] cmdArgs)
        {
            chain.ExportPublicKey();
        }


    }
}
