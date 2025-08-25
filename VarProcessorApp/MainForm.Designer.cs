namespace VarProcessorApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.logWindow = new System.Windows.Forms.TextBox();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.clearLogButton = new System.Windows.Forms.Button();
            this.reassembleButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // logWindow
            // 
            this.logWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logWindow.Location = new System.Drawing.Point(0, 0);
            this.logWindow.Multiline = true;
            this.logWindow.Name = "logWindow";
            this.logWindow.ReadOnly = true;
            this.logWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logWindow.Size = new System.Drawing.Size(800, 411);
            this.logWindow.TabIndex = 0;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.clearLogButton);
            this.buttonPanel.Controls.Add(this.reassembleButton);
            this.buttonPanel.Controls.Add(this.startButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 411);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(800, 39);
            this.buttonPanel.TabIndex = 1;
            // 
            // clearLogButton
            // 
            this.clearLogButton.Location = new System.Drawing.Point(500, 6);
            this.clearLogButton.Name = "clearLogButton";
            this.clearLogButton.Size = new System.Drawing.Size(100, 23);
            this.clearLogButton.TabIndex = 2;
            this.clearLogButton.Text = "清除日誌";
            this.clearLogButton.UseVisualStyleBackColor = true;
            this.clearLogButton.Click += new System.EventHandler(this.clearLogButton_Click);
            // 
            // reassembleButton
            // 
            this.reassembleButton.Location = new System.Drawing.Point(250, 6);
            this.reassembleButton.Name = "reassembleButton";
            this.reassembleButton.Size = new System.Drawing.Size(100, 23);
            this.reassembleButton.TabIndex = 1;
            this.reassembleButton.Text = "重新組裝";
            this.reassembleButton.UseVisualStyleBackColor = true;
            this.reassembleButton.Click += new System.EventHandler(this.reassembleButton_Click);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(12, 6);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(100, 23);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "開始處理";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.logWindow);
            this.Controls.Add(this.buttonPanel);
            this.Name = "MainForm";
            this.Text = "VAR Processor";
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox logWindow;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button reassembleButton;
        private System.Windows.Forms.Button clearLogButton;
    }
}