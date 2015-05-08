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

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _controller.getStatus();
        }


        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        //Freeze Worker
        private void button3_Click(object sender, EventArgs e)
        {
                _controller.freezeWorkerW(int.Parse(freezeWText.Text));  
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _controller.freezeWorkerC(int.Parse(freezeJTText.Text));  
        }

        private void button5_Click(object sender, EventArgs e)
        {
                _controller.unFreezeWorkerW(int.Parse(unFreezeWText.Text)); 
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _controller.unFreezeWorkerC(int.Parse(unFreezeJTText.Text));    
        }

        private void button7_Click(object sender, EventArgs e)
        {
                _controller.slowWorker(int.Parse(slowIDText.Text), int.Parse(slowSeconds.Text));
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void step_Click(object sender, EventArgs e)
        {
            _controller.stepScript(inputFileTextBox.Text);
        }

        private void stop_Click(object sender, EventArgs e)
        {
            _controller.stopScript();
        }

        private void run_Click(object sender, EventArgs e)
        {
            String command = commandTextBox.Text;
            _controller.runCommand(command);
        }

    }
}
