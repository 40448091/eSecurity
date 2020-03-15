using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;
using Newtonsoft.Json;


namespace BlockChainClassLib
{
    public class Transaction
    {
        public string id { get; set; }
        public List<Input> InputAddressList = new List<Input>();
        public List<Output> OutputList = new List<Output>();
    }

    public class Input
    {
        public string address { get; set; }
        public string signature { get; set; }
        public string base64PublicKey { get; set; }

        public Input(string address, string signature, string base64PublicKey)
        {
            this.address = address;
            this.signature = signature;
            this.base64PublicKey = base64PublicKey;
        }

        public bool HasValidSignature(CryptoProvider.ICryptoProvider provider)
        {
            return CryptoProvider.AddressEncoder.Verify(address, signature, base64PublicKey, provider);
        }
    }

    public class Output
    {
        public string address { get; set; }
        public int amount { get; set; }

        public Output()
        {

        }

        public Output(string address, int amount)
        {
            this.address = address;
            this.amount = amount;
        }
    }

    public class Mine
    {
        public string Message { get; set; }
        public int Index { get; set; }
        //public Transaction[] Transactions { get; set; }
        public int Proof { get; set; }
        public string PreviousHash { get; set; }
    }

    public class CommandProcessor
    {
        CryptoProvider.ICryptoProvider _cryptoProvider = null; 
        string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string rootUrl = "http://{host}:{port}";
        string _walletFilePath = "";
        WalletLib.Wallet _wallet = new WalletLib.Wallet();

        public CommandProcessor(string cryptoProviderName, string host, string port)
        {
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

            //load the wallet if it exists
            Wallet_Load();

        }

        private static CryptoProvider.ICryptoProvider LoadCryptoProvider(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);
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

        public void transaction(Transaction t)
        {
            string json = JsonConvert.SerializeObject(t);

            string url = rootUrl + "/transactions/new";
            //send
            RestClientLib.RestClient client = new RestClientLib.RestClient();
            string jsonResult = client.Post(url, json);

            Console.WriteLine(jsonResult);
        }

        public void mine(string address)
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            string url = rootUrl + "/Mine?" + address;
            string json = client.Get(url);

            var json_serializer = new JavaScriptSerializer();
            Mine result = json_serializer.Deserialize<Mine>(json);
            Console.WriteLine(string.Format("Index={0}, Message={1}, proof={2}, PreviousHash={3}", result.Index, result.Message, result.Proof, result.PreviousHash));
        }

        public void chain()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            string url = rootUrl + "/chain";
            string json = client.Get(url);

            var json_serializer = new JavaScriptSerializer();
            //Mine result = json_serializer.Deserialize<Mine>(json);

            Console.WriteLine(json);
        }
        public void publicKey()
        {
            Console.WriteLine(_cryptoProvider.ExportPublicKey());
        }

        public void pending(string[] cmdArgs)
        {
            throw new Exception("not yet implemented");
        }


        public bool Wallet_SelectAddress(int index)
        {
            bool result = false;

            if (_wallet.WalletEntries.Count() > index)
            {
                WalletLib.WalletEntry we = _wallet.WalletEntries[index];
                _cryptoProvider.ImportPublicKey(we.publicKey);
                _cryptoProvider.ImportPrivateKey(we.privateKey);
            }
            else
                result = false;

            return result;
        }

        public string Wallet_CreateAddress()
        {
            _cryptoProvider.GenerateKeyPair();
            string privateKey = _cryptoProvider.ExportPrivateKey();
            string publicKey = _cryptoProvider.ExportPublicKey();
            string address = CryptoProvider.AddressEncoder.CreateAddress(publicKey);
            WalletLib.WalletEntry we = new WalletLib.WalletEntry(address, publicKey, privateKey, 0);

            _wallet.WalletEntries.Add(we);

            return we.address;
        }

        public List<WalletLib.WalletEntry> Wallet_ListEntries()
        {
            return _wallet.WalletEntries;
        }

        public void Wallet_Save()
        {
            _wallet.Save(_walletFilePath);
        }

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

        public WalletLib.WalletEntry Wallet_FindEntry(string address)
        {
            return _wallet.WalletEntries.Where(x => x.address == address).FirstOrDefault();
        }

        public string CreateAddress(string publicKey)
        {
            return CryptoProvider.AddressEncoder.CreateAddress(publicKey);
        }

        public string SignAddress(string address, string publicKey, string privateKey)
        {
            //initialize public and private keys for the crypto provider
            _cryptoProvider.ImportPublicKey(publicKey);
            _cryptoProvider.ImportPrivateKey(privateKey);
            return CryptoProvider.AddressEncoder.SignAddress(address, _cryptoProvider);
        }

        public void Wallet_Balance()
        {
            var json_serializer = new JavaScriptSerializer();

            List<string> addressList = _wallet.WalletEntries.Select(x => x.address).ToList();
            string addresses = json_serializer.Serialize(addressList);

            RestClientLib.RestClient client = new RestClientLib.RestClient();

            string url = rootUrl + "/balance";
            string jsonResult = client.Post(url, addresses);

            List<Output> balances = json_serializer.Deserialize<List<Output>>(jsonResult);
            
            foreach(Output o in balances)
            {
                WalletLib.WalletEntry we = _wallet.WalletEntries.Where(x => x.address == o.address).FirstOrDefault();
                if (we != null)
                    if (o.amount < 0)
                        we.amount = 0;
                    else
                        we.amount = o.amount;
            }

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
    }

}
