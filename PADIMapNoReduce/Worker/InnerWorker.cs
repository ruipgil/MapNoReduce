using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    class InnerWorker : MarshalByRefObject, IInnerWorker
    {
        public void work(int start, int end, string clientUrl)
        {
            // open connection with client, through clientUrl
            // ask for data from start to end
            // process data with mapper
            // signal master that this has ended
        }
    }
}
