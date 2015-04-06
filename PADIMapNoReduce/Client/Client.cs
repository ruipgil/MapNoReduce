using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapNoReduce
{
    public class Client : MarshalByRefObject, IClient
    {
        Dictionary<string, string> originalKeyVal = new Dictionary<string, string>();
        Dictionary<string, string> processedKeyVal = new Dictionary<string, string>();

        public List<string> getKeys(int start, int end)
        {
            // TODO: exception if end > originalKeyVal.Keys.length
            return new List<string>(originalKeyVal.Keys).GetRange(start, end);
        }

        public Dictionary<string, string> getValues(int start, int end)
        {
            return new Dictionary<string, string>(originalKeyVal);
        }

        public string getValue(string key)
        {
            return originalKeyVal[key];
        }

        // asks the client for the values of keys
        public Dictionary<string, string> getValues(List<string> keys)
        {
            return originalKeyVal.Where(x => keys.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }

        // sets the processing result of keys
        public void setResult(Dictionary<string, string> keyValProcessed)
        {
            foreach (KeyValuePair<string, string> entry in keyValProcessed)
            {
                processedKeyVal.Add(entry.Key, entry.Value);
            }
        }

        // unlock lock
        public void jobHasCompleted()
        {
            return;
        }
    }
}
