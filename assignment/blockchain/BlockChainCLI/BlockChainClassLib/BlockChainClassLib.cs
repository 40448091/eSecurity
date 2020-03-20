using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

/**********************************************************************
 * Block Chain command line client library
 * Author: Paul Haines (March 2020)
 * Provides interface between the command line client and 
 * the BlockChain server
 ***********************************************************************/
namespace BlockChainClassLib
{
    //transaction object
    public class Transaction
    {
        public string id { get; set; }
        public List<Input> InputAddressList = new List<Input>();
        public List<Output> OutputList = new List<Output>();
    }

    //BlockChain Input object (inputs to BlockChain transactions)
    public class Input
    {
        public string address { get; set; }             //One of the sources used by the BlockChain server 
        public string signature { get; set; }           //Used by the BlockChain server to verify the owner of the Input address
        public string base64PublicKey { get; set; }     //Used by the BlockChain server to verify the owner of the Input address

        //constructor
        public Input(string address, string signature, string base64PublicKey)
        {
            this.address = address;
            this.signature = signature;
            this.base64PublicKey = base64PublicKey;
        }

        //Uses the cryptographic provider specified to verify the owner of the address
        //Uses the address, public key and signature from input itself
        public bool HasValidSignature(CryptoProvider.ICryptoProvider provider)
        {
            return CryptoProvider.AddressEncoder.Verify(address, signature, base64PublicKey, provider);
        }
    }

    //BlockChain Output object (outputs from BlockChain transactions)
    public class Output
    {
        public string address { get; set; }
        public int amount { get; set; }

        //parameter less constructor (required for json serialization and deserialization
        public Output()
        {

        }

        //primary constructor
        public Output(string address, int amount)
        {
            this.address = address;
            this.amount = amount;
        }
    }

    //BlockChain Mine object (used to deserialize the result of a mine request sent to the BlockChain server) 
    public class Mine
    {
        public string Message { get; set; }
        public int Index { get; set; }
        public int Proof { get; set; }
        public string PreviousHash { get; set; }
    }

    //Command processor: interprets commands entered in the console and returns returns results to the console
    public class CommandProcessor
    {
        CryptoProvider.ICryptoProvider _cryptoProvider = null; 
        string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string rootUrl = "http://{host}:{port}";
        string _walletFilePath = "";
        WalletLib.Wallet _wallet = new WalletLib.Wallet();

        //used for adding test start and end log entries
        string TestId = "";

        //construct a command processor using the Crypto Provider, host address and port provided
        public CommandProcessor(string cryptoProviderName, string host, string port)
        {
            //create the root URL to the BlockChain server using the host and port provided
            rootUrl = $"http://{host}:{port}";

            //Look for the name of the CryptoProvider in the first argument
            //and use this to derive the dll name and key file names
            string cryptoProviderFilename = Path.Combine(appDir, cryptoProviderName + "_CryptoProvider.dll");
            //string keyFilename = Path.Combine(appDir, cryptoProviderName + ".key");
            _walletFilePath = Path.Combine(appDir, cryptoProviderName + ".wal");

            //load the crypto provider dll
            if (File.Exists(cryptoProviderFilename))
                _cryptoProvider = LoadCryptoProvider(cryptoProviderFilename);
            else
                throw new Exception(string.Format("Crypto Provider not found : {0}", cryptoProviderFilename));

            //load the wallet if it exists (.wal file in the application directory), create one if it doesn't
            Wallet_Load();
        }

        //Loads the specified crypto provider ({cryptoprovider name}.dll)
        //crypto provider must implement the ICryptoProvider interface
        private static CryptoProvider.ICryptoProvider LoadCryptoProvider(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);
            //use reflection to load the crypto library dll
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

        //Sends a create Transaction request to the BlockChain server
        public void transaction(Transaction t)
        {
            //serialize the Transaction object
            string json = JsonConvert.SerializeObject(t);

            //build the request url
            string url = rootUrl + "/transactions/new";

            //send the request via the REST Client
            RestClientLib.RestClient client = new RestClientLib.RestClient();
            string jsonResult = client.Post(url, json);

            Console.WriteLine(jsonResult);
        }

        //Sends a mine reqest to the BlockChain server
        public string mine(string address)
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();
            //build the request url and send the Get request
            string url = rootUrl + "/Mine?" + address;
            string json = client.Get(url);

            //deserialize the result into the Mine object
            var json_serializer = new JavaScriptSerializer();
            Mine result = json_serializer.Deserialize<Mine>(json);

            return string.Format("Index={0}, Message={1}, proof={2}, PreviousHash={3}", result.Index, result.Message, result.Proof, result.PreviousHash);
        }

        //Sends a chain request to the BlockChain server (asks the server to return a list of BlockChain objects)
        public string chain()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send the Get request
            string url = rootUrl + "/chain";
            string json = client.Get(url);

            //display the results (json) 
            return json;
        }

        public void publicKey()
        {
            Console.WriteLine(_cryptoProvider.ExportPublicKey());
        }

        //sends an address transaction history request to the BlockChain server
        public string history(string address)
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send the Get request
            string url = rootUrl + "/history?" + address;
            string json = client.Get(url);

            //deserialize the list of transactions retured from the server
            var json_serializer = new JavaScriptSerializer();
            List<string> txList = json_serializer.Deserialize<List<string>>(json);

            StringBuilder b = new StringBuilder();
            //output each transaction returned to the console
            foreach(string tx in txList)
            {
                b.AppendLine(tx);
            }

            return b.ToString();
        }

        //selects the specified address from the wallet
        //Initializes the crypto provider with the public and private keys from the wallet
        public bool Wallet_SelectAddress(int index)
        {
            bool result = false;

            if (_wallet.WalletEntries.Count() > index)
            {
                //if the wallet entry was found at the specified index, 
                //initialize the crypto provider instance with the public and private keys from the wallet
                WalletLib.WalletEntry we = _wallet.WalletEntries[index];
                _cryptoProvider.ImportPublicKey(we.publicKey);
                _cryptoProvider.ImportPrivateKey(we.privateKey);
            }
            else
                result = false;

            return result;
        }

        //creates a new wallet entry
        public string Wallet_CreateAddress()
        {
            //Use the Crypto provider to generate a new public + private key pair
            _cryptoProvider.GenerateKeyPair();
            //export encoded string representations (usually base 64) of the public and private keys
            string privateKey = _cryptoProvider.ExportPrivateKey();
            string publicKey = _cryptoProvider.ExportPublicKey();

            //use the Address Encode mentods of the Cryto provider to create a BitCoin like address from the public key
            string address = CryptoProvider.AddressEncoder.CreateAddress(publicKey);

            //create the wallet entry object and add it to the wallet
            WalletLib.WalletEntry we = new WalletLib.WalletEntry(address, publicKey, privateKey, 0);
            _wallet.WalletEntries.Add(we);

            //return the new entry
            return we.address;
        }

        //return a list of wallet entries
        public List<WalletLib.WalletEntry> Wallet_ListEntries()
        {
            return _wallet.WalletEntries;
        }

        //save the wallet (to a {crypto provider name}.wal file in the application directory) 
        public void Wallet_Save()
        {
            _wallet.Save(_walletFilePath);
        }

        //load the wallet (from a {crypto provider name}.wal file in the application directory)
        //or create a new one if none was found
        public bool Wallet_Load()
        {
            //load the wallet if it exists
            if (File.Exists(_walletFilePath))
            {
                _wallet.Load(_walletFilePath);
                Wallet_SelectAddress(0);
            } else
            {
                Wallet_CreateAddress();
                Wallet_Save();
            }
            return true;
        }

        //find and return a wallet entry from the address specified
        public WalletLib.WalletEntry Wallet_FindEntry(string address)
        {
            return _wallet.WalletEntries.Where(x => x.address == address).FirstOrDefault();
        }

        //creates a new BitCoin like address from the public key specified
        public string CreateAddress(string publicKey)
        {
            return CryptoProvider.AddressEncoder.CreateAddress(publicKey);
        }

        //Creates and address signature (used when creating transactions to send to the BlockChain Server)
        public string SignAddress(string address, string publicKey, string privateKey)
        {
            //initialize public and private keys for the crypto provider
            _cryptoProvider.ImportPublicKey(publicKey);
            _cryptoProvider.ImportPrivateKey(privateKey);

            //call the sign-address method on the Crypto Providers AddressEncoder
            return CryptoProvider.AddressEncoder.SignAddress(address, _cryptoProvider);
        }

        //sends a Balance request to the BlockChain server
        //A list of adresses fromt he client wallet is sent
        public void Wallet_Balance()
        {
            var json_serializer = new JavaScriptSerializer();

            //serialize the list of addresses from the client wallet
            List<string> addressList = _wallet.WalletEntries.Select(x => x.address).ToList();
            string addresses = json_serializer.Serialize(addressList);

            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = rootUrl + "/balance";
            string jsonResult = client.Post(url, addresses);

            //deserialize the list of balances returned
            List<Output> balances = json_serializer.Deserialize<List<Output>>(jsonResult);
            
            //update the wallet entry balance for each address returned
            foreach(Output o in balances)
            {
                WalletLib.WalletEntry we = _wallet.WalletEntries.Where(x => x.address == o.address).FirstOrDefault();
                if (we != null)
                    if (o.amount < 0)
                        we.amount = 0;
                    else
                        we.amount = o.amount;
            }

            //save the wallet
            Wallet_Save();
        }

        public string SelectedAddress()
        {
            return CryptoProvider.AddressEncoder.CreateAddress(_cryptoProvider.ExportPublicKey());
        }

        public string GetAddress(string publicKey)
        {
            return CryptoProvider.AddressEncoder.CreateAddress(publicKey);
        }

        public void Test_Start(string TestId)
        {
            this.TestId = TestId;

            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = $"{rootUrl}/test/start?{TestId}";
            string result = client.Get(url);
        }

        public void Test_End()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = $"{rootUrl}/test/end?{TestId}";
            string result = client.Get(url);
        }

        public void Test_Server_Init()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = $"{rootUrl}/test/init";
            string result = client.Get(url);
        }

        public void Test_Server_Checkpoint()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = $"{rootUrl}/test/checkpoint";
            string result = client.Get(url);
        }

        public void Test_Server_Rollback()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            //send request to the BlockChain Server
            string url = $"{rootUrl}/test/rollback";
            string result = client.Get(url);
        }

    }

}
