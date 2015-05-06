namespace PADIMapNoReduce
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.otherText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.workerPort = new System.Windows.Forms.NumericUpDown();
            this.browseFolder = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.openInputFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.runScript = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.freezeWText = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.unFreezeWText = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.freezeJTText = new System.Windows.Forms.TextBox();
            this.unFreezeJTText = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.slowIDText = new System.Windows.Forms.TextBox();
            this.button7 = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.slowSeconds = new System.Windows.Forms.TextBox();
            this.stepButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.inputFileTextBox = new System.Windows.Forms.TextBox();
            this.commandTextBox = new System.Windows.Forms.TextBox();
            this.runButton = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.workerPort)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(273, 143);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(98, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Create Worker";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.createWorker_Click);
            // 
            // otherText
            // 
            this.otherText.Location = new System.Drawing.Point(89, 146);
            this.otherText.Name = "otherText";
            this.otherText.Size = new System.Drawing.Size(178, 20);
            this.otherText.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 127);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Worker Port";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 149);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Other Workers";
            // 
            // workerPort
            // 
            this.workerPort.Location = new System.Drawing.Point(89, 120);
            this.workerPort.Maximum = new decimal(new int[] {
            39999,
            0,
            0,
            0});
            this.workerPort.Minimum = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            this.workerPort.Name = "workerPort";
            this.workerPort.Size = new System.Drawing.Size(120, 20);
            this.workerPort.TabIndex = 5;
            this.workerPort.Value = new decimal(new int[] {
            30001,
            0,
            0,
            0});
            // 
            // browseFolder
            // 
            this.browseFolder.Location = new System.Drawing.Point(243, 12);
            this.browseFolder.Name = "browseFolder";
            this.browseFolder.Size = new System.Drawing.Size(35, 23);
            this.browseFolder.TabIndex = 6;
            this.browseFolder.TabStop = false;
            this.browseFolder.Text = "...";
            this.browseFolder.UseVisualStyleBackColor = true;
            this.browseFolder.Click += new System.EventHandler(this.selectInputFile);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Script:";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // openInputFileDialog
            // 
            this.openInputFileDialog.FileName = "openFileDialog1";
            // 
            // runScript
            // 
            this.runScript.Location = new System.Drawing.Point(124, 38);
            this.runScript.Name = "runScript";
            this.runScript.Size = new System.Drawing.Size(113, 23);
            this.runScript.TabIndex = 9;
            this.runScript.Text = "Run Script";
            this.runScript.UseVisualStyleBackColor = true;
            this.runScript.Click += new System.EventHandler(this.runScript_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(90, 169);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 10;
            this.button2.Text = "Status";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 174);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Status";
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 211);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Freeze Worker ID";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // freezeWText
            // 
            this.freezeWText.Location = new System.Drawing.Point(155, 204);
            this.freezeWText.Name = "freezeWText";
            this.freezeWText.Size = new System.Drawing.Size(100, 20);
            this.freezeWText.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 232);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(110, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "Freeze JobTracker ID";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // unFreezeWText
            // 
            this.unFreezeWText.Location = new System.Drawing.Point(155, 248);
            this.unFreezeWText.Name = "unFreezeWText";
            this.unFreezeWText.Size = new System.Drawing.Size(100, 20);
            this.unFreezeWText.TabIndex = 16;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(285, 201);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 17;
            this.button3.Text = "FreezeW";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(285, 223);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 18;
            this.button4.Text = "FreezeC";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(15, 255);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(105, 13);
            this.label7.TabIndex = 19;
            this.label7.Text = "UnFreeze Worker ID";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 277);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(124, 13);
            this.label8.TabIndex = 20;
            this.label8.Text = "UnFreeze JobTracker ID";
            // 
            // freezeJTText
            // 
            this.freezeJTText.Location = new System.Drawing.Point(155, 226);
            this.freezeJTText.Name = "freezeJTText";
            this.freezeJTText.Size = new System.Drawing.Size(100, 20);
            this.freezeJTText.TabIndex = 21;
            // 
            // unFreezeJTText
            // 
            this.unFreezeJTText.Location = new System.Drawing.Point(155, 270);
            this.unFreezeJTText.Name = "unFreezeJTText";
            this.unFreezeJTText.Size = new System.Drawing.Size(100, 20);
            this.unFreezeJTText.TabIndex = 22;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(285, 245);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(75, 23);
            this.button5.TabIndex = 23;
            this.button5.Text = "UnFreezeW";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(285, 267);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(75, 23);
            this.button6.TabIndex = 24;
            this.button6.Text = "UnFreezeC";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(15, 303);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(80, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "Slow Worker Id";
            this.label9.Click += new System.EventHandler(this.label9_Click);
            // 
            // slowIDText
            // 
            this.slowIDText.Location = new System.Drawing.Point(101, 300);
            this.slowIDText.Name = "slowIDText";
            this.slowIDText.Size = new System.Drawing.Size(55, 20);
            this.slowIDText.TabIndex = 26;
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(294, 298);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(75, 23);
            this.button7.TabIndex = 27;
            this.button7.Text = "SlowW";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(162, 303);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(49, 13);
            this.label10.TabIndex = 28;
            this.label10.Text = "Seconds";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // slowSeconds
            // 
            this.slowSeconds.Location = new System.Drawing.Point(217, 300);
            this.slowSeconds.Name = "slowSeconds";
            this.slowSeconds.Size = new System.Drawing.Size(55, 20);
            this.slowSeconds.TabIndex = 29;
            // 
            // stepButton
            // 
            this.stepButton.Location = new System.Drawing.Point(285, 12);
            this.stepButton.Name = "stepButton";
            this.stepButton.Size = new System.Drawing.Size(43, 23);
            this.stepButton.TabIndex = 30;
            this.stepButton.Text = "Step";
            this.stepButton.UseVisualStyleBackColor = true;
            this.stepButton.Click += new System.EventHandler(this.step_Click);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(334, 12);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(44, 23);
            this.stopButton.TabIndex = 31;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stop_Click);
            // 
            // inputFileTextBox
            // 
            this.inputFileTextBox.Location = new System.Drawing.Point(54, 12);
            this.inputFileTextBox.Name = "inputFileTextBox";
            this.inputFileTextBox.Size = new System.Drawing.Size(183, 20);
            this.inputFileTextBox.TabIndex = 7;
            // 
            // commandTextBox
            // 
            this.commandTextBox.Location = new System.Drawing.Point(74, 69);
            this.commandTextBox.Name = "commandTextBox";
            this.commandTextBox.Size = new System.Drawing.Size(213, 20);
            this.commandTextBox.TabIndex = 32;
            // 
            // runButton
            // 
            this.runButton.Location = new System.Drawing.Point(294, 67);
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(75, 23);
            this.runButton.TabIndex = 33;
            this.runButton.Text = "Run";
            this.runButton.UseVisualStyleBackColor = true;
            this.runButton.Click += new System.EventHandler(this.run_Click);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(11, 70);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(57, 13);
            this.label11.TabIndex = 34;
            this.label11.Text = "Command:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(381, 336);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.runButton);
            this.Controls.Add(this.commandTextBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.stepButton);
            this.Controls.Add(this.slowSeconds);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.slowIDText);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.unFreezeJTText);
            this.Controls.Add(this.freezeJTText);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.unFreezeWText);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.freezeWText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.runScript);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.inputFileTextBox);
            this.Controls.Add(this.browseFolder);
            this.Controls.Add(this.workerPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.otherText);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.workerPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox otherText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown workerPort;
        private System.Windows.Forms.Button browseFolder;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.OpenFileDialog openInputFileDialog;
        private System.Windows.Forms.Button runScript;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox freezeWText;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox unFreezeWText;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox freezeJTText;
        private System.Windows.Forms.TextBox unFreezeJTText;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox slowIDText;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox slowSeconds;
        private System.Windows.Forms.Button stepButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.TextBox inputFileTextBox;
        private System.Windows.Forms.TextBox commandTextBox;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Label label11;
    }
}

