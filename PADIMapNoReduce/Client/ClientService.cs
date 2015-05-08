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

namespace PADIMapNoReduce
{
    public class ClientService : MarshalByRefObject, IClientService
    {

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
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "C", typeof(ClientService));
			ownAddress = @"tcp://"+host+":" + port + "/C";
        }

		public ClientService(int port) : this(port, "localhost")
		{
		}

        public void init(string workerEntryUrl)
        {
            Console.Out.WriteLine("#init "+workerEntryUrl);
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

            Console.Out.WriteLine("#submiting");

            //fileContent = new List<string>(File.ReadAllLines(inputFile));
            //lines = fileContent.Count();
			int lineCount = 0;
			using (var reader = File.OpenText(inputFile))
			{
				while (reader.ReadLine() != null)
				{
					lineCount++;
				}
			}
			lines = lineCount;

            Console.Out.WriteLine("\tlines:"+lines);
			knownWorker.submit(ownAddress, lines, info.Length, splits, code, mapperName);

			doneCallback = callback;
        }

        public List<string> get(int start, int end)
        {
            Console.Out.WriteLine("#get "+start+" "+end);

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
            Console.Out.WriteLine("#set "+split+" "+results.Count());
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
