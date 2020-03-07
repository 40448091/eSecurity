using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoProvider
{
    public class PublicKey : IPublicKey
    {
        byte[] _KeyValue { get; set; }

        public PublicKey(byte[] value = null)
        {
            _KeyValue = value;
        }

        public byte[] Bytes
        {
            get { return _KeyValue; }
            set { _KeyValue = value; }
        }

        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    public class PrivateKey : CryptoProvider.IPrivateKey
    {
        byte[] _KeyValue = null;

        public PrivateKey(byte[] value = null)
        {
            _KeyValue = value;
        }

        public byte[] Bytes
        {
            get { return _KeyValue; }
            set { _KeyValue = value; }
        }
        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }

    }

    public class ED25519_Provider : CryptoProvider.ICryptoProvider
    {
        PrivateKey _privateKey = null;
        PublicKey _publicKey = null;

        public string ProviderName()
        {
            return "ED25519";
        }

        public bool GenerateKeyPair()
        {
            var seed = new Random().Next();
            var rnd = new Random(seed);

            var _privateKeyBytes = Enumerable.Range(0, 32).Select(x => (byte)rnd.Next(256)).ToArray();
            _privateKey = new PrivateKey(_privateKeyBytes);
            _publicKey = new PublicKey(Cryptographic.Ed25519.PublicKey(_privateKeyBytes));

            return true;
        }

        /* -------------------------- */

        const string HexString = "0123456789abcdef";

        public static String GetHex(byte[] raw)
        {
            if (raw == null)
            {
                return null;
            }
            var hex = new StringBuilder(2 * raw.Length);
            foreach (byte b in raw)
            {
                hex.Append(HexString[((b & 0xF0) >> 4)]);
                hex.Append(HexString[((b & 0x0F))]);
            }
            return hex.ToString();
        }

        public bool ImportKeyPair(string filepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    _privateKey = (PrivateKey)PrivateKey_FromBase64String(b64);     // new PrivateKey(Convert.FromBase64String(b64));
                    b64 = sr.ReadLine();
                    _publicKey = (PublicKey)PublicKey_FromBase64String(b64);        // new PublicKey(Convert.FromBase64String(b64));
                    sr.Close();
                }
                return true;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

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
                sw.Write(_publicKey.ToBase64String());
                sw.Close();
            }
        }

        public IPublicKey LoadPublicKey(string filepath)
        {
            PublicKey pubKey = null;
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    pubKey = (PublicKey)PublicKey_FromBase64String(b64);        // new PublicKey(Convert.FromBase64String(b64));
                    sr.Close();
                }
                return pubKey;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

        public IPublicKey PublicKey_FromBase64String(string b64)
        {
            return new PublicKey(Convert.FromBase64String(b64));
        }

        public IPrivateKey PrivateKey_FromBase64String(string b64)
        {
            return new PrivateKey(Convert.FromBase64String(b64));
        }


        public string SignMessage(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] signature = Cryptographic.Ed25519.Signature(messageBytes, _privateKey.Bytes, _publicKey.Bytes);
            return Convert.ToBase64String(signature);
        }

        public bool VerifySignature(string message, string signature, IPublicKey publicKey)
        {
            byte[] signatureBytes = Convert.FromBase64String(signature);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            bool signatureValid = Cryptographic.Ed25519.CheckValid(signatureBytes, messageBytes, ((PublicKey)publicKey).Bytes);
            return signatureValid;
        }

        public bool IsInitialized()
        {
            bool isInitialized = (_privateKey != null) && (_privateKey is PrivateKey) && (_privateKey.Bytes != null);
            isInitialized = isInitialized &&  (_publicKey != null) && (_publicKey is PublicKey) && (_publicKey.Bytes != null);
            return isInitialized;
        }
    }
}
