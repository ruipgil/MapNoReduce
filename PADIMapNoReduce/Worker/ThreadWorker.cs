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
        private byte[] _code;
        private string _mapperName;
        private IWorkingWorkerService _worker;
        private ManualResetEvent _doneEvent;

        
        public ThreadWorker(IWorkingWorkerService worker, int item1, int item2, int splitNumber, string url, ManualResetEvent doneEvent, byte[] code, string mapperName)
        {
            _worker = worker;
            _item1 = item1;
            _item2 = item2;
            _splitNumber = splitNumber;
            _url = url;
            _doneEvent = doneEvent;
            _code = code;
            _mapperName = mapperName;
        }

        //add if freeze dont do thread
        public void ThreadPoolCallback(Object threadContext)
        {
            
            int threadIndex = (int)threadContext;
            if (!_worker.getFreeze())
            {
                Console.WriteLine("thread {0} started...", threadIndex);
                _worker.work(_item1, _item2, _splitNumber, _url, _code, _mapperName);
                Console.WriteLine("thread {0} result calculated...", threadIndex);
                _doneEvent.Set();
            }
            else
            {
                Console.WriteLine("thread {0}: It's cold in here, i'm freezing!!! ",threadIndex);
            }
        }
    }
}
