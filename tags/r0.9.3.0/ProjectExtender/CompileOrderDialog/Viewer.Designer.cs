namespace FSharp.ProjectExtender
{
    partial class CompileOrderViewer
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.CompileItems = new System.Windows.Forms.TreeView();
            this.panel3 = new System.Windows.Forms.Panel();
            this.MoveDown = new System.Windows.Forms.Button();
            this.MoveUp = new System.Windows.Forms.Button();
            this.compileItemMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addDependencyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.compileItemMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(382, 35);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.CompileItems);
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 35);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(382, 276);
            this.panel2.TabIndex = 1;
            // 
            // CompileItems
            // 
            this.CompileItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CompileItems.HideSelection = false;
            this.CompileItems.Location = new System.Drawing.Point(0, 0);
            this.CompileItems.Name = "CompileItems";
            this.CompileItems.Size = new System.Drawing.Size(256, 276);
            this.CompileItems.TabIndex = 2;
            this.CompileItems.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.CompileItems_AfterSelect);
            // 
            // panel3
            // 
            this.panel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel3.Controls.Add(this.MoveDown);
            this.panel3.Controls.Add(this.MoveUp);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(256, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(126, 276);
            this.panel3.TabIndex = 1;
            // 
            // MoveDown
            // 
            this.MoveDown.Enabled = false;
            this.MoveDown.Location = new System.Drawing.Point(26, 67);
            this.MoveDown.Name = "MoveDown";
            this.MoveDown.Size = new System.Drawing.Size(75, 23);
            this.MoveDown.TabIndex = 1;
            this.MoveDown.Text = "Move Down";
            this.MoveDown.UseVisualStyleBackColor = true;
            this.MoveDown.Click += new System.EventHandler(this.MoveDown_Click);
            // 
            // MoveUp
            // 
            this.MoveUp.Enabled = false;
            this.MoveUp.Location = new System.Drawing.Point(26, 25);
            this.MoveUp.Name = "MoveUp";
            this.MoveUp.Size = new System.Drawing.Size(75, 23);
            this.MoveUp.TabIndex = 0;
            this.MoveUp.Text = "Move Up";
            this.MoveUp.UseVisualStyleBackColor = true;
            this.MoveUp.Click += new System.EventHandler(this.MoveUp_Click);
            // 
            // compileItemMenu
            // 
            this.compileItemMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addDependencyToolStripMenuItem});
            this.compileItemMenu.Name = "compileItemMenu";
            this.compileItemMenu.Size = new System.Drawing.Size(172, 26);
            this.compileItemMenu.Click += new System.EventHandler(this.compileItemMenu_Click);
            // 
            // addDependencyToolStripMenuItem
            // 
            this.addDependencyToolStripMenuItem.Name = "addDependencyToolStripMenuItem";
            this.addDependencyToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.addDependencyToolStripMenuItem.Text = "Edit Dependencies";
            // 
            // CompileOrderViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "CompileOrderViewer";
            this.Size = new System.Drawing.Size(382, 311);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.compileItemMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TreeView CompileItems;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button MoveDown;
        private System.Windows.Forms.Button MoveUp;
        private System.Windows.Forms.ContextMenuStrip compileItemMenu;
        private System.Windows.Forms.ToolStripMenuItem addDependencyToolStripMenuItem;



    }
}
