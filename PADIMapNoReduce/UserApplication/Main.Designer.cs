namespace UserApplication
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.connectButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.addressTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.inputFileTextBox = new System.Windows.Forms.TextBox();
            this.outputFolderTextBox = new System.Windows.Forms.TextBox();
            this.mapperFileTextBox = new System.Windows.Forms.TextBox();
            this.mapperClassnameTextBox = new System.Windows.Forms.TextBox();
            this.workButton = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.portUpDown = new System.Windows.Forms.NumericUpDown();
            this.splitsUpDown = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.openInputFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.openDllFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.portUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitsUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(293, 35);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 0;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Worker endpoint";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // addressTextBox
            // 
            this.addressTextBox.Location = new System.Drawing.Point(118, 37);
            this.addressTextBox.Name = "addressTextBox";
            this.addressTextBox.Size = new System.Drawing.Size(169, 20);
            this.addressTextBox.TabIndex = 2;
            this.addressTextBox.Text = "tcp://localhost:30001/W";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Client port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 89);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Input file";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 124);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Output folder";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 160);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Mapper file";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 194);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(99, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Mapper class name";
            // 
            // inputFileTextBox
            // 
            this.inputFileTextBox.Enabled = false;
            this.inputFileTextBox.Location = new System.Drawing.Point(118, 86);
            this.inputFileTextBox.Name = "inputFileTextBox";
            this.inputFileTextBox.Size = new System.Drawing.Size(201, 20);
            this.inputFileTextBox.TabIndex = 9;
            this.inputFileTextBox.Text = "./input.txt";
            // 
            // outputFolderTextBox
            // 
            this.outputFolderTextBox.Enabled = false;
            this.outputFolderTextBox.Location = new System.Drawing.Point(118, 121);
            this.outputFolderTextBox.Name = "outputFolderTextBox";
            this.outputFolderTextBox.Size = new System.Drawing.Size(201, 20);
            this.outputFolderTextBox.TabIndex = 10;
            this.outputFolderTextBox.Text = "./";
            // 
            // mapperFileTextBox
            // 
            this.mapperFileTextBox.Enabled = false;
            this.mapperFileTextBox.Location = new System.Drawing.Point(118, 157);
            this.mapperFileTextBox.Name = "mapperFileTextBox";
            this.mapperFileTextBox.Size = new System.Drawing.Size(201, 20);
            this.mapperFileTextBox.TabIndex = 11;
            this.mapperFileTextBox.Text = "./Shared.dll";
            // 
            // mapperClassnameTextBox
            // 
            this.mapperClassnameTextBox.Enabled = false;
            this.mapperClassnameTextBox.Location = new System.Drawing.Point(118, 191);
            this.mapperClassnameTextBox.Name = "mapperClassnameTextBox";
            this.mapperClassnameTextBox.Size = new System.Drawing.Size(250, 20);
            this.mapperClassnameTextBox.TabIndex = 12;
            this.mapperClassnameTextBox.Text = "SampleMapper";
            // 
            // workButton
            // 
            this.workButton.Enabled = false;
            this.workButton.Location = new System.Drawing.Point(293, 260);
            this.workButton.Name = "workButton";
            this.workButton.Size = new System.Drawing.Size(75, 23);
            this.workButton.TabIndex = 13;
            this.workButton.Text = "Work";
            this.workButton.UseVisualStyleBackColor = true;
            this.workButton.Click += new System.EventHandler(this.work);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 227);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Splits";
            // 
            // portUpDown
            // 
            this.portUpDown.Location = new System.Drawing.Point(118, 11);
            this.portUpDown.Maximum = new decimal(new int[] {
            19999,
            0,
            0,
            0});
            this.portUpDown.Minimum = new decimal(new int[] {
            10001,
            0,
            0,
            0});
            this.portUpDown.Name = "portUpDown";
            this.portUpDown.Size = new System.Drawing.Size(63, 20);
            this.portUpDown.TabIndex = 16;
            this.portUpDown.Value = new decimal(new int[] {
            10001,
            0,
            0,
            0});
            // 
            // splitsUpDown
            // 
            this.splitsUpDown.Enabled = false;
            this.splitsUpDown.Location = new System.Drawing.Point(118, 225);
            this.splitsUpDown.Maximum = new decimal(new int[] {
            -402653185,
            -1613725636,
            54210108,
            0});
            this.splitsUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.splitsUpDown.Name = "splitsUpDown";
            this.splitsUpDown.Size = new System.Drawing.Size(250, 20);
            this.splitsUpDown.TabIndex = 17;
            this.splitsUpDown.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(325, 84);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(43, 23);
            this.button1.TabIndex = 18;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.selectInputFile);
            // 
            // openInputFileDialog
            // 
            this.openInputFileDialog.FileName = "openFileDialog1";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(325, 119);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(43, 23);
            this.button2.TabIndex = 19;
            this.button2.Text = "...";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.selectOutputFolder);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(325, 155);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(43, 23);
            this.button3.TabIndex = 20;
            this.button3.Text = "...";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.selectMapper);
            // 
            // openDllFileDialog
            // 
            this.openDllFileDialog.FileName = "openFileDialog1";
            this.openDllFileDialog.Filter = "Library (*.dll)|*.dll";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(386, 295);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.splitsUpDown);
            this.Controls.Add(this.portUpDown);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.workButton);
            this.Controls.Add(this.mapperClassnameTextBox);
            this.Controls.Add(this.mapperFileTextBox);
            this.Controls.Add(this.outputFolderTextBox);
            this.Controls.Add(this.inputFileTextBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.addressTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.connectButton);
            this.Name = "Main";
            this.Text = "User Application - PADI MapNoReduce";
            ((System.ComponentModel.ISupportInitialize)(this.portUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitsUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox addressTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox inputFileTextBox;
        private System.Windows.Forms.TextBox outputFolderTextBox;
        private System.Windows.Forms.TextBox mapperFileTextBox;
        private System.Windows.Forms.TextBox mapperClassnameTextBox;
        private System.Windows.Forms.Button workButton;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown portUpDown;
        private System.Windows.Forms.NumericUpDown splitsUpDown;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.OpenFileDialog openInputFileDialog;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.OpenFileDialog openDllFileDialog;
    }
}

