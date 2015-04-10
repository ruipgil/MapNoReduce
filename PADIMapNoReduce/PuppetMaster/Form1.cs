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

        private void button1_Click(object sender, EventArgs e)
        {
            //Console.Out.WriteLine("Creating Worker");
            _controller.createWorker();
            
        }
    }
}
