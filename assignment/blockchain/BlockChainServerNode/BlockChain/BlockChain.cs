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
using System.Linq;

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

            RegisterNodes();
        }

        //private functionality
        private void RegisterNode(string address)
        {
            _nodes.Add(new Node { Address = new Uri(address) });
            Logger.Log(String.Format("Node Registered : {0}", address));
        }

        private void DeregisterNode(string address)
        {
            Node n = _nodes.Where(x => x.Address.ToString() == address).FirstOrDefault();
            _nodes.Remove(n);
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

            List<Node> inactiveNodes = new List<Node>();

            foreach (Node node in _nodes)
            {
                Logger.Log(string.Format("getting chain from : {0}", node.Address));

                var url = new Uri(node.Address, "/chain");
                var request = (HttpWebRequest)WebRequest.Create(url);

                try
                {
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
                catch (Exception ex)
                {
                    inactiveNodes.Add(node);
                }
            }

            if (newChain != null)
            {
                _chain = newChain;
                return true;
            }

            foreach(Node node in inactiveNodes)
            {
                _nodes.Remove(node);
                Logger.Log(string.Format("Deregistering unresponsive node: {0}",node.Address));
            }

            return false;
        }

        private Block CreateNewBlock(int proof, string previousHash = null)
        {
            Logger.Log("Creating new block");

            var block = new Block
            {
                Index = _chain.Count,
                Timestamp = DateTime.UtcNow.Ticks,
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

            Consensus();

            CreateTransaction(tx,true);
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

        internal void RegisterNodes()
        {
            string nodesList = System.Configuration.ConfigurationManager.AppSettings["registerNodes"];
            string[] nodes = nodesList.Replace(" ", "").Split(',');
            RegisterNodes(nodes);
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

        internal int CreateTransaction(Transaction trx, bool isMining = false)
        {
            Logger.Log(string.Format("Adding Transaction id={0} ", trx.id));


            if (trx.HasValidInputSignatures(_cryptoProvider))
            {
                Logger.Log(string.Format("Signature is valid, Transaction added id={0}", trx.id));

                List<string> inputAddresses = trx.InputAddressList.Select(x => x.address).ToList();

                //TODO : check to see if any of these addresses are in the current transaction list, of so reject transaction or remove them from the list

                if(!isMining)
                {
                    //Now check the balances for the input addresses
                    List<Output> inputBalances = GetBalance(inputAddresses);

                    int total = inputBalances.Sum(x => x.amount);
                    int amount = trx.OutputList.First().amount;
                    if (total >= amount)
                    {
                        //set the change
                        trx.OutputList[1].amount = total - amount;
                        _currentTransactions.Add(trx);
                    }
                    else
                    {
                        Logger.Log(string.Format("Signature validation failed, transaction rejected id={0}", trx.id));
                        throw new Exception("Message does not match signature");
                    }
                } else //we're mining, so just add the new transaction
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
            
            string checkpointDir = Path.Combine(this.appDir,_cryptoProvider.ProviderName(), "checkpoints");
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

        public bool Rollback(string filename = "")
        {
            string checkpointDir = Path.Combine(this.appDir, _cryptoProvider.ProviderName(), "checkpoints");

            if (!Directory.Exists(checkpointDir))
                return false;

            string[] files = Directory.GetFiles(checkpointDir);

            if ((filename == "") && files.Count() > 0)
                filename = files.OrderBy(x => x).Reverse().First();

            if (string.IsNullOrEmpty(filename))
                return false;

            string filepath = Path.Combine(checkpointDir,_cryptoProvider.ProviderName(), filename);

            Logger.Log($"Rolling back, loading previous : {filepath}");

            using (StreamReader sr = File.OpenText(filepath))
            {
                string line = sr.ReadLine();
                string[] fields = line.Split('|');
                NodeId = fields[0];
                _currentTransactions = RollbackTransactions(int.Parse(fields[2]), sr);

                _chain = new List<Block>();
                line = sr.ReadLine();
                int blockCount = int.Parse(line);
                for (int i = 0; i < blockCount; i++)
                {
                    line = sr.ReadLine();
                    fields = line.Split('|');
                    Block b = new Block { Index = int.Parse(fields[0]), Timestamp = long.Parse(fields[1]), Proof = int.Parse(fields[2]), PreviousHash = fields[3], Transactions = new List<Transaction>() };
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
                Transaction tx = JsonConvert.DeserializeObject<Transaction>(line);
                txs.Add(tx);
            }
            return txs;
        }

        public void status()
        {
            string host = System.Configuration.ConfigurationManager.AppSettings["host"];
            string port = System.Configuration.ConfigurationManager.AppSettings["port"];
            
            System.Console.WriteLine(string.Format("NodeId={0}, Host={1}, Port={2}, CryptoProvider={3}",NodeId,host,port, _cryptoProvider.ProviderName()));
            foreach(Node n in _nodes)
            {
                System.Console.WriteLine(string.Format("Registered Node: {0}",n.Address));
            }
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

        public string Balance(string json)
        {
            List<string> al = JsonConvert.DeserializeObject<List<string>>(json);
            List<Output> result = GetBalance(al);
            return JsonConvert.SerializeObject(result);
        }


        public List<string> TransactionHistory(string address)
        {
            List<string> txList = new List<string>();

            int balance = 0;

            foreach (Block b in _chain)
            {
                TransactionHistory(b.Transactions, address, ref balance, txList);
            }

            TransactionHistory(_currentTransactions, address, ref balance, txList);

            return txList;
        }

        internal void TransactionHistory(List<Transaction> transactions, string address, ref int balance,List<string> txList)
        {
            List<Transaction> ordered = transactions.OrderBy(x => x.TimeStamp).ToList<Transaction>();
            foreach (Transaction t in ordered)
            {
                foreach (Input i in t.InputAddressList)
                {
                    if (i.address == address)
                    {
                        balance = 0;
                        txList.Add(string.Format("{0} Debit = 0", new System.DateTime(t.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss.fff")));
                    }
                }
                foreach (Output o in t.OutputList)
                {
                    if (o.address == address)
                    {
                        balance += o.amount;
                        txList.Add(string.Format("{0} Credit(+{1}) = {2}", new System.DateTime(t.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss.fff"), o.amount, balance));
                    }
                }
            }
        }



        public List<Output> GetBalance(List<string> addressList)
        {
            Dictionary<string, int> addressBalances = new Dictionary<string, int>();

            foreach (string addr in addressList)
            {
                addressBalances[addr] = 0;
            }

            //first process all the blocks
            foreach (Block b in _chain)
            {
                GetTransactionListBalances(b.Transactions, addressBalances);
            }

            //next process the current transactions
            GetTransactionListBalances(_currentTransactions, addressBalances);

            List<Output> balanceList = new List<Output>();
            foreach (string address in addressList)
            {
                balanceList.Add(new Output(address, addressBalances[address]));
            }

            return balanceList;
        }

        internal void GetTransactionListBalances(List<Transaction> txList, Dictionary<string, int> addressBalances)
        {
            List<Transaction> ordered = txList.OrderBy(x => x.TimeStamp).ToList<Transaction>();
            foreach (Transaction t in ordered)
            {
                foreach (Input i in t.InputAddressList)
                {
                    if(addressBalances.ContainsKey(i.address))
                        addressBalances[i.address] = 0;
                }
                foreach (Output o in t.OutputList)
                {
                    if (addressBalances.ContainsKey(o.address))
                        addressBalances[o.address] += o.amount;
                }
            }
        }

        public bool Validate()
        {
            return IsValidChain(_chain);
        }

        public string Resolve()
        {
            return Consensus();
        }

        /*
        public List<Output> GetBalance_old(List<string> addressList)
        {
            List<Output> addressBalances = new List<Output>();

            //Initialize the addressBalances list using the list provided
            foreach (string a in addressList)
            {   //build a list of Address Balances. A value of -1 means that it hasn't yet been fully balanced
                addressBalances.Add(new Output(a, -1));    
            }

            //Initialize the unBalanced list
            List<Output> unBalanced = addressBalances.Where(a => a.amount == -1).ToList();

            //now we iterate through the blocks, looking for Credits (Outputs) and Debits(Inputs)
            //If a Debit is found before any credits (ie. it was spent), 
            //then since all of the address balance is spent when used as an Input transaction,
            //we can set it's balance to zero and remove it from the unBalanced list as soon as it's.
            //However, an address could receive input from a number of transactions, so if a credit 
            //is found first (ie. the address appears in an Output List), then we Output list, 
            //we must continue to process until either it is found in an input (at which point, 
            //we can determine that the address has been used again), or we've reached the last block
            //in the chain (ie. the address has not been used as an Input)

            foreach(Block blk in _chain)
            {
                List<Output> allBlockOutputAddresses = new List<Output>();
                List<string> allBlockInputAddresses = new List<string>();
                //iterate all the transactions in the block to extract the Inputs (debits) and outputs (credits)
                foreach(Transaction t in blk.Transactions)
                {
                    allBlockInputAddresses.AddRange(t.InputAddressList.Select(x => x.address).ToList());
                    allBlockOutputAddresses.AddRange(t.OutputList);
                }

                foreach(Output ubo in unBalanced)
                {
                    //process credits first
                    IEnumerable<Output> matchingOutputs = allBlockOutputAddresses.Where(x => x.address == ubo.address);
                    foreach(Output mo in matchingOutputs)
                    {
                        if (ubo.amount < 0)
                            ubo.amount = mo.amount;
                        else
                            ubo.amount += mo.amount;
                    }


                    //process the debits
                    IEnumerable<string> matchingInputs = allBlockInputAddresses.Where(x => x == ubo.address);
                    //if we have any matching inputs, the address was used, so set it's balance to zero
                    if (matchingInputs.Count() > 0)
                        ubo.amount = 0;             
                }

                //if (unBalanced.Count() == 0)
                //    break;
            }

            //now zero any balances for addresses as input to pending transactions
            List<string> pendingInputs = new List<string>();
            List<Output> pendingOutputs = new List<Output>();

            foreach (Transaction t in _currentTransactions)
            {
                pendingInputs.AddRange(t.InputAddressList.Select(x => x.address));
                pendingOutputs.AddRange(t.OutputList);
            }
            foreach (Output b in addressBalances)
            {
                //
                IEnumerable<Output> matchingPendingOutputs = pendingOutputs.Where(x => x.address == b.address);
                foreach(Output mo in matchingPendingOutputs)
                {
                    if (b.amount < 0)
                        b.amount = mo.amount;
                    else
                        b.amount += mo.amount;
                }

                IEnumerable<string> matchingPendingInputs = pendingInputs.Where(x => x == b.address);
                if (matchingPendingInputs.Count() > 0)
                    b.amount = 0;

            }

            return addressBalances;
        }
        */
    }
}
