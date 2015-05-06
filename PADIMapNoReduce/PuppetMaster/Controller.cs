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
                // Console.Out.WriteLine(splits[1]);

                //SAVE WORKER ID
                KeyValuePair<int, string> kvpair = new KeyValuePair<int, string>(int.Parse(tempSplits[0]), tempSplits[2]);
                workerIds.Add(kvpair);

                String pmEntryUrl = tempSplits[1];
                IPuppetMasterService pm = (IPuppetMasterService)Activator.GetObject(typeof(IPuppetMasterService), pmEntryUrl);
                //TODO what to do with <entryurl> (split[4])???

                //TODO IMPROVE THE CREATION OF WORKER GIVING THE URL
                pm.createWorker(tempSplits[2], tempSplits[3]);

            }
            if (splits[0].Equals("SUBMIT"))
            {
                Console.Out.WriteLine("Submitting job...");
                string[] tempSplits = splits[1].Split(new string[] { " " }, 6, StringSplitOptions.None);
                String workerEntryUrl = tempSplits[0];
                String inputFile = tempSplits[1];
                String outputFile = tempSplits[2];
                int nrSplits = int.Parse(tempSplits[3]);
                String mapName = tempSplits[4];
                String mapPath = tempSplits[5];
                IWorkerService worker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), workerEntryUrl);


                // TODO I ASSUME THE CLIENT IS ALWAYS IN THE SAME LOCATION!!
                IClientService client = (IClientService)Activator.GetObject(typeof(IWorkerService), "tcp://localhost:10001/C");
                byte[] code = System.IO.File.ReadAllBytes(mapPath);
                client.submit(inputFile, outputFile, nrSplits, code, mapName);


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
                freezeWorker(int.Parse(splits[1]));
            }
            if (splits[0].Equals("UNFREEZEW"))
            {
                unFreezeWorker(int.Parse(splits[1]));
            }
            if (splits[0].Equals("FREEZEC"))
            {
                //call freeze with job tracker id
            }
            if (splits[0].Equals("UNFREEZEC"))
            {
                // call unfreeze with job tracker id
            }
        }


        public void createWorker()
        {
            String s = Convert.ToString(workerPortNr);
            /*string[] args = new string[1];
            args[0] = s;*/
            Console.Out.WriteLine("Creating Worker @ port {0}", workerPortNr);
            Process.Start("Worker.exe", s);
            workerPortNr++;
        }

        public void createWorker(int port, string otherWorkers)
        {
            String s = Convert.ToString(port) + " " + otherWorkers;
            /*string[] args = new string[1];
            args[0] = s;*/
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Process.Start("Worker.exe", s);
            
        }
        
        public void createWorker(string url, string otherWorkers)
        {
            //TODO IMPROVE THIS : Only works for localhost (i think)
            String[] splits = url.Split(new string[] { ":" }, 3, StringSplitOptions.None);
            String[] splits2 = splits[2].Split(new string[] { "/" }, 2, StringSplitOptions.None);
            int port = int.Parse(splits2[0]);
            String s = splits2[0] + " " + otherWorkers;
            /*string[] args = new string[1];
            args[0] = s;*/
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Process.Start("Worker.exe", s);

        }

        public void getStatus()
        {
            Console.Out.WriteLine("Obtaining the workers and job trackers status");
            Console.Out.WriteLine("Status:");
            foreach (KeyValuePair<int, string> pair in workerIds)
            {
                IWorkerService worker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), pair.Value);
                Console.WriteLine("Worker {0} : {1} ", pair.Key, worker.getStatus());
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

        public void freezeWorker(int id)
        {
            Console.Out.WriteLine("Freezing worker {0} ...", id);
            IWorkerService worker = getWorker(id);
            worker.freezeWorker();
        }

        public void unFreezeWorker(int id)
        {
            Console.Out.WriteLine("Unfreezing worker {0} ...", id);
            IWorkerService worker = getWorker(id);
            worker.unFreezeWorker();
        }

    }
}
