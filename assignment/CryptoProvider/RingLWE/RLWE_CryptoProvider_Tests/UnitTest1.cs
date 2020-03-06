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
                p.ExportKeyPair("C:\\temp\\keyPair.rle");
            }
        }

        [TestMethod]
        public void ImportKeyPair()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            if (!p.IsInitialized())
            {
                p.ImportKeyPair("C:\\temp\\keyPair.rle");
                Assert.IsTrue(p.IsInitialized());
            }
        }

        [TestMethod]
        public void ExportPublicKey()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.ImportKeyPair("C:\\temp\\keyPair.rle");
            if (p.IsInitialized())
            {
                p.ExportPublicKey("C:\\temp\\pubKey.rle");
            }
        }

        [TestMethod]
        public void LoadPublicKey()
        {
            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.ImportKeyPair("C:\\temp\\keyPair.rle");
            if (p.IsInitialized())
            {
                IPublicKey pubKey = p.LoadPublicKey("C:\\temp\\pubKey.rle");
                Assert.IsTrue(pubKey != null);
            }
        }

        [TestMethod]
        public void SignMessage()
        {
            string Message = "This is a test message";

            CryptoProvider.RLWE_Provider p = new CryptoProvider.RLWE_Provider();
            p.ImportKeyPair("C:\\temp\\keyPair.rle");

            Assert.IsTrue(p.IsInitialized());

            string signature = p.SignMessage(Message);

            Assert.IsFalse(string.IsNullOrEmpty(signature));

            Console.WriteLine(signature);

            IPublicKey pubKey = p.LoadPublicKey("C:\\temp\\pubKey.rle");

            Assert.IsTrue(pubKey != null);

            bool isValid = p.VerifySignature(Message, signature, pubKey);

            Assert.IsTrue(isValid);

            isValid = p.VerifySignature("This message has been tampered with", signature, pubKey);

            Assert.IsFalse(isValid);
        }
    }
}
