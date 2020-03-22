using System.Collections.Generic;
using System.Linq;

namespace BlockChain
{
    public class Transaction
    {
        public System.Guid id;
        public long TimeStamp = System.DateTime.UtcNow.Ticks;
        public List<Input> Inputs = new List<Input>();
        public List<Output> Outputs = new List<Output>();
        public string PublicKey;
        public string Signature;
 
        public bool HasValidInputSignatures(CryptoProvider.ICryptoProvider provider)
        {
            bool valid = true;
            foreach (Input i in Inputs)
            {
                valid = valid && i.HasValidSignature(provider);
                if (!valid)
                    break;
            }
            return valid;
        }

        public bool HasValidSignature(CryptoProvider.ICryptoProvider provider)
        {
            return provider.VerifySignature(this.ToString(), Signature, provider.PublicKeyFromBase64(PublicKey));
        }

        public override string ToString()
        {
            string i = string.Join(",", Inputs.Select(x => x.address));
            string o = string.Join(",", Outputs.Select(x => $"({x.address},{x.amount})"));
            return $"ID={id.ToString()},TimeStamp={new System.DateTime(TimeStamp).ToString("yyyy-MM-dd HH:mm:ss.fff")},Inputs[{i}],Outputs[{o}]";

        }
    }

    public class Input
    {
        public string address { get; set; }
        public string signature { get; set; }
        public string base64PublicKey { get; set; }

        public Input(string address, string signature, string base64PublicKey)
        {
            this.address = address;
            this.signature = signature;
            this.base64PublicKey = base64PublicKey;
        }

        public bool HasValidSignature(CryptoProvider.ICryptoProvider provider)
        {
            return CryptoProvider.AddressEncoder.Verify(address, signature, base64PublicKey, provider);
        }
    }

    public class Output
    {
        public string address { get; set; }
        public int amount { get; set; }

        public Output(string address, int amount)
        {
            this.address = address;
            this.amount = amount;
        }
    }

}