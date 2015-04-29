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

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void selectInputFile(object sender, EventArgs e)
        {
            selectFile(openInputFileDialog, inputFileTextBox);
        }

        private void selectFile(OpenFileDialog dialog, TextBox tb)
        {
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                tb.Text = dialog.FileName;
            }
        }

        private void runScript_Click(object sender, EventArgs e)
        {
            _controller.runScript(inputFileTextBox.Text);
        }

    }
}
