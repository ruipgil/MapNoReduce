using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    // This is the interface for the client to communicate with the worker,
    // mainly to request processing to a worker
    public interface IWorkerService
    {
        void submit(int inputSize, int splits);
    }
}
