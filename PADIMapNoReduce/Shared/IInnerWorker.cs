using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    public interface IInnerWorker
    {
        /// <summary>
        /// Asks client for the values. Process them with the mapper and maps them with the mapper.
        /// Sends the mapped values to the client.
        /// </summary>
        /// <param name="start">Start index (line)</param>
        /// <param name="end">End index (line), exclusive</param>
        /// <param name="split">The identifier of this split</param>
        /// <param name="clientUrl">Client url</param>
        void work(int start, int end, int split, string clientUrl);
    }
}
