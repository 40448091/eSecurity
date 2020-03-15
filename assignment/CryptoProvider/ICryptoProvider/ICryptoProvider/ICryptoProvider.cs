using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoProvider
{
    public interface ICryptoProvider
    {
        string ProviderName();
        bool IsInitialized();
        bool GenerateKeyPair();
        bool ImportKeyPairFromFile(string filepath);
        void ExportKeyPairToFile(string filepath);

        string ExportPublicKey();                   //exports public key to base 64
        void ImportPublicKey(string base64);        //imports public key from base 64
        string ExportPrivateKey();                  //exports private key to base 64
        void ImportPrivateKey(string base64);       //imports private key from base 64

        IPublicKey GetPublicKey();                  //returns the internal public key
        IPublicKey PublicKeyFromBase64(string base64);     //returns the public key from base64 string
        IPrivateKey GetPrivateKey();                //returns the internal private key
        IPrivateKey PrivateKeyFromBase64(string base64);   //returns the private key from base64
       

        string SignMessage(string message);
        bool VerifySignature(string message, string signature, IPublicKey publicKey);
        
}

    public interface IPublicKey
    {
        string ToBase64String();
    }

    public interface IPrivateKey
    {
        string ToBase64String();
    }
}
