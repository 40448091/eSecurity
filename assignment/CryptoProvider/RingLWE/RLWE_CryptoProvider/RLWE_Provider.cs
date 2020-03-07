using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoProvider;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.RLWE;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;
using VTDev.Libraries.CEXEngine.Tools;


namespace CryptoProvider
{
    public class PublicKey : IPublicKey
    {
        //Private and public keys appear reversed in this signing inplementation
        //So the external public key actually uses the internal private key and vice versa
        RLWEPrivateKey _keyValue = null;

        public PublicKey(RLWEPrivateKey publicKey)
        {
            _keyValue = publicKey;
        }

        public PublicKey(byte[] value = null)
        {
            _keyValue = RLWEPrivateKey.From(value);
        }

        public RLWEPrivateKey Key {
            get { return _keyValue;  }
            set { _keyValue = value; }
        }

        public byte[] Bytes
        {
            get { return _keyValue.ToBytes(); }
            set { _keyValue = RLWEPrivateKey.From(value); }
        }

        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    public class PrivateKey : CryptoProvider.IPrivateKey
    {
        //Private and public keys appear reversed in this signing inplementation
        //So the external public key actually uses the internal private key and vice versa

        RLWEPublicKey _keyValue = null;

        public PrivateKey (RLWEPublicKey privateKey)
        {
            _keyValue = privateKey;
        }

        public PrivateKey(byte[] value = null)
        {
            _keyValue = RLWEPublicKey.From(value);
        }

        public RLWEPublicKey Key
        {
            get { return _keyValue; }
            set { _keyValue = value; }
        }

        public byte[] Bytes
        {
            get { return _keyValue.ToBytes(); }
            set { _keyValue = RLWEPublicKey.From(value); }
        }

        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }


    public class RLWE_Provider : ICryptoProvider
    {
        PublicKey _publicKey = null;
        PrivateKey _privateKey = null;
       
        public void ExportKeyPair(string filepath)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filepath))
            {
                sw.WriteLine(_privateKey.ToBase64String());
                sw.WriteLine(_publicKey.ToBase64String());
                sw.Close();
            }
        }

        public string ExportPublicKey()
        {
            return _publicKey.ToBase64String(); 
        }

        public void ExportPublicKey(string filepath)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filepath))
            {
                //string b64 = Convert.ToBase64String(_publicKey.Bytes);
                sw.Write(_publicKey.ToBase64String());
                sw.Close();
            }
        }

        public IPublicKey PublicKey_FromBase64String(string b64)
        {
            return  new PublicKey(Convert.FromBase64String(b64));
        }

        public IPrivateKey PrivateKey_FromBase64String(string b64)
        {
            return new PrivateKey(Convert.FromBase64String(b64));
        }

        public bool GenerateKeyPair()
        {
            RLWEParameters mpar = RLWEParamSets.RLWEN512Q12289;
            RLWEKeyGenerator mkgen = new RLWEKeyGenerator(mpar);
            IAsymmetricKeyPair keyPair = mkgen.GenerateKeyPair();

            RLWEPublicKey pubKey = (RLWEPublicKey)keyPair.PublicKey;
            RLWEPrivateKey privKey = (RLWEPrivateKey)keyPair.PrivateKey;

            //Private and public keys appear reversed in this signing inplementation
            _privateKey = new PrivateKey((RLWEPublicKey)keyPair.PublicKey);     
            _publicKey = new PublicKey((RLWEPrivateKey)keyPair.PrivateKey);

            return true;
        }

        public bool ImportKeyPair(string filepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    _privateKey = (PrivateKey)PrivateKey_FromBase64String(b64);     //new PrivateKey(Convert.FromBase64String(b64));
                    b64 = sr.ReadLine();
                    _publicKey = (PublicKey)PublicKey_FromBase64String(b64);        //new PublicKey(Convert.FromBase64String(b64));
                    sr.Close();
                }
                return true;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

        public bool IsInitialized()
        {
            return _privateKey != null && _publicKey != null; 
        }

        public IPublicKey LoadPublicKey(string filepath)
        {
            PublicKey pubKey = null;
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    pubKey = (PublicKey)PublicKey_FromBase64String(b64);        //  new PublicKey(Convert.FromBase64String(b64));
                    sr.Close();
                }
                return pubKey;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

        public string SignMessage(string message)
        {
            RLWEParameters mpar = RLWEParamSets.RLWEN512Q12289;
            string signature = "";
            using (RLWESign sgn = new RLWESign(mpar))
            {
                sgn.Initialize(_privateKey.Key);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                byte[] signatureBytes = sgn.Sign(messageBytes, 0, messageBytes.Length);
                signature = Convert.ToBase64String(signatureBytes);
            }

            return signature;
        }

        public bool VerifySignature(string message, string signature, IPublicKey publicKey)
        {
            bool result = false;

            RLWEParameters mpar = RLWEParamSets.RLWEN512Q12289;

            using (RLWESign sgn = new RLWESign(mpar))
            {
                sgn.Initialize(((PublicKey)publicKey).Key);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] signatureBytes = Convert.FromBase64String(signature);

                result = sgn.Verify(messageBytes, 0, messageBytes.Length, signatureBytes);
            }

            return result;
        }
    }
}
