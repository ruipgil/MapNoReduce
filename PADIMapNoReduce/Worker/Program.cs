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
        public static void Main(string[] args)
        {
            int port;
            if (Convert.ToInt32(args[0]) == 0)
            {
                port = 30001;
            } else {
                port = Convert.ToInt32(args[0]);
            }
            WorkingWorkerService inner = new WorkingWorkerService(port);

            Console.Out.WriteLine("Press enter to close");
            Console.In.ReadLine();
        }
    }
}
