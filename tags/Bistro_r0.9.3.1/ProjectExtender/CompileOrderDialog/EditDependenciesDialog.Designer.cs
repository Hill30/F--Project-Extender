namespace FSharp.ProjectExtender
{
    partial class EditDependenciesDialog
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
            System.Windows.Forms.Panel panel1;
            System.Windows.Forms.Button CancelButton;
            System.Windows.Forms.Button OKButton;
            this.Dependencies = new System.Windows.Forms.CheckedListBox();
            panel1 = new System.Windows.Forms.Panel();
            CancelButton = new System.Windows.Forms.Button();
            OKButton = new System.Windows.Forms.Button();
            panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(CancelButton);
            panel1.Controls.Add(OKButton);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 203);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(284, 61);
            panel1.TabIndex = 1;
            // 
            // CancelButton
            // 
            CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            CancelButton.Location = new System.Drawing.Point(156, 17);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new System.Drawing.Size(75, 23);
            CancelButton.TabIndex = 1;
            CancelButton.Text = "Cancel";
            CancelButton.UseVisualStyleBackColor = true;
            // 
            // OKButton
            // 
            OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            OKButton.Location = new System.Drawing.Point(53, 17);
            OKButton.Name = "OKButton";
            OKButton.Size = new System.Drawing.Size(75, 23);
            OKButton.TabIndex = 0;
            OKButton.Text = "OK";
            OKButton.UseVisualStyleBackColor = true;
            // 
            // Dependencies
            // 
            this.Dependencies.CheckOnClick = true;
            this.Dependencies.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Dependencies.FormattingEnabled = true;
            this.Dependencies.Location = new System.Drawing.Point(0, 0);
            this.Dependencies.Name = "Dependencies";
            this.Dependencies.Size = new System.Drawing.Size(284, 259);
            this.Dependencies.TabIndex = 0;
            // 
            // EditDependenciesDialog
            // 
            this.AcceptButton = OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = CancelButton;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.Controls.Add(panel1);
            this.Controls.Add(this.Dependencies);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditDependenciesDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Dependencies";
            panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.CheckedListBox Dependencies;

    }
}