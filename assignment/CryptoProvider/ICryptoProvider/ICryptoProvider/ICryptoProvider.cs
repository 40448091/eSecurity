using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoProvider
{
    public interface ICryptoProvider
    {
        IKeyPair GenerateKeyPair();
        IKeyPair LoadKeyPair(string filepath);
        IPublicKey LoadPublicKey(string filepath);
        string SignMessage(string messate, IPrivateKey privateKey);
        string CheckMessage(string message, IPublicKey publicKey);
    }

    public interface IKeyPair
    {
        IPublicKey GetPublicKey();
        IPrivateKey GetPrivateKey();
    }

    public interface IPublicKey
    {

    }

    public interface IPrivateKey
    {

    }
}
