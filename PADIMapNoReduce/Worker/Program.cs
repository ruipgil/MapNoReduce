using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;

namespace PADIMapNoReduce
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 30001;
            InnerWorker inner = new InnerWorker(port);
            Console.Out.WriteLine("Press enter to close");
            Console.In.ReadLine();
        }
    }
}
