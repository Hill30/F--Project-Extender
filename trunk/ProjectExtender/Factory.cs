using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FSharp.ProjectExtender
{
    [Guid(Constants.guidProjectExtenderFactoryString)]
    public class Factory : FlavoredProjectFactoryBase, IVsSolutionEvents
    {
        private readonly Package package;

        public Factory(Package package)
        {
            this.package = package;
            ErrorHandler.ThrowOnFailure(GlobalServices.Solution.AdviseSolutionEvents(this, out solutionCookie));
        }

        protected override void Dispose(bool disposing)
        {
            ErrorHandler.ThrowOnFailure(GlobalServices.Solution.UnadviseSolutionEvents((solutionCookie)));
            base.Dispose(disposing);
        }

        readonly uint solutionCookie;

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            var project = new ProjectManager();
            project.SetSite((IOleServiceProvider)((IServiceProvider)package).GetService(typeof(IOleServiceProvider)));
            return project;
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
            ValidateNReload();
            return VSConstants.S_OK;
        }

        void ValidateNReload()
        {
            var guid = Guid.Empty;
            IEnumHierarchies projects;
            ErrorHandler.ThrowOnFailure(GlobalServices.Solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_UNLOADEDINSOLUTION, ref guid, out projects));
            var hiers = new IVsHierarchy[1];
            while (true)
            {
                uint hiersCount;
                ErrorHandler.ThrowOnFailure(projects.Next((uint)hiers.Length, hiers, out hiersCount));
                if (hiersCount == 0)
                    break;
                ValidateNReload(hiers[0]);
            }
        }


        private const string FixerWarning = "Project {0} ({1}) seems to be a corrupted F# Project Extender project. Do you want F# Project Extender to fix it?";
        private void ValidateNReload(IVsHierarchy project)
        {
            object value;
            ErrorHandler.ThrowOnFailure(project.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_BrowseObject, out value));
            var browseObjType = value.GetType();
            var name = (string)browseObjType.InvokeMember("Name", BindingFlags.GetProperty, null, value, null);
            var projectFile = (string)browseObjType.InvokeMember("ProjectFile", BindingFlags.GetProperty, null, value, null);
            var fixer = new ProjectFixerXml(projectFile);
            if (fixer.IsExtenderProject && fixer.NeedsFixing)
            {
                var guid = Guid.Empty;
                int result;
                ErrorHandler.ThrowOnFailure(GlobalServices.Shell.ShowMessageBox(0, ref guid,
                    null,
                    String.Format(FixerWarning, name, projectFile),
                    null,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND,
                    OLEMSGICON.OLEMSGICON_WARNING,
                    0,
                    out result));
                if (result == NativeMethods.IDOK)
                    fixer.Fixup();
            }
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            var project = pHierarchy as IProjectManager;
            if (project != null)
                project.FixupProject();
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
