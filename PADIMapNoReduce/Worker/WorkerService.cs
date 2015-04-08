using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Services;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    public abstract class WorkerService : MarshalByRefObject, IWorkerService
    {
        public void submit(int inputSize, int splits)
        {
            Console.Out.WriteLine("# submit "+inputSize+" "+splits);
            List<Tuple<int, int>> s = split(inputSize, splits);
            List<IWorkingWorkerService> workers = getAvaiableWorkers();
            string clientUrl = "tcp://localhost:10001/C";

            distributeWorkToWorkers(s, workers, clientUrl);
        }

        // returns a list with the values split into chunks
        // each chunk is a tuple, the first item is INCUSIVE the second item is NOT INCLUSIVE
        // this means that a tuple of 6,9 represents the indexes from 6 to 8
        private List<Tuple<int,int>> split(int inputSize, int splits)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            int splitSize = inputSize / splits;
            for (int i=1; i<=splits; i++)
            {
                result.Add(new Tuple<int,int>((i-1)*splitSize, i*splitSize));
            }

            int rest = inputSize - splitSize * splits;
            if (rest != 0)
            {
                int last = splits * splitSize;
                result.Add(new Tuple<int, int>(last, last+rest));
            }
            return result;
        }

        private List<IWorkingWorkerService> getAvaiableWorkers()
        {
            return new List<IWorkingWorkerService>() { (IWorkingWorkerService)this };
        }

        // missing mapper
        private void distributeWorkToWorkers(List<Tuple<int, int>> splits, List<IWorkingWorkerService> workers, string clientUrl)
        {
            // key is the index of 'List<int> splits' and value is the worker responsible for the job
            Dictionary<int, IWorkingWorkerService> splitsToWorker = new Dictionary<int, IWorkingWorkerService>();
            int workersCount = workers.Count();
            for (int i = 0; i < splits.Count(); i++)
            {
                IWorkingWorkerService worker = workers[i % workersCount];
                splitsToWorker.Add(i, worker);
                Tuple<int, int> split = splits[i];
                worker.work(split.Item1, split.Item2, i, clientUrl); // missing mapper
            }
        }
    }
}
