using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    /// <summary>
    /// Worker interface visible to the client
    /// </summary>
    public interface IPuppetMasterService
    {
        void createWorker(int port, string otherWorkers);
        void createWorker(string url, string otherWorkers);

    }
}