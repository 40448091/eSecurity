using System.Collections.Generic;

namespace BlockChain
{
    public class Transaction
    {
        public string id { get; set; }
        public System.DateTime TimeStamp = System.DateTime.Now;
        public List<Input> InputAddressList = new List<Input>();
        public List<Output> OutputList = new List<Output>();
 
        public bool HasValidInputSignatures(CryptoProvider.ICryptoProvider provider)
        {
            bool valid = true;
            foreach (Input i in InputAddressList)
            {
                valid = valid && i.HasValidSignature(provider);
                if (!valid)
                    break;
            }
            return valid;
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