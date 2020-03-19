using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoProvider
{
    /****************************************************************
     * ICrypto Provider : 
     * Author: Paul Haines (March 2020)
     *   Used to create wrappers for public key cryptographic 
     *   algorithms to allow them to be injected into the the 
     *   blockchain (Bitcoin) simulation framework.
     *   All of the following methods must be implemented.
     ****************************************************************/
    public interface ICryptoProvider
    {
        string ProviderName();                          //returns the name of the crypto provider (mainly used in log entries)
        bool IsInitialized();                           //returns true if the provider is initialized with a public + private key pair
        bool GenerateKeyPair();                         //generates a public + private key pair
        bool ImportKeyPairFromFile(string filepath);    //Import a public + private key pair from the specified file
        void ExportKeyPairToFile(string filepath);      //Exports a public + private key pair to the specified file

        string ExportPublicKey();                       //exports public key to a base 64 encoded string
        void ImportPublicKey(string base64);            //imports public key from a base 64 encoded string
        string ExportPrivateKey();                      //exports private key to a base 64 encoded string
        void ImportPrivateKey(string base64);           //imports private key from a base 64 encoded string

        IPublicKey GetPublicKey();                          //returns the internal public key (as a PublicKey object)
        IPublicKey PublicKeyFromBase64(string base64);      //creates a PublicKey object from a base64 encoded string
        IPrivateKey GetPrivateKey();                        //returns the internal private key (as a PrivateKey object)
        IPrivateKey PrivateKeyFromBase64(string base64);    //creates a PrivateKey object from a base64 encoded string
       
        string SignMessage(string message);                                                 //signs the message using the internal PrivateKey
        string SignMessage(string message, IPrivateKey privateKey, IPublicKey publicKey);   //signs the message using the internal PrivateKey

        bool VerifySignature(string message, string signature, IPublicKey publicKey);       //Verifies the specified message using the signature and public key provided
        
}

    /**********************************************************
     * IPublicKey : 
     *   Represents the public key object for a provider
     *   Primarily used to ensure the correct type of public 
     *   key object is passed to a provider
     **********************************************************/
    public interface IPublicKey
    {
        string ToBase64String();
    }

    /**********************************************************
     * IPrivateKey : 
     *   Represents the private key object for a provider
     *   Primarily used to ensure the correct type of private 
     *   key object is passed to a provider
     **********************************************************/
    public interface IPrivateKey
    {
        string ToBase64String();
    }
}
