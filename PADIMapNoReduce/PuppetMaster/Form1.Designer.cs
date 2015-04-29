﻿namespace PADIMapNoReduce
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
            this.inputFileTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.openInputFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.runScript = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.workerPort)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(90, 238);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Create Worker";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.createWorker_Click);
            // 
            // otherText
            // 
            this.otherText.Location = new System.Drawing.Point(102, 212);
            this.otherText.Name = "otherText";
            this.otherText.Size = new System.Drawing.Size(178, 20);
            this.otherText.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 186);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Worker Port";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 215);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Other Workers";
            // 
            // workerPort
            // 
            this.workerPort.Location = new System.Drawing.Point(102, 186);
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
            this.browseFolder.Location = new System.Drawing.Point(244, 31);
            this.browseFolder.Name = "browseFolder";
            this.browseFolder.Size = new System.Drawing.Size(35, 23);
            this.browseFolder.TabIndex = 6;
            this.browseFolder.TabStop = false;
            this.browseFolder.Text = "...";
            this.browseFolder.UseVisualStyleBackColor = true;
            this.browseFolder.Click += new System.EventHandler(this.selectInputFile);
            // 
            // inputFileTextBox
            // 
            this.inputFileTextBox.Location = new System.Drawing.Point(55, 31);
            this.inputFileTextBox.Name = "inputFileTextBox";
            this.inputFileTextBox.Size = new System.Drawing.Size(183, 20);
            this.inputFileTextBox.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 34);
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
            this.runScript.Location = new System.Drawing.Point(90, 57);
            this.runScript.Name = "runScript";
            this.runScript.Size = new System.Drawing.Size(124, 23);
            this.runScript.TabIndex = 9;
            this.runScript.Text = "Run Script";
            this.runScript.UseVisualStyleBackColor = true;
            this.runScript.Click += new System.EventHandler(this.runScript_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 273);
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
        private System.Windows.Forms.TextBox inputFileTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.OpenFileDialog openInputFileDialog;
        private System.Windows.Forms.Button runScript;
    }
}

