using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Script.Serialization; //System.Web.Extensions.dll

namespace RestClientTest
{
    public class Transaction
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
        public Transaction[] Transactions { get; set; }
        public int Proof { get; set; }
        public string PreviousHash { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        JavaScriptSerializer json_serializer = new JavaScriptSerializer();

        [TestMethod]
        public void TestMine()
        {
            RestClientLib.RestClient client = new RestClientLib.RestClient();

            string json = client.Get("http://localhost:12345/Mine");
            Mine result = json_serializer.Deserialize<Mine>(json);

            Console.WriteLine(result.ToString());

            Assert.IsTrue(result != null);
        }

        [TestMethod]
        public void TestAddTransaction()
        {
            //CryptoProvider.ED25519_Provider cryptoProvider = new CryptoProvider.ED25519_Provider();
            CryptoProvider.RLWE_Provider cryptoProvider = new CryptoProvider.RLWE_Provider();
            cryptoProvider.GenerateKeyPair();

            Transaction t = new Transaction("Paul Haines",10, cryptoProvider);   //TODO: Consider putting a serial number or something in the pre-signed signature field
  
            //First serialize the transaction
            string json = json_serializer.Serialize(t);
            //t.Signature = cryptoProvider.SignMessage(json);

            //add the signature
            //json = json_serializer.Serialize(t);
            
            //send
            RestClientLib.RestClient client = new RestClientLib.RestClient();
            string jsonResult = client.Post("http://localhost:12345/transactions/new", json);

            Console.WriteLine(jsonResult);

            Assert.IsTrue(jsonResult != "");
        }

    }
}
