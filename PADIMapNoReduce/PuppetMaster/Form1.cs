using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PADIMapNoReduce
{
    public partial class Form1 : Form
    {
        Controller _controller;
        public Form1(Controller controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        

        private void createWorker_Click(object sender, EventArgs e)
        {
            //Console.Out.WriteLine("Creating Worker");
            int port = Decimal.ToInt32(workerPort.Value);
            String otherWorkers = otherText.Text;
            _controller.createWorker(port, otherWorkers);

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
