﻿namespace RMS_Proofing
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.ButtonLoadFile = new System.Windows.Forms.Button();
            this.ButtonPlayFile = new System.Windows.Forms.Button();
            this.LabelAudioFile = new System.Windows.Forms.Label();
            this.TextboxFileName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TextboxBitrate = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.TextboxSampleRate = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.TextboxChannelCount = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.TextboxEncodingType = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.TextboxAudioFrames = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(830, 14);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(200, 86);
            this.button1.TabIndex = 0;
            this.button1.Text = "RMS Calculation";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Visible = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(826, 110);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "RMS: ";
            this.label1.Visible = false;
            // 
            // textBox1
            // 
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(887, 110);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(113, 19);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "< Undefined >";
            this.textBox1.Visible = false;
            // 
            // ButtonLoadFile
            // 
            this.ButtonLoadFile.Image = ((System.Drawing.Image)(resources.GetObject("ButtonLoadFile.Image")));
            this.ButtonLoadFile.Location = new System.Drawing.Point(39, 20);
            this.ButtonLoadFile.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ButtonLoadFile.Name = "ButtonLoadFile";
            this.ButtonLoadFile.Size = new System.Drawing.Size(38, 37);
            this.ButtonLoadFile.TabIndex = 3;
            this.ButtonLoadFile.UseVisualStyleBackColor = true;
            this.ButtonLoadFile.Click += new System.EventHandler(this.ButtonLoadFile_Click);
            // 
            // ButtonPlayFile
            // 
            this.ButtonPlayFile.Enabled = false;
            this.ButtonPlayFile.Image = ((System.Drawing.Image)(resources.GetObject("ButtonPlayFile.Image")));
            this.ButtonPlayFile.Location = new System.Drawing.Point(84, 20);
            this.ButtonPlayFile.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ButtonPlayFile.Name = "ButtonPlayFile";
            this.ButtonPlayFile.Size = new System.Drawing.Size(38, 37);
            this.ButtonPlayFile.TabIndex = 4;
            this.ButtonPlayFile.UseVisualStyleBackColor = true;
            this.ButtonPlayFile.Click += new System.EventHandler(this.ButtonPlayFile_Click);
            // 
            // LabelAudioFile
            // 
            this.LabelAudioFile.AutoSize = true;
            this.LabelAudioFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelAudioFile.Location = new System.Drawing.Point(39, 105);
            this.LabelAudioFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelAudioFile.Name = "LabelAudioFile";
            this.LabelAudioFile.Size = new System.Drawing.Size(83, 20);
            this.LabelAudioFile.TabIndex = 5;
            this.LabelAudioFile.Text = "Audio File:";
            // 
            // TextboxFileName
            // 
            this.TextboxFileName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxFileName.Location = new System.Drawing.Point(155, 106);
            this.TextboxFileName.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxFileName.Name = "TextboxFileName";
            this.TextboxFileName.ReadOnly = true;
            this.TextboxFileName.Size = new System.Drawing.Size(455, 19);
            this.TextboxFileName.TabIndex = 6;
            this.TextboxFileName.Text = "< No File Loaded> ";
            this.TextboxFileName.TextChanged += new System.EventHandler(this.TextboxFileName_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(39, 136);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Bit Rate: ";
            // 
            // TextboxBitrate
            // 
            this.TextboxBitrate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxBitrate.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxBitrate.Location = new System.Drawing.Point(159, 137);
            this.TextboxBitrate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxBitrate.Name = "TextboxBitrate";
            this.TextboxBitrate.ReadOnly = true;
            this.TextboxBitrate.Size = new System.Drawing.Size(218, 19);
            this.TextboxBitrate.TabIndex = 8;
            this.TextboxBitrate.Text = "placeholder";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(39, 156);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 20);
            this.label3.TabIndex = 9;
            this.label3.Text = "Sample Rate: ";
            // 
            // TextboxSampleRate
            // 
            this.TextboxSampleRate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxSampleRate.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxSampleRate.Location = new System.Drawing.Point(159, 156);
            this.TextboxSampleRate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxSampleRate.Name = "TextboxSampleRate";
            this.TextboxSampleRate.ReadOnly = true;
            this.TextboxSampleRate.Size = new System.Drawing.Size(218, 19);
            this.TextboxSampleRate.TabIndex = 10;
            this.TextboxSampleRate.Text = "placeholder";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(39, 176);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(84, 20);
            this.label4.TabIndex = 11;
            this.label4.Text = "Channels: ";
            // 
            // TextboxChannelCount
            // 
            this.TextboxChannelCount.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxChannelCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxChannelCount.Location = new System.Drawing.Point(159, 177);
            this.TextboxChannelCount.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxChannelCount.Name = "TextboxChannelCount";
            this.TextboxChannelCount.ReadOnly = true;
            this.TextboxChannelCount.Size = new System.Drawing.Size(218, 19);
            this.TextboxChannelCount.TabIndex = 12;
            this.TextboxChannelCount.Text = "placeholder";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(39, 196);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 20);
            this.label5.TabIndex = 13;
            this.label5.Text = "Encoding: ";
            // 
            // TextboxEncodingType
            // 
            this.TextboxEncodingType.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxEncodingType.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxEncodingType.Location = new System.Drawing.Point(159, 197);
            this.TextboxEncodingType.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxEncodingType.Name = "TextboxEncodingType";
            this.TextboxEncodingType.ReadOnly = true;
            this.TextboxEncodingType.Size = new System.Drawing.Size(218, 19);
            this.TextboxEncodingType.TabIndex = 14;
            this.TextboxEncodingType.Text = "placeholder";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(39, 216);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(112, 20);
            this.label6.TabIndex = 15;
            this.label6.Text = "Audio Frames:";
            // 
            // TextboxAudioFrames
            // 
            this.TextboxAudioFrames.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextboxAudioFrames.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextboxAudioFrames.Location = new System.Drawing.Point(159, 217);
            this.TextboxAudioFrames.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TextboxAudioFrames.Name = "TextboxAudioFrames";
            this.TextboxAudioFrames.ReadOnly = true;
            this.TextboxAudioFrames.Size = new System.Drawing.Size(218, 19);
            this.TextboxAudioFrames.TabIndex = 16;
            this.TextboxAudioFrames.Text = "placeholder";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1039, 692);
            this.Controls.Add(this.TextboxAudioFrames);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.TextboxEncodingType);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.TextboxChannelCount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.TextboxSampleRate);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TextboxBitrate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.TextboxFileName);
            this.Controls.Add(this.LabelAudioFile);
            this.Controls.Add(this.ButtonPlayFile);
            this.Controls.Add(this.ButtonLoadFile);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form1";
            this.Text = "Encoding: ";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button ButtonLoadFile;
        private System.Windows.Forms.Button ButtonPlayFile;
        private System.Windows.Forms.Label LabelAudioFile;
        private System.Windows.Forms.TextBox TextboxFileName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextboxBitrate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TextboxSampleRate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox TextboxChannelCount;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox TextboxEncodingType;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox TextboxAudioFrames;
    }
}

