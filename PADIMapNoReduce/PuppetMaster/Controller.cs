using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PADIMapNoReduce
{
    public class Controller

    {
        private int workerPortNr = 30001;

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
                string[] splits = line.Split(new string[] { " " }, 5, StringSplitOptions.None);
                if (splits[0].Equals("WORKER"))
                {
                    Console.Out.WriteLine("Creating Worker...");
                   // Console.Out.WriteLine(splits[1]);
                    
                    //TODO SAVE WORKER ID
                    //splits[1] something something

                    String pmEntryUrl = splits[2];
                    IPuppetMasterService pm = (IPuppetMasterService)Activator.GetObject(typeof(IPuppetMasterService), pmEntryUrl);
                    //TODO what to do with <entryurl> (split[4])???
                    
                    //TODO IMPROVE THE CREATION OF WORKER GIVING THE URL
                    pm.createWorker(splits[3], splits[4]);
                   
                }
                if (splits[0].Equals("SUBMIT"))
                {
                }
                if (splits[0].Equals("WAIT"))
                {
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
