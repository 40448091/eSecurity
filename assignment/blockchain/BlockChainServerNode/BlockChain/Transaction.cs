namespace BlockChainDemo
{
    public class Transaction
    {
        public System.Guid id { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public int Amount { get; set; }
        public string Signature { get; set; }

        public bool VerifySignature(CryptoProvider.ICryptoProvider cryptoProvider)
        {
            CryptoProvider.IPublicKey pubKey = cryptoProvider.PublicKey_FromBase64String(Sender);
            string message = $"{id}~{Sender}~{Recipient}~{Amount}";
            return cryptoProvider.VerifySignature(message, Signature, pubKey);
        }
    }
}