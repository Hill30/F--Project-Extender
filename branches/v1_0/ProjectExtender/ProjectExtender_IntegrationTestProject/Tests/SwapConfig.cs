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

namespace IntegrationTests
{
    public struct MoveOp
    {
        public int Index { get; set; }
        public CompileOrderViewer.Direction Dir { get; set; }
    }
    public interface ISwapConfig
    {
        ISwapConfig ExpectedOrder(params string[] filenames);
        ISwapConfig Move(params MoveOp[] moves);
        void Run();
    }
    public class SwapConfig : ISwapConfig
    {
        List<string> fileList;
        List<MoveOp> actions;
        string name;
        TestContext ctx;
        internal SwapConfig(string configName,ref TestContext testContext)
        {
            name = configName;
            ctx = testContext;
            fileList = new List<string>();
            actions = new List<MoveOp>();
        }
        public ISwapConfig ExpectedOrder(params string[] filenames)
        {
            foreach (string item in filenames)
                fileList.Add(item);
            return this;
        }
        public ISwapConfig Move(params MoveOp[] moves)
        {
            foreach (MoveOp move in moves)
                actions.Add(move);
            return this;

        }
        public void Run()
        {
            Initialize();
            CompileOrderViewer viewer;
            viewer = ctx.Properties["viewer"] as CompileOrderViewer;
            foreach (MoveOp move in actions)
                typeof(CompileOrderViewer).InvokeMember("MoveElement",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, viewer, new object[] { viewer.CompileItemsTree.Nodes[move.Index], move.Dir });

            Check_OntheFly();
            Check_Reopen();

        }
        public void NoneChanged()
        {
            Initialize();
            IProjectManager project = ((IProjectManager)ctx.Properties["hierarchy"]);
            project.BuildManager.FixupProject();
            int i = 0;
            foreach (var item in project.BuildManager.GetElements(n => n.Name == "Compile"))
            {
                Assert.AreEqual(fileList[i], item.ToString(),
                    "Test {0} : Compilation order is wrong at {1} position", name, i);
                i++;
            }
            CleanUp();


        }
        private void Check_OntheFly()
        {
            //Check order 1 (Changes to project file On-the-fly)
            IProjectManager project = (IProjectManager)ctx.Properties["hierarchy"];
            int i = 0;
            foreach (var item in project.BuildManager.GetElements(n => n.Name == "Compile" ))
            {
                Assert.AreEqual(fileList[i], item.ToString(),
                    "Test {0} : Compilation order is wrong at {1} position", name, i);
                i++;
            }
            CleanUp();

        }
        private void Check_Reopen()
        {
            //Check order 3 (Reopen project - check changes have been saved correctly)
            IVsSolution sln = (ctx.Properties["solution"] as IVsSolution);
            IVsHierarchy hier;
            sln.OpenSolutionFile(
                (uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, ctx.Properties["slnfile"].ToString());
            sln.GetProjectOfUniqueName(ctx.Properties["testfile"].ToString(), out hier);
            IProjectManager project = (IProjectManager)hier;
            int i = 0;
            
            foreach (var item in project.BuildManager.GetElements(n => n.Name == "Compile" ))
            {
                Assert.AreEqual(item.ToString(), fileList[i],
                    "Test {0} after reopen : Compilation order is wrong at {1} position", name, i);
                i++;
            }

            sln.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK, null, 0);

        }
        private void CleanUp()
        {
            ((CompileOrderViewer)ctx.Properties["viewer"]).Dispose();
            ((IVsSolution)ctx.Properties["solution"]).CloseSolutionElement
                ((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK, null, 0);

        }
        private void Initialize()
        {
            File.Delete(ctx.Properties["suo"].ToString());
            File.Copy(ctx.Properties["projfile"].ToString(), ctx.Properties["testfile"].ToString(), true);
            IVsHierarchy hier;
            IVsSolution sln = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution;
            sln.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, ctx.Properties["slnfile"].ToString());
            sln.GetProjectOfUniqueName(ctx.Properties["testfile"].ToString(), out hier);
            Assert.IsNotNull(hier, "Project is not IProjectManager");
            CompileOrderViewer viewer = new CompileOrderViewer((IProjectManager)hier);
            Assert.IsNotNull(viewer, "Fail to create Viewer");
            ctx.Properties["viewer"] = viewer;
            ctx.Properties["solution"] = sln;
            ctx.Properties["hierarchy"] = hier;
        }

    }
}
