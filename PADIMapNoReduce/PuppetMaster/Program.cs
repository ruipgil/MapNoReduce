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
        static void Main()
        {
            //int workerPort = 30001;
            Controller controller = new Controller();
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
