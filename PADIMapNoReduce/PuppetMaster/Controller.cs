using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace PADIMapNoReduce
{
    public class Controller

    {
        List<KeyValuePair<int, string>> workerIds = new List<KeyValuePair<int, string>>();
        private bool isRunningScript = false;
        private int step = 0;
        System.IO.StreamReader fileStep; 

        public void run()
        {
            Console.Out.WriteLine("Console For PuppetMaster:");

        }

        public void stepScript(String location)
        {
            Console.Out.WriteLine("Step to line {0} of Script...", step);
            if (!isRunningScript)
            {
                fileStep = new System.IO.StreamReader(location);
                step = 0;
                isRunningScript = true;
            }
            String line = fileStep.ReadLine();
            if (line != null)
            {
                step++;
                //Console.WriteLine(line);
                executeCommand(line);
            }
            else
            {
                fileStep.Close();
                step = 0;
                isRunningScript = false;
            }
        }

        public void runScript(String location)
        {
            Console.Out.WriteLine("Running Script...");
            //String line;
           // System.IO.StreamReader file = new System.IO.StreamReader(location);

            do
            {
                stepScript(location);
            } while (isRunningScript);

        }

        public void stopScript()
        {
            if (isRunningScript)
            {
                fileStep.Close();
                step = 0;
                isRunningScript = false;
            }
        }

        public void runCommand(String command)
        {
            executeCommand(command);
        }

        public void executeCommand(String command)
        {
            string[] splits = command.Split(' ');
            if (splits[0].StartsWith("%")) { }
            if (splits[0].Equals("WORKER"))
            {
                Console.Out.WriteLine("Creating Worker...");
                var id = splits[1];
                var workerUrl = splits[3];
                var knows = splits.Length>4?splits[4]:"";
                var puppetMaster = splits[2];
                Console.WriteLine(id+" "+workerUrl+" "+knows+" "+puppetMaster);

                KeyValuePair<int, string> kvpair = new KeyValuePair<int, string>(int.Parse(id), workerUrl);
                workerIds.Add(kvpair);

                IPuppetMasterService pm = (IPuppetMasterService)Activator.GetObject(typeof(IPuppetMasterService), puppetMaster);
                pm.createWorker(workerUrl, knows+" -a "+string.Join(":", workerUrl.Split(':'), 0, 2));

                Thread.Sleep(1000);

            }
            if (splits[0].Equals("SUBMIT"))
            {
                Console.Out.WriteLine("Submitting job...");
                string workerEntryUrl = splits[1];
                string inputFile = splits[2];
                string outputFolder = splits[3];
                int nrSplits = int.Parse(splits[4]);
                string mapName = splits[5];
                string mapPath = splits[6];
                byte[] code = System.IO.File.ReadAllBytes(mapPath);

                var client = new ClientService(10001);
				//var next = false;
                client.init(workerEntryUrl);
                client.submit(inputFile, outputFolder, nrSplits, code, mapName, () => {
					//next = true;
                    Console.WriteLine("Work of "+inputFile+" completed!");
                });
				/*var t = new Thread (() => {
					while(!next) {}
				});
				t.Join ();*/

            }
            if (splits[0].Equals("WAIT"))
            {
                Console.Out.WriteLine("Sleeping for {0} seconds", splits[1]);
                Thread.Sleep(int.Parse(splits[1]) * 1000);
            }
            if (splits[0].Equals("STATUS"))
            {
                getStatus();
            }
            if (splits[0].Equals("SLOWW"))
            {
                slowWorker(int.Parse(splits[1]), int.Parse(splits[2]));
            }
            if (splits[0].Equals("FREEZEW"))
            {
                freezeWorkerW(int.Parse(splits[1]));
            }
            if (splits[0].Equals("UNFREEZEW"))
            {
                unFreezeWorkerW(int.Parse(splits[1]));
            }
            if (splits[0].Equals("FREEZEC"))
            {
                freezeWorkerC(int.Parse(splits[1]));
            }
            if (splits[0].Equals("UNFREEZEC"))
            {
                freezeWorkerC(int.Parse(splits[1]));
            }
        }

        public int workerPortNr = 30001;

        public void createWorker()
        {
            String s = Convert.ToString(workerPortNr);
            Console.Out.WriteLine("Creating Worker @ port {0}", workerPortNr);
            Process.Start("Worker.exe", s);
            workerPortNr++;
        }

        public void createWorker(int port, string otherWorkers)
        {
            String s = Convert.ToString(port) + " " + otherWorkers;
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Process.Start("Worker.exe", s);
            
        }
        
        public void createWorker(string url, string args)
        {
            string port = url.Split(':')[2].Split('/')[0];
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Console.WriteLine(port + " " + args);
            Process.Start("Worker.exe", port + " "+args);

        }

        public void getStatus()
        {
            Console.Out.WriteLine("Obtaining the workers and job trackers status");
            foreach (KeyValuePair<int, string> pair in workerIds)
            {
				IWService worker = (IWService)Activator.GetObject(typeof(IWService), pair.Value);
                worker.getStatus();
            }
        }

		public IWService getWorker(int id)
        {
            string entryUrl = null;
            //get entry url for the worker id
            foreach (KeyValuePair<int, string> pair in workerIds)
            {
                if (id == pair.Key)
                {
                    entryUrl = pair.Value;
                }
            }

			return (IWService)Activator.GetObject(typeof(IWService), entryUrl);
        }

        public void slowWorker(int id, int seconds)
        {
            Console.Out.WriteLine("Injecting worker {0} with a delay ...", id);
			IWService worker = getWorker(id);
            worker.slowWorker(seconds);
        }

        public void freezeWorkerW(int id)
        {
            Console.Out.WriteLine("Freezing worker {0} ...", id);
			IWService worker = getWorker(id);
            worker.freezeWorker();
        }

        public void unFreezeWorkerW(int id)
        {
            Console.Out.WriteLine("Unfreezing worker {0} ...", id);
			IWService worker = getWorker(id);
            worker.unfreezeWorker();
        }

        public void freezeWorkerC(int id)
        {
            Console.Out.WriteLine("Freezing worker {0} ...", id);
			IWService worker = getWorker(id);
			worker.freezeCoordinator ();
        }

        public void unFreezeWorkerC(int id)
        {
            Console.Out.WriteLine("Unfreezing worker {0} ...", id);
			IWService worker = getWorker(id);
            worker.unfreezeCoordinator ();
        }

    }
}
