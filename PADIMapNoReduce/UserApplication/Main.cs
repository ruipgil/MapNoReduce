using PADIMapNoReduce;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserApplication
{
    public partial class Main : Form
    {
        ClientService client;

        public Main()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void toggleUI()
        {
            portUpDown.Enabled = !portUpDown.Enabled;
            addressTextBox.Enabled = !addressTextBox.Enabled;

            inputFileTextBox.Enabled = !inputFileTextBox.Enabled;
            outputFolderTextBox.Enabled = !outputFolderTextBox.Enabled;
            mapperFileTextBox.Enabled = !mapperFileTextBox.Enabled;
            mapperClassnameTextBox.Enabled = !mapperClassnameTextBox.Enabled;
            splitsUpDown.Enabled = !splitsUpDown.Enabled;
            workButton.Enabled = !workButton.Enabled;
        }

        private void work(object sender, EventArgs e)
        {
            try
            {
                int clientPort = Decimal.ToInt32(portUpDown.Value);
                string workerAddress = addressTextBox.Text;

                client = new ClientService(clientPort);
                client.init(workerAddress);

                string inputFilePath = inputFileTextBox.Text;
                string mapperPath = mapperFileTextBox.Text;
                byte[] code = System.IO.File.ReadAllBytes(mapperPath);
                string mapperName = mapperClassnameTextBox.Text;
                int splits = Decimal.ToInt32(splitsUpDown.Value);
                string outputFolder = outputFolderTextBox.Text;

				client.submit(inputFilePath, outputFolder, splits, code, mapperName, ()=> {
                    MessageBox.Show("Work completed with success", "Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    client = null;
                    //toggleUI();
				});

                //toggleUI();
            }
            catch (SocketException exp)
            {
                if(exp.ErrorCode == 10061)
                    MessageBox.Show("Could not connect to worker at " + addressTextBox.Text, "Error",
MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if(exp.ErrorCode == 10048)
                    MessageBox.Show("Port " + portUpDown.Value+" cannot be used. Is it in use already?", "Error",
MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(exp.ToString(), "Error",
MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            catch (Exception exp) {
                MessageBox.Show(exp.Message, "Error Message",
    MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                MessageBox.Show(exp+"", "Stack Trace",
MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
            }
        }

        private void selectInputFile(object sender, EventArgs e)
        {
            selectFile(openInputFileDialog, inputFileTextBox);
        }

        private void selectOutputFolder(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                outputFolderTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void selectMapper(object sender, EventArgs e)
        {
            selectFile(openDllFileDialog, mapperFileTextBox);
        }

        private void selectFile(OpenFileDialog dialog, TextBox tb)
        {
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                tb.Text = dialog.FileName;
            }
        }

    }
}
