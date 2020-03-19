using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*********************************************************
 * BlockChain Type Library
 * Author: Paul Haines (March 2020)
 *   Used when serializing and deserializing BlockChain 
 *   Transactions and Mine objects between the 
 *   BlockChain web-server node and the client
 *********************************************************/
namespace BlockChain
{
    //
    public interface ITransaction
    {
        Guid id { get; set; }
        int Amount { get; set; }
        string Recipient { get; set; }
        string Sender { get; set; }
        string Signature { get; set; }
    }

    public interface IMine
    {
        string Message { get; set; }
        int Index { get; set; }
        ITransaction[] Transactions { get; set; }
        int Proof { get; set; }
        string PreviousHash { get; set; }
    }
}
