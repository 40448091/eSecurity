using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WalletLib
{
    public class Wallet
    {
        public List<WalletEntry> _WalletEntries = new List<WalletEntry>();

        public IEnumerable<WalletEntry> WalletEntries {
            get { return _WalletEntries; }
        }

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

    public class WalletEntry
    {
        public string address { get; set; }
        public string publicKey { get; set; }
        public string privateKey { get; set; }
        public int amount { get; set; }

        public WalletEntry(string address, string publicKey, string privateKey, int amount)
        {
            this.address = address;
            this.publicKey = publicKey;
            this.privateKey = privateKey;
            this.amount = amount;
        }

    }

}
