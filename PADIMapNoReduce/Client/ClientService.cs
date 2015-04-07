using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;

namespace PADIMapNoReduce
{
    public class Client : MarshalByRefObject, IClientService
    {
        Dictionary<string, string> originalKeyVal = new Dictionary<string, string>();
        Dictionary<string, string> processedKeyVal = new Dictionary<string, string>();

        TcpChannel channel;
        IWorkerService knownWorker;


        public Client()
        {
        }

        public void init(string entryUrl)
        {
            channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
            knownWorker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), entryUrl);
        }

        public void submit(string inputFile, int splits) // incomplete
        {
            int lines = File.ReadLines(@"C:\file.txt").Count();
            knownWorker.submit(lines, splits);
        }


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
