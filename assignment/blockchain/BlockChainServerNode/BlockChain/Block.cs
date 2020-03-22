using System;
using System.Collections.Generic;


/********************************************************************
 * Simple BitCoin like BlockChain
 * Author: Paul Haines (March 2020)
 * 
 * Based on csharp section of "Learn Blockchains by Building One"
 * By: Daniel van Flymen
 * Original Source: 
 *   https://github.com/dvf/blockchain/tree/master/csharp/BlockChain
 *   https://github.com/dvf/blockchain
 * 
 * Extended by Paul Haines (March 2020) to add 
 * - BitCoin like Transactions
 * - Injection of Crypto Providers (implementing ICryptoProvider interface)
 * - Transaction signing and verification
 ********************************************************************/
namespace BlockChain
{
    public class Block
    {
        public int Index { get; set; }
        public long Timestamp { get; set; }
        public List<Transaction> Transactions { get; set; }
        public int Proof { get; set; }
        public string PreviousHash { get; set; }

        public override string ToString()
        {
            return $"{Index} [{new DateTime(Timestamp).ToString("yyyy-MM-dd HH:mm:ss")}] Proof: {Proof} | PrevHash: {PreviousHash} | Trx: {Transactions.Count}";
        }
    }
}