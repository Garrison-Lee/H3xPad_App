
namespace MacroUpdater_FormsApp
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.updateButton = new System.Windows.Forms.Button();
            this.tapTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panelUserInput = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.pressTextBox = new System.Windows.Forms.TextBox();
            this.userLog = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.title = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panelUserInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // updateButton
            // 
            this.updateButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.updateButton.Cursor = System.Windows.Forms.Cursors.Default;
            this.updateButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.updateButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(219)))), ((int)(((byte)(255)))));
            this.updateButton.FlatAppearance.BorderSize = 2;
            this.updateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.updateButton.Font = new System.Drawing.Font("Cascadia Code SemiBold", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.updateButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(219)))), ((int)(((byte)(255)))));
            this.updateButton.Location = new System.Drawing.Point(256, 8);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(272, 59);
            this.updateButton.TabIndex = 0;
            this.updateButton.Text = "Update";
            this.updateButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
            this.updateButton.UseVisualStyleBackColor = false;
            this.updateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // tapTextBox
            // 
            this.tapTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.tapTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tapTextBox.Font = new System.Drawing.Font("Cascadia Code SemiBold", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tapTextBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(219)))), ((int)(((byte)(255)))));
            this.tapTextBox.Location = new System.Drawing.Point(200, 21);
            this.tapTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.tapTextBox.MaximumSize = new System.Drawing.Size(384, 36);
            this.tapTextBox.MaxLength = 512;
            this.tapTextBox.MinimumSize = new System.Drawing.Size(384, 30);
            this.tapTextBox.Name = "tapTextBox";
            this.tapTextBox.Size = new System.Drawing.Size(384, 19);
            this.tapTextBox.TabIndex = 1;
            this.tapTextBox.Text = "password";
            this.tapTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tapTextBox.WordWrap = false;
            this.tapTextBox.TextChanged += new System.EventHandler(this.tapTextBox_TextChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.updateButton);
            this.panel1.Location = new System.Drawing.Point(0, 362);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(256, 8, 256, 32);
            this.panel1.Size = new System.Drawing.Size(784, 99);
            this.panel1.TabIndex = 2;
            // 
            // panelUserInput
            // 
            this.panelUserInput.Controls.Add(this.button2);
            this.panelUserInput.Controls.Add(this.button1);
            this.panelUserInput.Controls.Add(this.label1);
            this.panelUserInput.Controls.Add(this.pressTextBox);
            this.panelUserInput.Controls.Add(this.userLog);
            this.panelUserInput.Controls.Add(this.label2);
            this.panelUserInput.Controls.Add(this.tapTextBox);
            this.panelUserInput.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelUserInput.Location = new System.Drawing.Point(0, 156);
            this.panelUserInput.Name = "panelUserInput";
            this.panelUserInput.Padding = new System.Windows.Forms.Padding(200, 40, 200, 0);
            this.panelUserInput.Size = new System.Drawing.Size(784, 208);
            this.panelUserInput.TabIndex = 3;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(33, 130);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(127, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Log Contents";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Cascadia Code SemiBold", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(221)))), ((int)(((byte)(74)))));
            this.label1.Location = new System.Drawing.Point(200, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Short Press:";
            // 
            // pressTextBox
            // 
            this.pressTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.pressTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pressTextBox.Font = new System.Drawing.Font("Cascadia Code SemiBold", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pressTextBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(219)))), ((int)(((byte)(255)))));
            this.pressTextBox.Location = new System.Drawing.Point(200, 92);
            this.pressTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.pressTextBox.MaximumSize = new System.Drawing.Size(384, 36);
            this.pressTextBox.MaxLength = 512;
            this.pressTextBox.MinimumSize = new System.Drawing.Size(384, 30);
            this.pressTextBox.Name = "pressTextBox";
            this.pressTextBox.Size = new System.Drawing.Size(384, 19);
            this.pressTextBox.TabIndex = 4;
            this.pressTextBox.Text = "password";
            this.pressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.pressTextBox.WordWrap = false;
            this.pressTextBox.TextChanged += new System.EventHandler(this.pressTextBox_TextChanged);
            // 
            // userLog
            // 
            this.userLog.Font = new System.Drawing.Font("Cascadia Code SemiBold", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.userLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(144)))), ((int)(((byte)(0)))));
            this.userLog.Location = new System.Drawing.Point(203, 175);
            this.userLog.Name = "userLog";
            this.userLog.Size = new System.Drawing.Size(384, 28);
            this.userLog.TabIndex = 2;
            this.userLog.Text = "I have something to tell you!";
            this.userLog.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Cascadia Code SemiBold", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(221)))), ((int)(((byte)(74)))));
            this.label2.Location = new System.Drawing.Point(200, 74);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Long Press:";
            // 
            // title
            // 
            this.title.Dock = System.Windows.Forms.DockStyle.Top;
            this.title.Font = new System.Drawing.Font("Cascadia Code SemiBold", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.title.Location = new System.Drawing.Point(0, 0);
            this.title.Margin = new System.Windows.Forms.Padding(30);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(784, 156);
            this.title.TabIndex = 1;
            this.title.Text = "Hello, Mom! \r\nWhat would you like your MacroPad to output?";
            this.title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(33, 175);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(127, 23);
            this.button2.TabIndex = 7;
            this.button2.Text = "Ping";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(16)))), ((int)(((byte)(20)))));
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panelUserInput);
            this.Controls.Add(this.title);
            this.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "MacroPad Updater";
            this.panel1.ResumeLayout(false);
            this.panelUserInput.ResumeLayout(false);
            this.panelUserInput.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.TextBox tapTextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelUserInput;
        private System.Windows.Forms.Label userLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox pressTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}

