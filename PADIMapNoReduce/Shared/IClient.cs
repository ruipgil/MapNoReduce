using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapNoReduce
{
    // Interface for the worker to communicate with the client
    public interface IClient
    {
        public List<string> getKeys(int start, int end);
        public Dictionary<string, string> getValues(int start, int end);
        // asks the client for the values of keys
        public Dictionary<string, string> getValues(List<string> keys);
        // sets the processing result of keys
        public void setResult(int split, Dictionary<string, string> map);
        // all key/values have been processed
        public void jobHasCompleted();
    }
}
