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

        public byte[] KeyValue
        {
            set
            {
                _KeyValue = value;
            }

            get
            {
                return _KeyValue;
            }
        }


        public override string ToString()
        {
            return Convert.ToBase64String(_KeyValue);
        }
    }

    public class PrivateKey : CryptoProvider.IPrivateKey
    {
        byte[] _KeyValue { get; set; }

        public PrivateKey(byte[] value = null)
        {
            if(value == null)
            {
                var seed = new Random().Next();
                var rnd = new Random(seed);
                var _privateKeyBytes = Enumerable.Range(0, 32).Select(x => (byte)rnd.Next(256)).ToArray();
            }
            _KeyValue = value;
        }

        public byte[] KeyValue
        {
            set
            {
                _KeyValue = value;
            }

            get
            {
                return _KeyValue;
            }
        }

        public override string ToString()
        {
            return Convert.ToBase64String(_KeyValue);
        }
    }

    public class ED25519_Provider : CryptoProvider.ICryptoProvider
    {
 
        public string CheckMessage(string message, IPublicKey publicKey)
        {
            throw new NotImplementedException();
        }

        public IKeyPair GenerateKeyPair()
        {
            IPublicKey PublicKey = new PublicKey();

            var seed = new Random().Next();
            var rnd = new Random(seed);

            var _privateKeyBytes = Enumerable.Range(0, 32).Select(x => (byte)rnd.Next(256)).ToArray();
            PrivateKey privateKey = new PrivateKey(_privateKeyBytes);
            

            byte[] publicKey = Cryptographic.Ed25519.PublicKey(_privateKeyBytes);

            IKeyPair = new 

            byte[] message = Encoding.UTF8.GetBytes("This is a secret message");
            byte[] signature = Ed25519.Signature(message, signingKey, publicKey);
            bool signatureValid = Ed25519.CheckValid(signature, message, publicKey);
            Assert.IsTrue(signatureValid, "Test with random seed {0} failed", seed);

            message[0] = (byte)(message[0] ^ 1);
            var signatureValidAfterChange = Ed25519.CheckValid(signature, message, publicKey);
            Assert.IsFalse(signatureValidAfterChange, "Test with random seed {0} failed", seed);




            throw new NotImplementedException();
        }

        public IKeyPair LoadKeyPair(string filepath)
        {
            throw new NotImplementedException();
        }

        public IPublicKey LoadPublicKey(string filepath)
        {
            throw new NotImplementedException();
        }

        public string SignMessage(string messate, IPrivateKey privateKey)
        {
            throw new NotImplementedException();
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

    }
}
