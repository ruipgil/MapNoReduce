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

        public void createWorker()
        {
            Process.Start("Worker.exe", Convert.ToString(workerPortNr));
            workerPortNr++;
        }

    }
}
