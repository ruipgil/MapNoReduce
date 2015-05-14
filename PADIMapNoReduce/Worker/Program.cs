﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Threading;

namespace PADIMapNoReduce
{
    public class Program
    {
        /// <summary>
        /// If no arguments provided it will initialize in the port 30001.
        /// If there are arguments, the first one is the port (between 30001
        /// and 39999). The rest should be the address of other workers.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            int port;
			string address = "tcp://localhost";
            List<string> workerMap = new List<string>();
            if (args.Length == 0)
            {
                port = 30001;
            }
            else
            {
                port = int.Parse(args[0]);
                if (port < 30001 || port > 39999)
                {
                    Console.Error.WriteLine("Workers ports must be between 30001 and 39999");
                    return;
                }
                if (args.Length > 1)
                {
                    int s = args.Length - 1;
                    if (s < 0)
                    {
                        s = 0;
                    }
                    workerMap = (new List<string>(args)).GetRange(1, s);
					var temp = workerMap.IndexOf ("-a");
					
					if (temp>=0 && workerMap.Count-1>temp) {
						address = workerMap [temp + 1];
						workerMap.Remove (address);
					}
					workerMap.Remove ("-a");
                }
            }
            TrackerService tracker = new TrackerService(address, port);
			tracker.addKnownWorkers (workerMap);

			ThreadStart ts = new ThreadStart(() => {
				while(true) {
					tracker.startHeartbeating();
					Thread.Sleep(2000);
				}
			});
			Thread t = new Thread(ts);
			t.Start();

			ThreadStart ts2 = new ThreadStart(()=>{
				while(true) {
					tracker.startSharingKnownWorkers();
					Thread.Sleep(2000);
				}
			});
			Thread t2 = new Thread(ts2);
			t2.Start();

			/*ThreadStart ts3 = new ThreadStart(()=>{
				while(true) {
					tracker.monitorSplits();
					Thread.Sleep(2000);
				}
			});
			Thread t3 = new Thread(ts3);
			t3.Start();*/

            Console.Out.WriteLine("Press enter to close");
            Console.In.ReadLine();
			t.Abort ();
        }
    }
}
