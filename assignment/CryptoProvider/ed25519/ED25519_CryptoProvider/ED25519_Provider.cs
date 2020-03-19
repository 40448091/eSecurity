using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/******************************************************************
 * ED25519 Crypto Provider:
 * Author: Paul Haines (March 2020)
 * 
 * Wrapper for the Ed25519 class created by C# by Hans Wolff
 * Source: https://github.com/hanswolff/ed25519
 *         Ed25519.cs source file
 ******************************************************************/

 namespace CryptoProvider
{
    /******************************************************************
     * Implementation of a Public Key object for the ED25519 provider
     ******************************************************************/
    public class PublicKey : IPublicKey
    {
        //binary (byte array) representation of the Public Key object
        byte[] _KeyValue { get; set; }

        //constructor, initialize with the byte array provided
        public PublicKey(byte[] value = null)
        {
            _KeyValue = value;
        }

        //returns the binary representation of the public key (as an array of bytes)
        //specifically required by parts of the ED25519 algorithms
        public byte[] Bytes
        {
            get { return _KeyValue; }
            set { _KeyValue = value; }
        }

        //return the public key object as a base 64 encoded string
        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    /******************************************************************
     * Implementation of a Private Key object for the ED25519 provider
     ******************************************************************/
    public class PrivateKey : CryptoProvider.IPrivateKey
    {
        //binary (byte array) representation of the Private Key object
        byte[] _KeyValue = null;

        //constructor, initialize with the byte array provided
        public PrivateKey(byte[] value = null)
        {
            _KeyValue = value;
        }

        //returns the binary representation of the private key (as an array of bytes)
        //specifically required by parts of the ED25519 algorithms
        public byte[] Bytes
        {
            get { return _KeyValue; }
            set { _KeyValue = value; }
        }

        //return the private key object as a base 64 encoded string
        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    /*****************************************************************
     * Implementation of the ICryptoProvider interface as a wrapper
     * to the ED25519 cryptographic algorithms
     *****************************************************************/
    public class ED25519_Provider : CryptoProvider.ICryptoProvider
    {
        PrivateKey _privateKey = null;      //internal representation of a private key (initialized by GenerateKeyPair()
        PublicKey _publicKey = null;        //internal representation of a public key (initialized by GenerateKeyPair()

        //Returns the provider name
        public string ProviderName()
        {
            return "ED25519";
        }

        //Generates a Key pair using the ED25519 algorithms
        //initializes internal public and private keys, which can then be exported as required
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

        //internal GetHex method
        const string HexString = "0123456789abcdef";

        //returns a hex string encoding of the raw byte array provided
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

        //Imports a key pair from the file specified
        public bool ImportKeyPairFromFile(string filepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    _privateKey = (PrivateKey)PrivateKeyFromBase64(b64);     // new PrivateKey(Convert.FromBase64String(b64));
                    b64 = sr.ReadLine();
                    _publicKey = (PublicKey)PublicKeyFromBase64(b64);        // new PublicKey(Convert.FromBase64String(b64));
                    sr.Close();
                }
                return true;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

        //Exports the internal key pair to the specified file
        public void ExportKeyPairToFile(string filepath)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filepath))
            {
                sw.WriteLine(_privateKey.ToBase64String());
                sw.WriteLine(_publicKey.ToBase64String());
                sw.Close();
            }
        }

        //returns a base 64 encoded string representation of the internal public key
        public string ExportPublicKey()
        {
            return _publicKey.ToBase64String();
        }

        //Imports a public key from a base 64 encoded string 
        public void ImportPublicKey(string base64)
        {
            _publicKey = new PublicKey(Convert.FromBase64String(base64));
        }

        //returns the internal public key as an IPublicKey object
        public IPublicKey GetPublicKey()
        {
            return _publicKey;
        }

        //Exports the internal as a base 64 encoded string
        public string ExportPrivateKey()
        {
            return _privateKey.ToBase64String();
        }

        //Imports the internal private key from a base 64 encoded string
        public void ImportPrivateKey(string base64)
        {
            _privateKey = new PrivateKey(Convert.FromBase64String(base64));
        }

        //returns the internal private key as an IPrivateKey object
        public IPrivateKey GetPrivateKey()
        {
            return _privateKey;
        }

        //creates a PrivateKey object from a base 64 encoded string
        public IPublicKey PublicKeyFromBase64(string b64)
        {
            return new PublicKey(Convert.FromBase64String(b64));
        }

        //creates a private key object from a base 64 encoded string
        public IPrivateKey PrivateKeyFromBase64(string b64)
        {
            return new PrivateKey(Convert.FromBase64String(b64));
        }

        //signs a message using the internal private and public keys
        public string SignMessage(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] signature = Cryptographic.Ed25519.Signature(messageBytes, _privateKey.Bytes, _publicKey.Bytes);
            return Convert.ToBase64String(signature);
        }

        //signs a message using the private and public keys provided
        public string SignMessage(string message, IPrivateKey privateKey, IPublicKey publicKey)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] signature = Cryptographic.Ed25519.Signature(messageBytes, ((PrivateKey)privateKey).Bytes, ((PublicKey)publicKey).Bytes);
            return Convert.ToBase64String(signature);
        }

        //verify provided message with the signature and public key provided
        public bool VerifySignature(string message, string signature, IPublicKey publicKey)
        {
            byte[] signatureBytes = Convert.FromBase64String(signature);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            bool signatureValid = Cryptographic.Ed25519.CheckValid(signatureBytes, messageBytes, ((PublicKey)publicKey).Bytes);
            return signatureValid;
        }

        //returns true if public and private keys are initialized
        public bool IsInitialized()
        {
            bool isInitialized = (_privateKey != null) && (_privateKey is PrivateKey) && (_privateKey.Bytes != null);
            isInitialized = isInitialized &&  (_publicKey != null) && (_publicKey is PublicKey) && (_publicKey.Bytes != null);
            return isInitialized;
        }
    }
}
