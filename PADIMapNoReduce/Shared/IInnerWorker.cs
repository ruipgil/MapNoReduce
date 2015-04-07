using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    public interface IInnerWorker
    {
        void work(int start, int end, string clientUrl);
    }
}
