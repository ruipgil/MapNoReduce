using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PADIMapNoReduce
{
    public class ThreadWorker
    {
        private int _item1;
        private int _item2;
        private int _splitNumber;
        private string _url;
        private IWorkingWorkerService _worker;
        private ManualResetEvent _doneEvent;
        
        public ThreadWorker(IWorkingWorkerService worker, int item1, int item2, int splitNumber, string url, ManualResetEvent doneEvent)
        {
            _worker = worker;
            _item1 = item1;
            _item2 = item2;
            _splitNumber = splitNumber;
            _url = url;
            _doneEvent = doneEvent;
        }

        public void ThreadPoolCallback(Object threadContext)
        {
            int threadIndex = (int)threadContext;
            Console.WriteLine("thread {0} started...", threadIndex);
            _worker.work(_item1, _item2, _splitNumber, _url);
            Console.WriteLine("thread {0} result calculated...", threadIndex);
            _doneEvent.Set();
        }

    }
}
