using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.ComponentModel.Design;

namespace FSharp.ProjectExtender
{
    [Guid(Constants.guidProjectExtenderFactoryString)]
    public class Factory : FlavoredProjectFactoryBase, IVsSolutionEvents
    {
        private Package package;

        public Factory(Package package)
            : base()
        {
            this.package = package;
            var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out solutionCookie));
        }

        uint solutionCookie;

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            var project = new ProjectManager();
            project.SetSite((IOleServiceProvider)((IServiceProvider)package).GetService(typeof(IOleServiceProvider)));
            return project;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        #region IVsSolutionEvents Members

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            if (pHierarchy is IProjectManager)
                ((IProjectManager)pHierarchy).BuildManager.FixupProject();
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
