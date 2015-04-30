﻿using System;
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

        public void run()
        {
            Console.Out.WriteLine("Console For PuppetMaster:");

        }

        public void runScript(String location)
        {
            Console.Out.WriteLine("Running Script...");
            String line;
            System.IO.StreamReader file = new System.IO.StreamReader(location);
            while ((line = file.ReadLine()) != null)
            {
                //Console.WriteLine(line);
                string[] splits = line.Split(new string[] { " " }, 2, StringSplitOptions.None);
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
                }
                if (splits[0].Equals("SLOWW"))
                {
                }
                if (splits[0].Equals("FREEZEW"))
                {
                }
                if (splits[0].Equals("UNFREEZEW"))
                {
                }
                if (splits[0].Equals("FREEZEC"))
                {
                }
                if (splits[0].Equals("UNFREEZEC"))
                {
                }
      
            }

            file.Close();

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
            //TODO IMPROVE THIS
            String[] splits = url.Split(new string[] { ":" }, 3, StringSplitOptions.None);
            String[] splits2 = splits[2].Split(new string[] { "/" }, 2, StringSplitOptions.None);
            int port = int.Parse(splits2[0]);
            String s = splits2[0] + " " + otherWorkers;
            /*string[] args = new string[1];
            args[0] = s;*/
            Console.Out.WriteLine("Creating Worker @ port {0}", port);
            Process.Start("Worker.exe", s);

        }

    }
}
