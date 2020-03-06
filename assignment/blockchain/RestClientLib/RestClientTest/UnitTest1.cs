using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Script.Serialization; //System.Web.Extensions.dll

namespace RestClientTest
{
    public class Transaction
    {
        public int Amount { get; set; }
        public string Recipient { get; set; }
        public string Sender { get; set; }
        public string Signature { get; set; }
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
            Transaction t = new Transaction { Sender = "abc123", Recipient = "xyz987", Amount = 10, Signature = "" };   //TODO: Consider putting a serial number or something in the pre-signed signature field

            //First serialize the transaction
            string json = json_serializer.Serialize(t);

            


            RestClientLib.RestClient client = new RestClientLib.RestClient();
            string jsonResult = client.Post("http://localhost:12345/transactions/new", json);

            Console.WriteLine(jsonResult);

            Assert.IsTrue(jsonResult != "");
        }

    }
}
