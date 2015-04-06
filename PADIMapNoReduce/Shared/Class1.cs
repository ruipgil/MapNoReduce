using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapNoReduce
{
    public interface IMapNoReduce
    {
        public void process(int splits, byte[] dll);
    }

    public interface IWorkerCommunication
    {
        public void process(int start, int end, byte[] dll);
    }

}
