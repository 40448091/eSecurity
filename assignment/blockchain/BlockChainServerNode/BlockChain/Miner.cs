using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Miner
    {

        Thread _thread = null;
        bool _stopping = false;
        bool _running = false;
        int _sleep = 1000;
        string _address = "";
        BlockChain.BlockChain _chain;

        public Miner(BlockChain.BlockChain chain, string address, int sleep = 10000)
        {
            _chain = chain;
            _sleep = sleep;
            _address = address;
            _thread = null;
            _running = false;
            _stopping = false;
        }

        public void Start()
        {
            _running = false;
            _stopping = false;
            _thread = new Thread(this.DoWork);
            _thread.Start();
        }

        public void Stop()
        {
            _stopping = true;
        }

        public bool IsRunning()
        {
            return _running;
        }

        public bool IsStopping()
        {
            return _stopping;
        }

        private void DoWork()
        {
            System.Console.WriteLine("Miner started");
            _running = true;
            int counter = 0;
            while (!_stopping)
            {
                counter++;
                if (counter >= _sleep)
                {
                    _chain.Mine(_address);
                    counter = 0;
                    System.Console.WriteLine($"Blocks={_chain.BlockCount()}, Transactions={_chain.TransactionCount()}");
                }
                Thread.Sleep(1000);
            }
            _running = false;

            System.Console.WriteLine("Miner stopped");
        }
    }
}
