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
		/// <summary>
		/// Submits a job to a worker.
		/// </summary>
		/// <param name="clientAddress">Client address.</param>
		/// <param name="inputSize">Input size.</param>
		/// <param name="fileSize">File size.</param>
		/// <param name="splits">Splits.</param>
		/// <param name="code">Code.</param>
		/// <param name="mapperName">Mapper name.</param>
		void submit(string clientAddress, int inputSize, long fileSize, int splits, byte[] code, string mapperName);
    }
}
