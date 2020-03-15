using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CryptoProvider;

namespace BlockChain
{
    public class BlockChain
    {
        ICryptoProvider _cryptoProvider = null;

        private List<Transaction> _currentTransactions = new List<Transaction>();
        private List<Block> _chain = new List<Block>();
        private List<Node> _nodes = new List<Node>();
        private Block _lastBlock => _chain.Last();

        public bool echo
        {
            get { return Logger.echo; }
            set { Logger.echo = value; }
        }

        public string NodeId { get; private set; }

        private string appDir {
            get {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }

        //ctor
        public BlockChain(CryptoProvider.ICryptoProvider cryptoProvider = null)
        {
            NodeId = Guid.NewGuid().ToString().Replace("-", "");
            CreateNewBlock(proof: 100, previousHash: "1"); //genesis block
            _cryptoProvider = cryptoProvider;

            string cp = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];

            //create or load keys
            string filename = Path.Combine(appDir, cp + ".key");
            if (File.Exists(filename))
                _cryptoProvider.ImportKeyPairFromFile(filename);
            else
            {
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPairToFile(filename);
            }

            Logger.Log(String.Format("BlockChain Initialized : NodeId={0}", NodeId));
        }

        //private functionality
        private void RegisterNode(string address)
        {
            _nodes.Add(new Node { Address = new Uri(address) });
            Logger.Log(String.Format("Node Registered : {0}", address));
        }

        private bool IsValidChain(List<Block> chain)
        {
            Block block = null;
            Block lastBlock = chain.First();
            int currentIndex = 1;
            while (currentIndex < chain.Count)
            {
                block = chain.ElementAt(currentIndex);
                Debug.WriteLine($"{lastBlock}");
                Debug.WriteLine($"{block}");
                Debug.WriteLine("----------------------------");

                //Check that the hash of the block is correct
                if (block.PreviousHash != GetHash(lastBlock))
                    return false;

                //Check that the Proof of Work is correct
                if (!IsValidProof(lastBlock.Proof, block.Proof, lastBlock.PreviousHash))
                    return false;

                lastBlock = block;
                currentIndex++;
            }

            return true;
        }

        private bool ResolveConflicts()
        {
            Logger.Log("Resolving conflicts");

            List<Block> newChain = null;
            int maxLength = _chain.Count;

            foreach (Node node in _nodes)
            {
                Logger.Log(string.Format("getting chain from : {0}", node.Address));

                var url = new Uri(node.Address, "/chain");
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var model = new
                    {
                        chain = new List<Block>(),
                        length = 0
                    };
                    string json = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    var data = JsonConvert.DeserializeAnonymousType(json, model);

                    bool validChain = IsValidChain(data.chain);
                    if (!validChain)
                        Logger.Log("  Chain validation failed");

                    if (data.chain.Count > _chain.Count && validChain)
                    {
                        maxLength = data.chain.Count;
                        newChain = data.chain;
                    }
                }
            }

            if (newChain != null)
            {
                _chain = newChain;
                return true;
            }

            return false;
        }

        private Block CreateNewBlock(int proof, string previousHash = null)
        {
            Logger.Log("Creating new block");

            var block = new Block
            {
                Index = _chain.Count,
                Timestamp = DateTime.UtcNow,
                Transactions = _currentTransactions.ToList(),
                Proof = proof,
                PreviousHash = previousHash ?? GetHash(_chain.Last())
            };

            _currentTransactions.Clear();
            _chain.Add(block);

            Logger.Log(string.Format("Block added Index={0}, previous hash={1}",block.Index,block.PreviousHash));

            return block;
        }

        private int CreateProofOfWork(int lastProof, string previousHash)
        {
            int proof = 0;
            while (!IsValidProof(lastProof, proof, previousHash))
                proof++;

            return proof;
        }

        private bool IsValidProof(int lastProof, int proof, string previousHash)
        {
            string guess = $"{lastProof}{proof}{previousHash}";
            string result = GetSha256(guess);
            return result.StartsWith("0000");
        }

        private string GetHash(Block block)
        {
            string blockText = JsonConvert.SerializeObject(block);
            return GetSha256(blockText);
        }

        private string GetSha256(string data)
        {
            var sha256 = new SHA256Managed();
            var hashBuilder = new StringBuilder();

            byte[] bytes = Encoding.Unicode.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);

            foreach (byte x in hash)
                hashBuilder.Append($"{x:x2}");

            return hashBuilder.ToString();
        }

        //web server calls
        public string Mine(string address)      //miner is the public key of the client mining
        {
            Logger.Log(string.Format("Mining for new block for ID={0}", address));

            int proof = CreateProofOfWork(_lastBlock.Proof, _lastBlock.PreviousHash);

            //need to sign this with the node credentials
            Transaction tx = new Transaction();
            tx.id = Guid.NewGuid().ToString();
            tx.InputAddressList = new List<Input>();
            tx.OutputList = new List<Output>();

            tx.OutputList.Add(new Output(address, 5));

            CreateTransaction(tx);
            Block block = CreateNewBlock(proof);

            var response = new
            {
                Message = "New Block Forged",
                Index = block.Index,
                Transactions = block.Transactions.ToArray(),
                Proof = block.Proof,
                PreviousHash = block.PreviousHash
            };

            Logger.Log(string.Format("Mined new block Index={0}, Proof={1}, previous Hash={2}", block.Index, block.Proof, block.PreviousHash));

            return JsonConvert.SerializeObject(response);
        }

        internal string GetFullChain()
        {
            var response = new
            {
                chain = _chain.ToArray(),
                length = _chain.Count
            };

            return JsonConvert.SerializeObject(response);
        }

        internal string RegisterNodes(string[] nodes)
        {
            var builder = new StringBuilder();
            foreach (string node in nodes)
            {
                string url = $"http://{node}";
                RegisterNode(url);
                builder.Append($"{url}, ");
            }

            builder.Insert(0, $"{nodes.Count()} new nodes have been added: ");
            string result = builder.ToString();
            return result.Substring(0, result.Length - 2);
        }

        internal string Consensus()
        {
            bool replaced = ResolveConflicts();
            string message = replaced ? "was replaced" : "is authoritive";
            
            var response = new
            {
                Message = $"Our chain {message}",
                Chain = _chain
            };

            return JsonConvert.SerializeObject(response);
        }

        internal int CreateTransaction(Transaction trx)
        {
            Logger.Log(string.Format("Adding Transaction id={0} ", trx.id));


            if (trx.HasValidInputSignatures(_cryptoProvider))
            {
                Logger.Log(string.Format("Signature is valid, Transaction added id={0}", trx.id));
                _currentTransactions.Add(trx);
            }
            else
            {
                Logger.Log(string.Format("Signature validation failed, transaction rejected id={0}", trx.id));
                throw new Exception("Message does not match signature");
            }

            return _lastBlock != null ? _lastBlock.Index + 1 : 0;
        }

        public string CheckPoint()
        {
            Logger.Log("Saving checkpoint");

            string checkpointDir = Path.Combine(this.appDir, "checkpoints");
            if (!Directory.Exists(checkpointDir))
                Directory.CreateDirectory(checkpointDir);

            string filename = System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".chk";
            string filepath = Path.Combine(checkpointDir, filename );

            using (StreamWriter writer = File.CreateText(filepath))
            {
                string line = "";

                line = String.Join("|",NodeId, _cryptoProvider.ProviderName(),_currentTransactions.Count().ToString());
                writer.WriteLine(line);

                CheckPointTransactions(_currentTransactions,writer);

                writer.WriteLine(_chain.Count());
                foreach (Block b in _chain)
                {
                    line = string.Join("|", b.Index, b.Timestamp, b.Proof, b.PreviousHash, b.Transactions.Count);
                    writer.WriteLine(line);
                    CheckPointTransactions(b.Transactions,writer);
                }
            }

            Logger.Log($"Checkpoint saved : {filename}");

            return filename;
        }

        private void CheckPointTransactions(List<Transaction> txs, StreamWriter writer)
        {
            string line;
            foreach (Transaction tx in txs)
            {
                line = JsonConvert.SerializeObject(tx);
                writer.WriteLine(line);
            }
        }

        public bool Rollback(string filename="")
        {
            string checkpointDir = Path.Combine(this.appDir, "checkpoints");
            string[] files = Directory.GetFiles(checkpointDir);

            if ((filename == "") && files.Count()>0)
                filename = files.OrderBy(x => x).Reverse().First();

            if (string.IsNullOrEmpty(filename))
                return false;

            string filepath = Path.Combine(checkpointDir, filename);

            Logger.Log($"Rolling back, loading previous : {filepath}");

            using (StreamReader sr = File.OpenText(filepath))
            {
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    JsonConvert.DeserializeObject(line);
                }
                sr.Close();
            }

            Logger.Log("Rolling complete");

            return false;
        }

        public void status()
        {
            string host = System.Configuration.ConfigurationManager.AppSettings["host"];
            string port = System.Configuration.ConfigurationManager.AppSettings["port"];
            
            System.Console.WriteLine(string.Format("NodeId={0}, Host={1}, Port={2}, CryptoProvider={3}",NodeId,host,port, _cryptoProvider.ProviderName()));
            System.Console.WriteLine(string.Format("Current Transactions={0}", _currentTransactions.Count()));
            System.Console.WriteLine(string.Format("Blocks in Chain={0}", _chain.Count()));
        }

        public void list_currentTransactions()
        {
            foreach(Transaction tx in _currentTransactions)
            {
                string line = JsonConvert.SerializeObject(tx);
                System.Console.WriteLine(line);
            }
        }

        public void list_blocks()
        {
            foreach (Block b in _chain)
            {
                string line = JsonConvert.SerializeObject(b);
                System.Console.WriteLine(line);
            }
        }

        public void ExportPublicKey()
        {
            System.Console.WriteLine(string.Format("Public Key={0}", _cryptoProvider.ExportPublicKey()));
        }
    }
}
