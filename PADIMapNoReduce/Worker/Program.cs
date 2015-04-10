using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;

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
                    workerMap = (new List<string>(args)).GetRange(1, args.Length - 2);
                }
            }
            WorkingWorkerService inner = new WorkingWorkerService(port);
            inner.updateWorkerMap(workerMap);

            Console.Out.WriteLine("Press enter to close");
            Console.In.ReadLine();
        }
    }
}
