using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.RLWE;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;


namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSign()
        {
            RLWEParameters mpar = RLWEParamSets.RLWEN512Q12289;
            RLWEKeyGenerator mkgen = new RLWEKeyGenerator(mpar);
            IAsymmetricKeyPair akp = mkgen.GenerateKeyPair();

            string strPubKey = Convert.ToBase64String(akp.PublicKey.ToBytes());
            string strPrivKey = Convert.ToBase64String(akp.PrivateKey.ToBytes());

            RLWEPrivateKey privKey = new RLWEPrivateKey(Convert.FromBase64String(strPrivKey));
            RLWEPublicKey pubKey = new RLWEPublicKey(Convert.FromBase64String(strPubKey));
            IAsymmetricKeyPair akp2 = new RLWEKeyPair(pubKey, privKey);

            using (RLWESign sgn = new RLWESign(mpar))
            {
                sgn.Initialize(akp.PublicKey);

                int sz = sgn.MaxPlainText;
                byte[] data = new byte[200];
                new VTDev.Libraries.CEXEngine.Crypto.Prng.CSPRng().GetBytes(data);

                byte[] code = sgn.Sign(data, 0, data.Length);

                sgn.Initialize(akp.PrivateKey);
                if (!sgn.Verify(data, 0, data.Length, code))
                    throw new Exception("EncryptionKey: private key comparison test failed!");

                sgn.Initialize(akp.PublicKey);
                code = sgn.Sign(new MemoryStream(data));

                sgn.Initialize(akp.PrivateKey);
                if (!sgn.Verify(new MemoryStream(data), code))
                    throw new Exception("EncryptionKey: private key comparison test failed!");
            }
        }
    }
}
