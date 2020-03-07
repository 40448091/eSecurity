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
        bool ImportKeyPair(string filepath);
        void ExportKeyPair(string filepath);

        string ExportPublicKey();
        void ExportPublicKey(string filepath);

        IPublicKey LoadPublicKey(string filepath);
        IPublicKey PublicKey_FromBase64String(string base64);
        IPrivateKey PrivateKey_FromBase64String(string base64);

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
