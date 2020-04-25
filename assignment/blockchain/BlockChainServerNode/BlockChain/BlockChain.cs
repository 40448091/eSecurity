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
using System.Threading;


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
    public class BlockChain
    {
        ICryptoProvider _cryptoProvider = null;                                         //crypto provider instance

        private List<Transaction> _currentTransactions = new List<Transaction>();       //Uncommitted transactions (will be committed to the next block when mined)
        private List<Block> _chain = new List<Block>();                                 //The Block Chain (ie. list of blocks)
        private List<Node> _nodes = new List<Node>();                                   //registered nodes
        private Block _lastBlock => _chain.Last();                                      //the last block in the chain
        private CryptoProvider.IPrivateKey _privateKey;
        private CryptoProvider.IPublicKey _publicKey;
        static BlockChainDemo.Miner _miner;


        //gets / sets the echo state. If on will echo server requests and responses to the console
        public bool echo
        {
            get { return Logger.echo; }
            set { Logger.echo = value; }
        }

        //id for the node
        public string NodeId { get; private set; }

        //returns the executing assembly directory path
        private string appDir {
            get {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }

        //BlockChain constructor
        public BlockChain()
        {
            Init();
        }

        public void Init()
        {
            //remove all blocks
            _chain = new List<Block>();
            _currentTransactions = new List<Transaction>();
            _nodes = new List<Node>();

            NodeId = System.Configuration.ConfigurationManager.AppSettings["NodeId"];

            //create the genesis block
            CreateNewBlock(proof: 100, previousHash: "1");

            //get the crytpo provider name from the 
            string cp = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];

            LoadCryptoProvider(cp);

            //create or load server node keys
            string filename = Path.Combine(appDir, cp + ".key");
            if (File.Exists(filename))
            {
                _cryptoProvider.ImportKeyPairFromFile(filename);
            }
            else
            {
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPairToFile(filename);
            }

            _publicKey = _cryptoProvider.GetPublicKey();
            _privateKey = _cryptoProvider.GetPrivateKey();

            Logger.Log(String.Format("BlockChain Initialized : NodeId={0}", NodeId));

            RegisterNodes();
        }

        //Loads the named crypto provider
        public bool LoadCryptoProvider(string name)
        {
            string cryptoProviderFilename = Path.Combine(appDir, name + "_CryptoProvider.dll");
            string keyFilename = Path.Combine(appDir, name + ".key");

            //load the specified crypto provider
            if (File.Exists(cryptoProviderFilename))
                _cryptoProvider = LoadCryptoProviderDLL(cryptoProviderFilename);
            else
                throw new Exception(string.Format("Crypto Provider not found : {0}", cryptoProviderFilename));

            //load the server node key pair (not currently used)
            if (File.Exists(keyFilename))
                _cryptoProvider.ImportKeyPairFromFile(keyFilename);
            else
            {
                _cryptoProvider.GenerateKeyPair();
                _cryptoProvider.ExportKeyPairToFile(keyFilename);
                Logger.Log("Key pair generated");
            }

            return true;
        }

        //Loads the crypto provider given the full path, using reflection
        private static CryptoProvider.ICryptoProvider LoadCryptoProviderDLL(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);

            //use reflection to load the Crypto Provider assembly
            System.Reflection.Assembly ptrAssembly = System.Reflection.Assembly.LoadFile(assembly);
            foreach (Type item in ptrAssembly.GetTypes())
            {
                if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(CryptoProvider.ICryptoProvider)))
                {
                    return (CryptoProvider.ICryptoProvider)Activator.CreateInstance(item);
                }
            }
            throw new Exception("Invalid DLL, Interface not found!");
        }

        //returns the number of blocks in the chain
        public int BlockCount()
        {
            return _chain.Count();
        }

        //returns the number of pending transactions on the node
        public int TransactionCount()
        {
            return _currentTransactions.Count();
        }


        //Registers the provided server node
        private void RegisterNode(string address)
        {
            _nodes.Add(new Node { Address = new Uri(address) });
            Logger.Log(String.Format("Node Registered : {0}", address));
        }

        //Deregisters the provided server node
        private void DeregisterNode(string address)
        {
            Node n = _nodes.Where(x => x.Address.ToString() == address).FirstOrDefault();
            _nodes.Remove(n);
        }

        //returns the true if the BlockChain held by the node is valid (false if not)
        private bool IsValidChain(List<Block> chain)
        {
            Block block = null;
            Block lastBlock = chain.First();
            int currentIndex = 1;

            //iterate through the block chain checking the proof of work and hashes are valid
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

        //rudimentary consusus algorithm
        private bool ResolveConflicts()
        {
            Logger.Log("Resolving conflicts");

            List<Block> newChain = null;                    //chain returned from registered node
            int maxLength = _chain.Count;

            List<Node> inactiveNodes = new List<Node>();    //if a registered node is unresponsive, it is added to this list

            //for each node in the registered nodes list...
            foreach (Node node in _nodes)
            {
                Logger.Log(string.Format("getting chain from : {0}", node.Address));

                //construct the url to the server node
                var url = new Uri(node.Address, "/chain");
                var request = (HttpWebRequest)WebRequest.Create(url);

                try
                {
                    //send a get chain request to the registered server node
                    var response = (HttpWebResponse)request.GetResponse();

                    //if we got an OK response, process the chain returned...
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var model = new
                        {
                            chain = new List<Block>(),
                            length = 0
                        };

                        //deserialize the response into a chain
                        string json = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        var data = JsonConvert.DeserializeAnonymousType(json, model);

                        //validate the chain returned from the server node
                        bool validChain = IsValidChain(data.chain);
                        if (!validChain)
                            Logger.Log("  Chain validation failed");

                        //if the chain is valid, and if it contains more blocks than this node, set newChain and maxLength
                        if ((data.chain.Count > _chain.Count) && validChain)
                        {
                            maxLength = data.chain.Count;
                            newChain = data.chain;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //if an exception was raised, then it's likely the registered server node did not respond
                    //so add it to the inactive nodes list
                    inactiveNodes.Add(node);
                }
            }

            //remove any inactive nodes from the registerd server list
            foreach (Node node in inactiveNodes)
            {
                _nodes.Remove(node);
                Logger.Log(string.Format("Deregistering unresponsive node: {0}", node.Address));
            }

            //if the newChain was set (ie. if the chain is valid, and if it contains more blocks than this node, set newChain and maxLength
            //replace the chain on this node, with the one returned from the registered node
            if (newChain != null)
            {
                _chain = newChain;
                return true;
            }

            return false;
        }


        //Creates a new block to add to the BlockChain
        private Block CreateNewBlock(int proof, string previousHash = null)
        {
            Logger.Log("Creating new block");

            //creates a block object based on the list of current uncommitted transactions
            //calculates hash
            var block = new Block
            {
                Index = _chain.Count,
                Timestamp = DateTime.UtcNow.Ticks,
                Transactions = _currentTransactions.ToList(),
                Proof = proof,
                PreviousHash = previousHash ?? GetHash(_chain.Last())
            };

            //add the block to the chain, and clear uncommitted transaction list
            _currentTransactions.Clear();
            _chain.Add(block);

            Logger.Log(string.Format("Block added Index={0}, previous hash={1}",block.Index,block.PreviousHash));

            //return the new block (ie. it's now the last one in the chain)
            return block;
        }

        //Create proof of work (simulates part of the mining process)
        private int CreateProofOfWork(int lastProof, string previousHash)
        {
            int proof = 0;
            while (!IsValidProof(lastProof, proof, previousHash))
                proof++;

            return proof;
        }

        //validates proof of work
        private bool IsValidProof(int lastProof, int proof, string previousHash)
        {
            string guess = $"{lastProof}{proof}{previousHash}";
            string result = GetSha256(guess);
            return result.StartsWith("0000");
        }

        //calculates a hash for the specified block
        private string GetHash(Block block)
        {
            string blockText = JsonConvert.SerializeObject(block);
            return GetSha256(blockText);
        }

        //calculates a shar256 hash from the specified string
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

        /************************************************************************
         * Mines a new block, sending the reward to the specified address
         ************************************************************************/
        public string Mine(string address)
        {
            Logger.Log(string.Format("Mining for new block for ID={0}", address));

            //Rudimentary consensus algorithm
            //Resolve conflicts this and registered server nodes (ie. update the chain)
            Consensus();

            //create proof of work
            int proof = CreateProofOfWork(_lastBlock.Proof, _lastBlock.PreviousHash);

            //create a transaction object
            Transaction tx = new Transaction();

            tx.id = Guid.NewGuid();                     //set a new ID for the transaction
            tx.Inputs = new List<Input>();    //a list to hold the inputs to the transaction
            tx.Outputs = new List<Output>();         //a list to hold the outputs from the transaction
            tx.Outputs.Add(new Output(address, 10));  //add reward to the mine transaction
            //sign the transaction with the server keys and add the public key so the signature can be checked
            tx.Signature = _cryptoProvider.SignMessage(tx.ToString(), _privateKey, _publicKey);
            tx.PublicKey = _publicKey.ToBase64String();


            //create a new transaction containing the reward. IsMining == true, so there are no input nodes to validate
            CreateTransaction(tx,true);

            //create a new block (commit all uncommitted transactions, including the reward transaction)
            Block block = CreateNewBlock(proof);

            //create a response object, which will be serialized and returned
            var response = new
            {
                Message = "New Block Forged",
                Index = block.Index,
                Transactions = block.Transactions.ToArray(),
                Proof = block.Proof,
                PreviousHash = block.PreviousHash
            };

            Logger.Log(string.Format("Mined new block Index={0}, Proof={1}, previous Hash={2}", block.Index, block.Proof, block.PreviousHash));

            //serialize and return the response
            return JsonConvert.SerializeObject(response);
        }

        //return the full BlockChain
        internal string GetFullChain()
        {
            var response = new
            {
                chain = _chain.ToArray(),
                length = _chain.Count
            };

            //serialize the BlockChain as a json object
            return JsonConvert.SerializeObject(response);
        }

        //Get a comma separated list of nodes from the app.config and register them
        internal void RegisterNodes()
        {
            string nodesList = System.Configuration.ConfigurationManager.AppSettings["registerNodes"];
            string[] nodes = nodesList.Replace(" ", "").Split(',');
            RegisterNodes(nodes);
        }

        //register the nodes specified in the string array provided
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

        //rudimentary consensus algorithm
        internal string Consensus(bool fullChain = true)
        {
            bool replaced = ResolveConflicts();
            string result = "";

            string message = replaced ? "was replaced" : "is authoritive";

            if (fullChain)
            {
                var response = new
                {
                    Message = $"Our chain {message}",
                    Chain = _chain
                };
                
                result = JsonConvert.SerializeObject(response);
            } else
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"Our Chain {message}");
                foreach(Block b in _chain)
                {
                    builder.AppendLine(b.ToString());
                }
                result = builder.ToString();
            }

            return result;
        }


        //creates a new transaction and adds it to the uncommitted transaction list
        //if isMining is true then there are no input nodes to verify
        internal int CreateTransaction(Transaction trx, bool isMining = false)
        {
            Logger.Log(string.Format("Adding Transaction id={0} ", trx.id));

            //attempt to validate the transaction input signatures using address + signature + public key from the transaction, and the cryptoProvider
            if (trx.HasValidInputSignatures(trx.id.ToString(),_cryptoProvider))
            {
                //if the input transactions are valid, add them to the input list
                List<string> inputAddresses = trx.Inputs.Select(x => x.address).ToList();

                //if not isMining, check the balance for input addresses >= Ouptut amount
                if(!isMining)
                {
                    //Iterate the Blocks in the chain to determine the value for the input addresses
                    List<Output> inputBalances = GetBalance(inputAddresses);

                    int total = inputBalances.Sum(x => x.amount);
                    int amount = trx.Outputs.First().amount;

                    //if the total amount for the input nodes >= Ouptut amount
                    if (total >= amount)
                    {
                        //set the amount for the change address
                        trx.Outputs[1].amount = total - amount;
                        _currentTransactions.Add(trx);
                    }
                    else
                    {
                        string msg = $"Insufficient funds. Input total={total}, Output required={amount}";
                        Logger.Log(msg);
                        throw new Exception(msg);
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

        //Saves the current BlockChain and uncommitted transaction state to a checkpoint file
        //{crypto provider name}\checkpoints\{checkpoint files}
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

        //outputs a list of transactions to the checkpoint file
        private void CheckPointTransactions(List<Transaction> txs, StreamWriter writer)
        {
            string line;
            foreach (Transaction tx in txs)
            {
                line = JsonConvert.SerializeObject(tx);
                writer.WriteLine(line);
            }
        }

        //restores the BlockChain and uncommitted transaction state from the last CheckPoint file
        //{crypto provider name}\checkpoints\{checkpoint files}
        public string Rollback(string filename = "")
        {
            string checkpointDir = Path.Combine(this.appDir, _cryptoProvider.ProviderName(), "checkpoints");

            if (!Directory.Exists(checkpointDir))
                return $"Error: Invalid Checkpoint directory: {checkpointDir}";

            string[] files = Directory.GetFiles(checkpointDir);

            if ((filename == "") && files.Count() > 0)
                filename = files.OrderBy(x => x).Reverse().First();

            if (string.IsNullOrEmpty(filename))
                return $"Error: Checkpoint File Not Found: {filename}";

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

            Logger.Log($"Rolling complete: {filename}");

            return $"Rollback Complete: {filename}";
        }

        //loads a transaction from the checkpoint file
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

        //displays current server node state
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
            System.Console.WriteLine(string.Format("Miner is {0}", _miner != null ? "Running" : "Stopped"));
        }

        //lists current uncommitted transactions
        public void list_currentTransactions()
        {
            foreach(Transaction tx in _currentTransactions)
            {
                System.Console.WriteLine(tx.ToString());
            }
        }

        public List<Transaction> PendingTransactions()
        {
            return _currentTransactions;
        }

        //lists blocks in the chain
        public void list_blocks()
        {
            foreach (Block b in _chain)
            {
                System.Console.WriteLine(b.ToString());
            }
        }

        public void ExportPublicKey()
        {
            System.Console.WriteLine(string.Format("Public Key={0}", _cryptoProvider.ExportPublicKey()));
        }

        //Gets the balance for a list of addresses by iterating over the BlockChain
        public string Balance(string json)
        {
            List<string> al = JsonConvert.DeserializeObject<List<string>>(json);
            List<Output> result = GetBalance(al);
            return JsonConvert.SerializeObject(result);
        }

        //returns the transaction history for the specified address by iterating over the blocks in the chain
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
                foreach (Input i in t.Inputs)
                {
                    if (i.address == address)
                    {
                        balance = 0;
                        txList.Add(string.Format("{0} Debit = 0", new System.DateTime(t.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss.fff")));
                    }
                }
                foreach (Output o in t.Outputs)
                {
                    if (o.address == address)
                    {
                        balance += o.amount;
                        txList.Add(string.Format("{0} Credit(+{1}) = {2}", new System.DateTime(t.TimeStamp).ToString("yyyy-MM-dd HH:mm:ss.fff"), o.amount, balance));
                    }
                }
            }
        }


        //returns the balaces for the specified address
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

        //process credits and debits for each transaction in the list,
        //for each of the addresses addressBalances specified
        internal void GetTransactionListBalances(List<Transaction> txList, Dictionary<string, int> addressBalances)
        {
            List<Transaction> ordered = txList.OrderBy(x => x.TimeStamp).ToList<Transaction>();
            foreach (Transaction t in ordered)
            {
                foreach (Input i in t.Inputs)
                {
                    if(addressBalances.ContainsKey(i.address))
                        addressBalances[i.address] = 0;
                }
                foreach (Output o in t.Outputs)
                {
                    if (addressBalances.ContainsKey(o.address))
                        addressBalances[o.address] += o.amount;
                }
            }
        }

        //process console request to validate
        public bool Validate()
        {
            return IsValidChain(_chain);
        }

        //process console request to Resolve / run consensus
        public string Resolve(bool fullChain=true)
        {
            return Consensus(fullChain);
        }

        public void Miner_Start(string address)
        {
            _miner = new BlockChainDemo.Miner(this, address);
            _miner.Start();
        }

        public void Miner_Stop()
        {
            if (_miner != null)
            {
                _miner.Stop();
                while (_miner.IsRunning())
                {
                    Thread.Sleep(100);
                }
                _miner = null;
            }
        }

    }
}
