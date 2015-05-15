using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.Collections;

namespace PADIMapNoReduce
{
    public class ClientService : MarshalByRefObject, IClientService
    {
        IDictionary RemoteChannelProperties = new Hashtable();

        IWorkerService knownWorker;
        string knownWorkerUrl;
        bool hasKnownWorker = false;
		string ownAddress;

		List<string> fileContent;
        int lines;
		string inputFile;
        string outputFolder;

		Action doneCallback;

        public ClientService(int port, string host)
        {
            ownAddress = @"tcp://" + host + ":" + port + "/C";
            RemoteChannelProperties["port"] = port+"";
            RemoteChannelProperties["name"] = "client"+port;
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "C", typeof(ClientService));
        }

		public ClientService(int port) : this(port, "localhost")
		{
		}

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void init(string workerEntryUrl)
        {
            knownWorkerUrl = workerEntryUrl;
        }

		public void submit(string inputFile, string outputFolder, int splits, byte[] code, string mapperName, Action callback) // incomplete
        {
            if (!hasKnownWorker)
            {
                knownWorker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), knownWorkerUrl);
                hasKnownWorker = true;
            }
            this.outputFolder = outputFolder;
			this.inputFile = inputFile;
			var info = new FileInfo (inputFile);

			int lineCount = 0;
			using (var reader = File.OpenText(inputFile))
			{
				while (reader.ReadLine() != null)
				{
					lineCount++;
				}
			}
			lines = lineCount;

			Console.Out.WriteLine("#submit {0} {1} {2}Bytes {3} {4} {5}", ownAddress, lines, info.Length, splits, "%code%", mapperName);
			knownWorker.submit(ownAddress, lines, info.Length, splits, code, mapperName);

			doneCallback = callback;
        }

        public List<string> get(int start, int end)
        {
            Console.Out.WriteLine("#> "+start+"-"+end);

			List<string> result = new List<string> ();
			int lineCount = 0;
			string line;
			using (var reader = File.OpenText(inputFile))
			{
				while ((line=reader.ReadLine()) != null)
				{
					if (lineCount >= start) {
						result.Add (line);
					}
					lineCount++;
					if (end <= lineCount) {
						break;
					}
				}
			}
			return result;
        }

        public void set(int split, List<IList<KeyValuePair<string, string>>> results)
        {
            string outputFile = outputFolder + "/" + split + ".out";
			Console.Out.WriteLine("#< S"+split+" (length "+results.Count()+")");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile))
            {
                foreach (List<KeyValuePair<string, string>> result in results)
                {
					if (result.Count == 0) {
						continue;
					}

                    string entryResult = "";
                    foreach (KeyValuePair<string, string> entry in result)
                    {
                        entryResult += entry.Key + ":" + entry.Value + ",";
                    }
                    file.WriteLine(entryResult);
                }
            }
        }

		public void done() {
			doneCallback ();
		}
    }
}
