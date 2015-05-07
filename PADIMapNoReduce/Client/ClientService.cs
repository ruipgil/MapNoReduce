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
        string outputFolder;

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

        public void submit(string inputFile, string outputFolder, int splits, byte[] code, string mapperName) // incomplete
        {
            if (!hasKnownWorker)
            {
                knownWorker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), knownWorkerUrl);
                hasKnownWorker = true;
            }
            this.outputFolder = outputFolder;
			FileInfo info = new FileInfo (inputFile);
			float fileSize = info.Length/(1000f*1000f);

            Console.Out.WriteLine("#submiting");
            fileContent = new List<string>(File.ReadAllLines(inputFile));
            lines = fileContent.Count();
            Console.Out.WriteLine("\tlines:"+lines);
			knownWorker.submit(ownAddress, lines, info.Length, splits, code, mapperName);
        }

        public List<string> get(int start, int end)
        {
            Console.Out.WriteLine("#get "+start+" "+end);
            return fileContent.GetRange(start, end - start);
        }

        public void set(int split, List<IList<KeyValuePair<string, string>>> results)
        {
            string outputFile = outputFolder + "/" + split + ".out";
            Console.Out.WriteLine("#set "+split+" "+results.Count());
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile))
            {
                foreach (List<KeyValuePair<string, string>> result in results)
                {
                    string entryResult = "";
                    foreach (KeyValuePair<string, string> entry in result)
                    {
                        entryResult += entry.Key + ":" + entry.Value + ",";
                    }
                    file.WriteLine(entryResult);
                }
            }
        }
    }
}
