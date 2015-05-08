using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    // Interface for the worker to communicate with the client
    public interface IClientService
    {
        /// <summary>
        /// Gets a list of values from key start to key end-1
        /// </summary>
        /// <param name="start">First key index</param>
        /// <param name="end">Last key index, minus one</param>
        /// <returns>A list where the first element corresponds to the key with index==start</returns>
        List<string> get(int start, int end);
        // sets the processing result of keys
        /// <summary>
        /// Sets the result of the mapping of a split
        /// </summary>
        /// <param name="split">The split index</param>
        /// <param name="results">List with results of the mapping</param>
        void set(int split, List<IList<KeyValuePair<string, string>>> results);

		void done ();


        // TODO ADDED THIS, CAN I?
        //void submit(string inputFile, string outputFolder, int splits, byte[] code, string mapperName);
    }
}
