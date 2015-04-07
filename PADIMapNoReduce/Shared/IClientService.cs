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
        List<string> getKeys(int start, int end);
        Dictionary<string, string> getValues(int start, int end);
        // asks the client for the values of keys
        Dictionary<string, string> getValues(List<string> keys);
        // sets the processing result of keys
        void setResult(Dictionary<string, string> keyValProcessed);
        // all key/values have been processed
        void jobHasCompleted();
    }
}
