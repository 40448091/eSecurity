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

namespace BlockChainDemo
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

            //create or load keys
            string filename = Path.Combine(appDir, "node.key");
            if (File.Exists(filename))
                _cryptoProvider.ImportKeyPair(filename);
            else
            {
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPair(filename);
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
        internal string Mine(string Recipient)      //miner is the public key of the client mining
        {
            Logger.Log(string.Format("Mining for new block for ID={0}", Recipient));

            int proof = CreateProofOfWork(_lastBlock.Proof, _lastBlock.PreviousHash);

            //need to sign this with the node credentials
            Guid id = Guid.NewGuid();
            string Sender = _cryptoProvider.ExportPublicKey();
            int Amount = 1;
            string message = $"{id}~{Sender}~{Recipient}~{Amount}";
            string Signature = _cryptoProvider.SignMessage(message);

            CreateTransaction(id, Sender, Recipient, Amount, Signature);
            Block block = CreateNewBlock(proof /*, _lastBlock.PreviousHash*/);

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

        internal int CreateTransaction(Guid id, string sender, string recipient, int amount, string signature)
        {
            Logger.Log(string.Format("Adding Transaction id={0}, sender={1}, recipient={2}, amount={3}, signature={4}", id,sender,recipient,amount,signature));

            Transaction tx = new Transaction { id = id, Sender = sender, Recipient = recipient, Amount = amount, Signature = signature };

            bool signatureIsValid = tx.VerifySignature(_cryptoProvider);

            if (signatureIsValid)
            {
                Logger.Log(string.Format("Signature is valid, Transaction added id={0}", id));
                _currentTransactions.Add(tx);
            }
            else
            {
                Logger.Log(string.Format("Signature validation failed, transaction rejected id={0}", id));
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
                line = string.Join("|", tx.id, tx.Sender, tx.Recipient, tx.Amount, tx.Signature);
                writer.WriteLine(line);
            }
        }

        public bool Rollback(string filename="")
        {
            string checkpointDir = Path.Combine(this.appDir, "checkpoints");
            string[] files = Directory.GetFiles(checkpointDir);

            if (filename == "")
                filename = files.OrderBy(x => x).Reverse().First();

            string filepath = Path.Combine(checkpointDir, filename);

            Logger.Log($"Rolling back, loading previous : {filepath}");

            using (StreamReader sr = File.OpenText(filepath))
            {
                string line = sr.ReadLine();
                string[] fields = line.Split('|');
                NodeId = fields[0];
                _currentTransactions = RollbackTransactions(int.Parse(fields[2]),sr);

                _chain = new List<Block>();
                line = sr.ReadLine();
                int blockCount = int.Parse(line);
                for(int i=0;i<blockCount;i++)
                {
                    line = sr.ReadLine();
                    fields = line.Split('|');
                    Block b = new Block { Index = int.Parse(fields[0]), Timestamp = DateTime.Parse(fields[1]), Proof = int.Parse(fields[2]), PreviousHash = fields[3], Transactions = new List<Transaction>() };
                    int txCount = int.Parse(fields[4]);
                    b.Transactions = RollbackTransactions(txCount, sr);
                    _chain.Add(b);
                }
            }

            Logger.Log("Rolling complete");

            return false;
        }

        private List<Transaction> RollbackTransactions(int txCount, StreamReader reader)
        {
            List<Transaction> txs = new List<Transaction>();
            string line;
            string[] fields;
            for (int i = 0; i < txCount; i++)
            {
                line = reader.ReadLine();
                fields = line.Split('|');
                Transaction tx = new Transaction { id = Guid.Parse(fields[0]), Sender = fields[1], Recipient = fields[2], Amount = int.Parse(fields[3]), Signature = fields[4] };
                txs.Add(tx);
            }
            return txs;
        }

        public void status()
        {
            System.Console.WriteLine(string.Format("NodeId={0}",NodeId));
            System.Console.WriteLine(string.Format("Current Transactions={0}", _currentTransactions.Count()));
            System.Console.WriteLine(string.Format("Blocks in Chain={0}", _chain.Count()));
        }

        public void list_currentTransactions()
        {
            foreach(Transaction tx in _currentTransactions)
            {
                System.Console.WriteLine("id={0}, Sender={1}, Recpiient={2}, Amount={3}, Signature={4}", tx.id, tx.Sender, tx.Recipient, tx.Amount, tx.Signature);
            }
        }

        public void list_blocks()
        {
            foreach (Block b in _chain)
            {
                System.Console.WriteLine("Block: Index={0}, Timestamp={1}, proof={2}, previousHash={3}",b.Index,b.Timestamp,b.Proof,b.PreviousHash);
                foreach (Transaction tx in b.Transactions)
                {
                    System.Console.WriteLine(" Transaction: id={0}, Sender={1}, Recpiient={2}, Amount={3}, Signature={4}", tx.id, tx.Sender, tx.Recipient, tx.Amount, tx.Signature);
                }
            }
        }

        public void ExportPublicKey()
        {
            System.Console.WriteLine(string.Format("Public Key={0}", _cryptoProvider.ExportPublicKey()));
        }
    }
}
