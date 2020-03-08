using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;


namespace BlockChainClassLib
{
    public class Transaction : BlockChain.ITransaction
    {
        public Guid id { get; set; }
        public int Amount { get; set; }
        public string Recipient { get; set; }
        public string Sender { get; set; }
        public string Signature { get; set; }

        public Transaction(string recipient, int amount, CryptoProvider.ICryptoProvider cryptoProvider)
        {
            id = Guid.NewGuid();
            Sender = cryptoProvider.ExportPublicKey();
            Recipient = recipient;
            Amount = amount;

            string message = $"{id}~{Sender}~{Recipient}~{Amount}";
            Signature = cryptoProvider.SignMessage(message);
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


        public CommandProcessor(string cryptoProviderName, string host, string port)
        {
            rootUrl = $"http://{host}:{port}";

            //Look for the name of the CryptoProvider in the first argument
            //and use this to derive the dll name and key file names
            string cryptoProviderFilename = Path.Combine(appDir, cryptoProviderName + "_CryptoProvider.dll");
            string keyFilename = Path.Combine(appDir, cryptoProviderName + ".key");

            if (File.Exists(cryptoProviderFilename))
                _cryptoProvider = LoadCryptoProvider(cryptoProviderFilename);
            else
                throw new Exception(string.Format("Crypto Provider not found : {0}", cryptoProviderFilename));

            //CryptoProvider.ED25519_Provider cryptoProvider = new CryptoProvider.ED25519_Provider();

            //Load the key file if it exists
            if (File.Exists(keyFilename))
                _cryptoProvider.ImportKeyPair(keyFilename);
            else
            {  //if not create a key pair and save
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPair(keyFilename);
                _cryptoProvider.ExportPublicKey(Path.Combine(appDir, cryptoProviderName + ".pub"));
            }

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

        public void transaction(string recipient, int amount)
        {
            Transaction t = new Transaction(recipient, amount, _cryptoProvider);   //TODO: Consider putting a serial number or something in the pre-signed signature field

            var json_serializer = new JavaScriptSerializer();

            //First serialize the transaction
            string json = json_serializer.Serialize(t);
            //t.Signature = cryptoProvider.SignMessage(json);

            string url = rootUrl + "/transactions/new";
            //send
            RestClientLib.RestClient client = new RestClientLib.RestClient();
            string jsonResult = client.Post(url, json);

            Console.WriteLine(jsonResult);
        }

        public void mine()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            string url = rootUrl + "/Mine?" + _cryptoProvider.ExportPublicKey();
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



    }

}
