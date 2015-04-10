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
    public interface IWorkerService
    {
        void submit(int inputSize, int splits, byte[] code, string mapperName);
    }
}
