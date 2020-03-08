using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Script.Serialization; //System.Web.Extensions.dll

namespace RestClientTest
{
    /*
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
    */

    [TestClass]
    public class UnitTest1
    {
        JavaScriptSerializer json_serializer = new JavaScriptSerializer();

        [TestMethod]
        public void TestED25519()
        {
            string[] args = {"ED25519"};
            BlockChainClient.Program.Main(args);
        }

        [TestMethod]
        public void TestRLWE()
        {
            string[] args = { "RLWE" };
            BlockChainClient.Program.Main(args);
        }

        [TestMethod]
        public void TestCommandLib_RLWE()
        {
            BlockChainClassLib.CommandProcessor cmdProc = new BlockChainClassLib.CommandProcessor("RLWE","localhost","12345");
            cmdProc.transaction("Paul", 100);
            cmdProc.transaction("Simon", 20);
            cmdProc.transaction("Joanne", 30);
            cmdProc.mine();
        }

        [TestMethod]
        public void TestCommandLib_ED25519()
        {
            BlockChainClassLib.CommandProcessor cmdProc = new BlockChainClassLib.CommandProcessor("ED25519", "localhost", "12345");
            cmdProc.transaction("Alice", 200);
            cmdProc.transaction("Bob", 20);
            cmdProc.transaction("Trent", 30);
            cmdProc.mine();
        }


    }
}
