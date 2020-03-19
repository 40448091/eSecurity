using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoProvider;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.RLWE;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;
using VTDev.Libraries.CEXEngine.Tools;

/******************************************************************
 * Wrapper for an implementation of Ring-LWE 
 * Author: Paul Haines (March 2020)
 * 
 * implementation of Ring-LWE in C# created by
 * John G. Underhill (Steppenwolfe65)
 * Source:   https://github.com/Steppenwolfe65/RingLWE-NET
 *           See WLWEEEngine Class library project
 * Details:
 *   An implementation based on the description in the paper 
 *   'Efficient Software Implementation of Ring-LWE Encryption
 *   https://eprint.iacr.org/2014/725.pdf and accompanying Github 
 *   project: https://github.com/ruandc/Ring-LWE-Encryption
 ******************************************************************/
namespace CryptoProvider
{
    /******************************************************************
     * Implementation of a Public Key object for the RLWE provider
     ******************************************************************/
    public class PublicKey : IPublicKey
    {
        //NB: Private and public keys appear reversed in this signing inplementation
        //    So the external public key actually uses the internal private key and vice versa
        RLWEPrivateKey _keyValue = null;

        //Constructor
        public PublicKey(RLWEPrivateKey publicKey)
        {
            _keyValue = publicKey;
        }

        //Sets internal public key using byte array provided
        public PublicKey(byte[] value = null)
        {
            _keyValue = RLWEPrivateKey.From(value);
        }

        //NB: Private and public keys appear reversed in this signing inplementation
        //    So the external public key actually uses the internal private key and vice versa
        public RLWEPrivateKey Key {
            get { return _keyValue;  }
            set { _keyValue = value; }
        }

        //Getter and setter for the private key byte array
        public byte[] Bytes
        {
            get { return _keyValue.ToBytes(); }
            set { _keyValue = RLWEPrivateKey.From(value); }
        }

        //returns a base 64 encoded string representation of the "PublicKey" object
        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    public class PrivateKey : CryptoProvider.IPrivateKey
    {
        //NB: Private and public keys appear reversed in this signing inplementation
        //    So the external public key actually uses the internal private key and vice versa
        RLWEPublicKey _keyValue = null;

        //constructor for the private key object (internally a "public key")
        public PrivateKey (RLWEPublicKey privateKey)
        {
            _keyValue = privateKey;
        }

        //constructor for the private key object using the Byte array provided
        public PrivateKey(byte[] value = null)
        {
            _keyValue = RLWEPublicKey.From(value);
        }

        //Getter and setter for the "private" key value
        //NB: Private and public keys appear reversed in this signing inplementation
        //    So the external public key actually uses the internal private key and vice versa
        public RLWEPublicKey Key
        {
            get { return _keyValue; }
            set { _keyValue = value; }
        }

        //Gets and sets the binary (Byte Array) representation for the private key object
        public byte[] Bytes
        {
            get { return _keyValue.ToBytes(); }
            set { _keyValue = RLWEPublicKey.From(value); }
        }

        //return a base 64 encoded represntation of the private key
        public string ToBase64String()
        {
            return Convert.ToBase64String(Bytes);
        }
    }

    /*****************************************************************
     * Implementation of the ICryptoProvider interface as a wrapper
     * to the RLWE cryptographic algorithms
     *****************************************************************/
    public class RLWE_Provider : ICryptoProvider
    {
        PublicKey _publicKey = null;        //internal representation of a private key (initialized by GenerateKeyPair()
        PrivateKey _privateKey = null;      //internal representation of a public key (initialized by GenerateKeyPair()

        //Returns the provider name
        public string ProviderName()
        {
            return "RLWE";
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
        public IPrivateKey GetPrivateKey()
        {
            return _privateKey;
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
        public IPublicKey GetPublicKey()
        {
            return _publicKey;
        }

        //creates a PrivateKey object from a base 64 encoded string
        public IPublicKey PublicKeyFromBase64(string b64)
        {
            return  new PublicKey(Convert.FromBase64String(b64));
        }

        //creates a private key object from a base 64 encoded string
        public IPrivateKey PrivateKeyFromBase64(string b64)
        {
            return new PrivateKey(Convert.FromBase64String(b64));
        }

        //Generates a Key pair using the ED25519 algorithms
        //initializes internal public and private keys, which can then be exported as required
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

        //Imports a key pair from the file specified
        public bool ImportKeyPairFromFile(string filepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    string b64 = sr.ReadLine();
                    _privateKey = (PrivateKey)PrivateKeyFromBase64(b64);     //new PrivateKey(Convert.FromBase64String(b64));
                    b64 = sr.ReadLine();
                    _publicKey = (PublicKey)PublicKeyFromBase64(b64);        //new PublicKey(Convert.FromBase64String(b64));
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

        //signs a message using the internal private and public keys
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

        //signs a message using the private and public keys provided
        public string SignMessage(string message, IPrivateKey privateKey, IPublicKey publicKey = null)
        {
            //NB: public key isn't required by this implementation, so is initialized to null
            RLWEParameters mpar = RLWEParamSets.RLWEN512Q12289;
            string signature = "";
            using (RLWESign sgn = new RLWESign(mpar))
            {
                sgn.Initialize(((PrivateKey)(privateKey)).Key);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                byte[] signatureBytes = sgn.Sign(messageBytes, 0, messageBytes.Length);
                signature = Convert.ToBase64String(signatureBytes);
            }

            return signature;
        }

        //verify provided message with the signature and public key provided
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
