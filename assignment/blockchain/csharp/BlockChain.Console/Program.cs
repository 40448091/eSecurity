namespace BlockChainDemo.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //CryptoProvider.ICryptoProvider cryptoProvider = new CryptoProvider.ED25519_Provider();
            CryptoProvider.ICryptoProvider cryptoProvider = new CryptoProvider.RLWE_Provider();

            var chain = new BlockChain(cryptoProvider);
            var server = new WebServer(chain);
            System.Console.Read();
        }
    }
}
