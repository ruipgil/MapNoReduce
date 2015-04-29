using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace PADIMapNoReduce
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            int port;
            List<string> pmMap = new List<string>();
            if (args.Length == 0)
            {
                port = 20001;
            }
            else
            {
                port = int.Parse(args[0]);
                if (port < 20001 || port > 29999)
                {
                    Console.Error.WriteLine("PuppetMaster ports must be between 20001 and 29999");
                    return;
                }
                if (args.Length > 1)
                {
                    int s = args.Length - 1;
                    if (s < 0)
                    {
                        s = 0;
                    }
                    pmMap = (new List<string>(args)).GetRange(1, s);
                }
            }
            
            Controller controller = new Controller();

            PuppetMasterService puppetMaster = new PuppetMasterService(port, controller);

            //int workerPort = 30001;
            
            controller.run();
            /*Process.Start("Worker.exe", Convert.ToString(workerPort));
            workerPort++;
            Process.Start("Worker.exe", Convert.ToString(workerPort));*/
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(controller));
        }
    }
}
