using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoProvider
{
    public interface ICryptoProvider
    {
        bool IsInitialized();
        bool GenerateKeyPair();
        bool ImportKeyPair(string filepath);
        void ExportKeyPair(string filepath);

        string ExportPublicKey();
        void ExportPublicKey(string filepath);

        IPublicKey LoadPublicKey(string filepath);
        string SignMessage(string message);
        bool VerifySignature(string message, string signature, IPublicKey publicKey);
    }

    public interface IPublicKey
    {

    }

    public interface IPrivateKey
    {

    }
}
