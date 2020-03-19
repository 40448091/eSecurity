using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

/*************************************************************
 * AddressEncode:
 * Author: Paul Haines (March 2020)
 *   Creates BitCoin like addresses from public key 
 *   using the crypto provider supplied
 *************************************************************/
namespace CryptoProvider
{
    public static class AddressEncoder
    {
        /*************************************************************
         * Creates an address string from a base64 encoded public key
         *************************************************************/
        public static string CreateAddress(string base64PublicKey)
        {
            return RipeMd160(Sha256(base64PublicKey));
        }

        /*********************************************
         * Creates a signature for an address string
         *********************************************/
        public static string SignAddress(string address, ICryptoProvider provider)
        {
            return provider.SignMessage(address);
        }

        /***********************************************************
         * Validates an address and public key against a signature 
         ***********************************************************/
        public static bool Verify(string address,string signature, string base64PublicKey, ICryptoProvider provider)
        {
            IPublicKey pubKey = provider.PublicKeyFromBase64(base64PublicKey);
            bool valid = provider.VerifySignature(address,signature,pubKey);
            valid = valid && (CreateAddress(base64PublicKey) == address);
            return valid;
        }

        //RipeMd160 encoder
        public static string RipeMd160(string text)
        {
            // create a ripemd160 object
            RIPEMD160 r160 = RIPEMD160Managed.Create();
            // convert the string to byte
            byte[] myByte = System.Text.Encoding.ASCII.GetBytes(text);
            // compute the byte to RIPEMD160 hash
            byte[] encrypted = r160.ComputeHash(myByte);
            // create a new StringBuilder process the hash byte
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < encrypted.Length; i++)
            {
                sb.Append(encrypted[i].ToString("X2"));
            }
            // convert the StringBuilder to String and convert it to lower case and return it.
            return sb.ToString().ToLower();
        }

        //sha256 encoder
        public static string Sha256(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}
