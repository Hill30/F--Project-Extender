using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSharp.ProjectExtender;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using FSharp.ProjectExtender.Project;

namespace IntegrationTests
{
    public enum Move 
    { Up, Down }

    public struct Action
    {
        public Action(int index, Move direction)
        {
            this.index = index;
            this.method = "Unknown";
            switch (direction)
            {
                case Move.Up:
                    method = "MoveUp_Click";
                    break;
                case Move.Down:
                    method = "MoveDown_Click";
                    break;
            }
        }
        int index;
        string method;
        public int Index { get { return index; } }
        public string Method { get { return method; } }
    }

    public class UseCase
    {
        List<string> fileList;
        List<Action> actions;
        TestContext ctx;
        string name;
        
        public UseCase(string name, TestContext testContext)
        {
            this.name = name;
            ctx = testContext;
            fileList = new List<string>();
            actions = new List<Action>();
        }

        private UseCase(UseCase parent, IEnumerable<string> filenames)
        {
            name = parent.name;
            ctx = parent.ctx;
            fileList = new List<string>(filenames);
            actions = parent.actions;
        }

        private UseCase(UseCase parent, Action action)
        {
            name = parent.name;
            ctx = parent.ctx;
            fileList = parent.fileList;
            actions = new List<Action>(parent.actions);
            actions.Add(action);
        }

        public UseCase ExpectedOrder(params string[] filenames)
        {
            return new UseCase(this, filenames);
        }
        
        public UseCase Apply(Action action)
        {
            return new UseCase(this, action);
        }

        IVsSolution sln = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution;
        CompileOrderViewer viewer;
        IVsHierarchy hier;

        public void Run()
        {
            Initialize();
            foreach (var action in actions)
            {
                viewer.CompileItemsTree.SelectedNode = viewer.CompileItemsTree.Nodes[action.Index];
                typeof(CompileOrderViewer).InvokeMember(action.Method,
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, viewer,
                    new object[] { viewer, EventArgs.Empty });
            }

            ValidateOrder("Before re-open");

            viewer.Dispose();

            CloseSolution();

            ReopenSolution();

            ValidateOrder("After re-open");

            CloseSolution();
        }

        private void Initialize()
        {
            Assert.IsTrue(File.Exists(ctx.Properties["cleanproj"].ToString()), "Clean project file could not be found");
            File.Delete(ctx.Properties["suo"].ToString());
            File.Copy(ctx.Properties["cleanproj"].ToString(), ctx.Properties["testproj"].ToString(), true);
            sln.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, ctx.Properties["slnfile"].ToString());
            sln.GetProjectOfUniqueName(ctx.Properties["testproj"].ToString(), out hier);
            Assert.IsNotNull(hier, "Failed to open the clean test project");
            viewer = new CompileOrderViewer((IProjectManager)hier);
            Assert.IsNotNull(viewer, "Fail to create Viewer");
        }

        private void CloseSolution()
        {
            sln.CloseSolutionElement
                ((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK, null, 0);
        }

        private void ReopenSolution()
        {
            sln.OpenSolutionFile(
                (uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, ctx.Properties["slnfile"].ToString());
            sln.GetProjectOfUniqueName(ctx.Properties["testproj"].ToString(), out hier);
        }

        private void ValidateOrder(string message)
        {
            var project = hier as IProjectManager;
            Assert.IsNotNull(project, "Project is not an extender project");
            int i = 0;
            foreach (IBuildItem item in project.BuildItems)
            {
                if (item.Type != "Compile")
                    continue;
                Assert.AreEqual(fileList[i], item.Include,
                    message + ": Use case {0} : Invalid build item order at position {1}.", name, i);
                i++;
            }
        }

    }
}
