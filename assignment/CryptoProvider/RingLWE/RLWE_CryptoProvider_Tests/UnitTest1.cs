using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CryptoProvider;

namespace RLWE_CryptoProvider_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GenerateKey()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            bool result = p.GenerateKeyPair();
            Assert.IsTrue(result);
            Assert.IsTrue(p.IsInitialized());
        }

        [TestMethod]
        public void ExportKeyPair()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.GenerateKeyPair();
            if (p.IsInitialized())
            {
                p.ExportKeyPairToFile("C:\\temp\\keyPair.rle");
            }
        }

        [TestMethod]
        public void ImportKeyPair()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            if (!p.IsInitialized())
            {
                p.ImportKeyPairFromFile("C:\\temp\\keyPair.rle");
                Assert.IsTrue(p.IsInitialized());
            }
        }


        [TestMethod]
        public void SignMessage()
        {
            string Message = "This is a test message";

            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.ImportKeyPairFromFile("C:\\temp\\keyPair.rle");

            Assert.IsTrue(p.IsInitialized());

            string signature = p.SignMessage(Message);

            Assert.IsFalse(string.IsNullOrEmpty(signature));

            Console.WriteLine(signature);

            IPublicKey pubKey = p.GetPublicKey();

            Assert.IsTrue(pubKey != null);

            bool isValid = p.VerifySignature(Message, signature, pubKey);

            Assert.IsTrue(isValid);

            isValid = p.VerifySignature("This message has been tampered with", signature, pubKey);

            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void CreateAddress()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.ImportKeyPairFromFile("C:\\temp\\keyPair.rle");
            string b64PublicKey = p.ExportPublicKey();
            string address = CryptoProvider.AddressEncoder.CreateAddress(b64PublicKey);
            string signature = CryptoProvider.AddressEncoder.SignAddress(address, p);
            bool verify = CryptoProvider.AddressEncoder.Verify(address, signature, b64PublicKey, p);

            Assert.IsTrue(verify);
        }
    }
}
