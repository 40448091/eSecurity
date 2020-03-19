using System;

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
    //server node object
    public class Node
    {
        public Uri Address { get; set; }
    }
}