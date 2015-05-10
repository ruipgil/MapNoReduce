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
        private int workerPortNr = 30001;
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
            string[] splits = command.Split(new string[] { " " }, 2, StringSplitOptions.None);
            if (splits[0].StartsWith("%")) { }
            if (splits[0].Equals("WORKER"))
            {
                string[] tempSplits = splits[1].Split(new string[] { " " }, 4, StringSplitOptions.None);
                Console.Out.WriteLine("Creating Worker...");

                //SAVE WORKER ID
                KeyValuePair<int, string> kvpair = new KeyValuePair<int, string>(int.Parse(tempSplits[0]), tempSplits[2]);
                workerIds.Add(kvpair);

                String pmEntryUrl = tempSplits[1];
                IPuppetMasterService pm = (IPuppetMasterService)Activator.GetObject(typeof(IPuppetMasterService), pmEntryUrl);
                var wa = tempSplits[2].Split(':')[1].Split('/')[0];
				pm.createWorker(tempSplits[2], (tempSplits.Length>3?tempSplits[3]:"") + " -a " + wa);

            }
            if (splits[0].Equals("SUBMIT"))
            {
                Console.Out.WriteLine("Submitting job...");
                string[] tempSplits = splits[1].Split(new string[] { " " }, 6, StringSplitOptions.None);
                string workerEntryUrl = tempSplits[0];
                string inputFile = tempSplits[1];
                string outputFolder = tempSplits[2];
                int nrSplits = int.Parse(tempSplits[3]);
                string mapName = tempSplits[4];
                string mapPath = tempSplits[5];
                byte[] code = System.IO.File.ReadAllBytes(mapPath);

                var client = new ClientService(10001);
				var next = false;
                client.init(workerEntryUrl);
                client.submit(inputFile, outputFolder, nrSplits, code, mapName, () => {
					next = true;
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
            //TODO IMPROVE THIS : Only works for localhost (i think)
            String[] splits = url.Split(new string[] { ":" }, 3, StringSplitOptions.None);
            String[] splits2 = splits[2].Split(new string[] { "/" }, 2, StringSplitOptions.None);
            int port = int.Parse(splits2[0]);
            String s = splits2[0] + " " + args;
            /*string[] args = new string[1];
            args[0] = s;*/
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Process.Start("Worker.exe", s);

        }

        public void getStatus()
        {
            Console.Out.WriteLine("Obtaining the workers and job trackers status");
            foreach (KeyValuePair<int, string> pair in workerIds)
            {
                IWorkerService worker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), pair.Value);
                worker.getStatus();
            }
        }

        public IWorkerService getWorker(int id)
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

            return (IWorkerService)Activator.GetObject(typeof(IWorkerService), entryUrl);
        }

        public void slowWorker(int id, int seconds)
        {
            Console.Out.WriteLine("Injecting worker {0} with a delay ...", id);
            IWorkerService worker = getWorker(id);
            worker.slowWorker(seconds);
        }

        public void freezeWorkerW(int id)
        {
            Console.Out.WriteLine("Freezing worker {0} ...", id);
            IWorkerService worker = getWorker(id);
            worker.freezeWorker();
        }

        public void unFreezeWorkerW(int id)
        {
            Console.Out.WriteLine("Unfreezing worker {0} ...", id);
            IWorkerService worker = getWorker(id);
            worker.unFreezeWorker();
        }

        public void freezeWorkerC(int id)
        {
            Console.Out.WriteLine("Freezing worker {0} ...", id);
            IWorkerService worker = getWorker(id);
            /*if (worker.isJobTracker())
            {
                worker.freezeWorker();
            }*/
        }

        public void unFreezeWorkerC(int id)
        {
            Console.Out.WriteLine("Unfreezing worker {0} ...", id);
            /*IWorkerService worker = getWorker(id);
            if (worker.isJobTracker())
            {
                worker.unFreezeWorker();
            }*/
        }

    }
}
