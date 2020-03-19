using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/**********************************************************************
 * Very simple implementation of a BitCoin like Wallet
 * Author: Paul Haines (March 2020)
 * 
 * Allows the client to store BitCoin like 
 *   addresses + public key + private key + balance
 * Allows storage and retrieval of wallet contents to and from files
 * *******************************************************************/
namespace WalletLib
{
    public class Wallet
    {
        //List of Wallet Entries
        public List<WalletEntry> _WalletEntries = new List<WalletEntry>();

        //returns the internal list of Wallet Entries
        public List<WalletEntry> WalletEntries {
            get { return _WalletEntries; }
        }

        //Loads the wallet contents from a flat file (address + public key + private key + balance)
        public bool Load(string filepath)
        {
            _WalletEntries.Clear();

            if (System.IO.File.Exists(filepath))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
                {
                    while(!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] items = line.Split('|');
                        _WalletEntries.Add(new WalletEntry(items[0], items[1], items[2], int.Parse(items[3])));
                    }
                    sr.Close();
                }
                return true;
            }
            else
                throw new System.IO.FileNotFoundException("File not found: " + filepath);
        }

        //saves the the wallet contents to a flat file
        public void Save(string filepath)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filepath))
            {
                foreach(WalletEntry we in _WalletEntries)
                {
                    string l = string.Join("|", we.address, we.publicKey, we.privateKey, we.amount.ToString());
                    sw.WriteLine(l);
                }
                sw.Close();
            }
        }

    }

    //WalletEntry object
    public class WalletEntry
    {
        public string address { get; set; }     //BitCoin like address
        public string publicKey { get; set; }   //string representation of the public key
        public string privateKey { get; set; }  //string represnetaion of the private key
        public int amount { get; set; }         //balance

        //WalletEntry object constructor
        public WalletEntry(string address, string publicKey, string privateKey, int amount)
        {
            this.address = address;
            this.publicKey = publicKey;
            this.privateKey = privateKey;
            this.amount = amount;
        }

    }

}
