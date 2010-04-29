using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using FSharp.ProjectExtender.Project;
using Microsoft.Build.BuildEngine;

namespace FSharp.ProjectExtender
{
    public partial class CompileOrderViewer : UserControl
    {
        IProjectManager project;
        public CompileOrderViewer(IProjectManager project)
        {
            this.project = project;
            InitializeComponent();
            refresh_file_list();
            var service = (ProjectManager)GetService(typeof(ProjectManager));
        }

        public event EventHandler OnPageUpdated;

        void project_OnProjectModified(object sender, EventArgs e)
        {
            refresh_file_list();
        }
        public TreeView CompileItemsTree
        {
            get { return CompileItems; }
        }
        public void refresh_file_list()
        {
            CompileItems.Nodes.Clear();
            foreach (BuildItemProxy element in project.BuildItems)
            {
                if (element.Name != "Compile")
                    continue;
                TreeNode compileItem = new TreeNode(element.Include);
                compileItem.Tag = project.Items[project.ProjectDir + "\\" + element.Include];
                //compileItem.ContextMenuStrip = compileItemMenu;
                BuildDependencies(compileItem);
                CompileItems.Nodes.Add(compileItem);
            }
        }

        private void BuildDependencies(TreeNode node)
        {
            node.Nodes.Clear();
            string dependencies = ((ItemNode)node.Tag).GetDependencies(); 
            if (dependencies != null)
                foreach (var d in dependencies.Split(','))
                    if (d != "")
                        node.Nodes.Add(d);
        }

        private void CompileItems_AfterSelect(object sender, TreeViewEventArgs e)
        {
            MoveUp.Enabled = false;
            MoveDown.Enabled = false;
            if (e.Node.Level == 0 && CompileItems.SelectedNode != null)
            {
                MoveUp.Enabled = CompileItems.Nodes.IndexOf(e.Node) > 0;
                MoveDown.Enabled = CompileItems.Nodes.IndexOf(e.Node) < CompileItems.Nodes.Count - 1;
            }
        }

        private void compileItemMenu_Click(object sender, EventArgs e)
        {
            EditDependenciesDialog addForm = new EditDependenciesDialog();
            var origin = CompileItems.HitTest(((MouseEventArgs)e).Location);
            if (origin.Node == null)
                return;
            foreach (TreeNode n in CompileItems.Nodes)
            {
                if (origin.Node != n)
                    addForm.Dependencies.Items.Add(n.Tag);
                if (((ItemNode)origin.Node.Tag).GetDependencies().IndexOf(n.Tag.ToString()) >= 0)
                    addForm.Dependencies.SetItemChecked(addForm.Dependencies.Items.Count - 1, true);
            }
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                List<ItemNode> dependencies = new List<ItemNode>();
                foreach (ItemNode item in addForm.Dependencies.CheckedItems)
                    dependencies.Add(item);

                ((ItemNode)origin.Node.Tag).UpdateDependencies(dependencies);
                BuildDependencies(origin.Node);
            }
            addForm.Dispose();
        }

        private void MoveUp_Click(object sender, EventArgs e)
        {
            if (CompileItems.SelectedNode != null)
                MoveElement(CompileItems.SelectedNode, ItemNode.Direction.Up);
        }


        private void MoveDown_Click(object sender, EventArgs e)
        {
            if (CompileItems.SelectedNode != null)
                MoveElement(CompileItems.SelectedNode, ItemNode.Direction.Down);
        }

        /// <summary>
        /// Moves a compile item in the compilation order list one position up or down
        /// </summary>
        /// <param name="n">item to move</param>
        /// <param name="dir">direction</param>
        private void MoveElement(TreeNode n, ItemNode.Direction dir)
        {
            if (!CompileItems.Nodes.Contains(n))
                return;

            // Calculate the node's new location
            int new_index = 0;
            switch (dir)
            {
                case ItemNode.Direction.Up:
                    if (n.Index <= 0) // already at the top - nowehere to go up
                        return;
                    new_index = n.Index - 1;
                    break;
                case ItemNode.Direction.Down:
                    if (n.Index >= CompileItems.Nodes.Count - 1) // already at the bottom - nowehere to go down
                        return;
                    new_index = n.Index + 1;
                    break;
            }
            if (OnPageUpdated != null)
                OnPageUpdated(this, EventArgs.Empty);

            //((BuildElement)n.Tag).SwapWith((BuildElement)CompileItems.Nodes[new_index].Tag);
            ((Project.ItemNode)n.Tag).Move(dir);

            // Update the UI (the TreeView)
            CompileItems.Nodes.Remove(n);
            CompileItems.Nodes.Insert(new_index, n);
            CompileItems.SelectedNode = n;
        }

    }
}
