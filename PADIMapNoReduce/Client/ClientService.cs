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
using System.Threading;

namespace PADIMapNoReduce
{
    public class ClientService : MarshalByRefObject, IClientService
    {
        IDictionary RemoteChannelProperties = new Hashtable();

        IWorkerService knownWorker;
        string knownWorkerUrl;
        bool hasKnownWorker = false;
		string ownAddress;

        int lines;
		Dictionary<int, int> linesOffsets = new Dictionary<int, int>();
		string inputFile;
        string outputFolder;

		ManualResetEvent reading = new ManualResetEvent (true);
		FileStream fileStream;

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
			reading.Reset ();

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
				string line;
				int offset = 0;
				while ((line = reader.ReadLine ()) != null)
				{
					//Console.WriteLine ("LINE["+line+"]"+lineCount+" "+offset+" "+line.Length);
					linesOffsets.Add (lineCount, offset);
					offset += line.Length+1;
					lineCount++;
				}
				linesOffsets.Add (lineCount, offset);
			}
			lines = lineCount;

			reading.Set ();

			Console.Out.WriteLine("#submit {0} {1} {2}Bytes {3} {4} {5}", ownAddress, lines, info.Length, splits, "%code%", mapperName);
			knownWorker.submit(ownAddress, lines, info.Length, splits, code, mapperName);

			doneCallback = callback;
        }

        public List<string> get(int start, int end)
        {
			reading.WaitOne ();

            Console.Out.WriteLine("#> "+start+"-"+end);

			List<string> result = new List<string> ();

			int length = linesOffsets[end]-linesOffsets[start]-1;
			using (FileStream fsSource = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				try {
					var binReader = new BinaryReader(fsSource);
					fsSource.Position = linesOffsets[start];

					char[] rr = binReader.ReadChars(length);

					string[] r = new string(rr).Split('\n');
					result = new List<string> (r);

				} catch(Exception e) {
					Console.WriteLine (e);
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
                        entryResult += entry.Key + ":" + entry.Value;
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
