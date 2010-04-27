using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FSharp.ProjectExtender
{
    static class GlobalServices
    {
        public static readonly IVsMonitorSelection selectionMonitor = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

        public static readonly IVsTrackProjectDocuments2 documentTracker = (IVsTrackProjectDocuments2)Package.GetGlobalService(typeof(SVsTrackProjectDocuments));

        public static readonly IVsUIHierarchyWindow solutionExplorer = getSolutionExplorer();

        public static readonly IVsUIShell shell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));

        public static readonly IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));

        public static readonly EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(SDTE));

        /// <summary>
        /// retrieves the IVsProject interface for currentll selected project
        /// </summary>
        /// <returns></returns>
        public static IVsProject get_current_project()
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC;
            IVsProject result = null;
            ErrorHandler.ThrowOnFailure(selectionTracker.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC));
            if (!IntPtr.Zero.Equals(ppHier))
            {
                result = Marshal.GetObjectForIUnknown(ppHier) as IVsProject;
                Marshal.Release(ppHier);
            }
            if (!IntPtr.Zero.Equals(ppSC))
                Marshal.Release(ppSC);
            return result;
        }

        /// <summary>
        /// Retrieves the FSharp project node from the IVsProject interface
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static Microsoft.VisualStudio.FSharp.ProjectSystem.ProjectNode getFSharpProjectNode(IVsProject root)
        {
            IOLEServiceProvider sp;
            ErrorHandler.ThrowOnFailure(root.GetItemContext(VSConstants.VSITEMID_ROOT, out sp));

            IntPtr objPtr = IntPtr.Zero;
            try
            {
                Guid hierGuid = typeof(VSLangProj.VSProject).GUID;
                Guid UNKguid = NativeMethods.IID_IUnknown;
                ErrorHandler.ThrowOnFailure(sp.QueryService(ref hierGuid, ref UNKguid, out objPtr));

                var OAVSProject = (VSLangProj.VSProject)Marshal.GetObjectForIUnknown(objPtr);
                var OAProject = (Microsoft.VisualStudio.FSharp.ProjectSystem.Automation.OAProject)OAVSProject.Project;
                return OAProject.Project;
            }
            finally
            {
                if (objPtr != IntPtr.Zero)
                    Marshal.Release(objPtr);
            }
        }


        /// <summary>
        /// Verifies that two objects represent the same instance of a COM object.
        /// This essentially compares the IUnkown pointers of the 2 objects.
        /// This is needed in scenario where aggregation is involved.
        /// </summary>
        /// <param name="obj1">Can be an object, interface or IntPtr</param>
        /// <param name="obj2">Can be an object, interface or IntPtr</param>
        /// <returns>True if the 2 items represent the same thing</returns>
        public static bool IsSameComObject(object obj1, object obj2)
        {
            IntPtr unknown1 = IntPtr.Zero;
            IntPtr unknown2 = IntPtr.Zero;
            try
            {
                // If we have 2 null, then they are not COM objects and as such "it's not the same COM object"
                if (obj1 != null && obj2 != null)
                {
                    unknown1 = QueryInterfaceIUnknown(obj1);
                    unknown2 = QueryInterfaceIUnknown(obj2);

                    return IntPtr.Equals(unknown1, unknown2);
                }
                return false;
            }
            finally
            {
                if (unknown1 != IntPtr.Zero)
                    Marshal.Release(unknown1);

                if (unknown2 != IntPtr.Zero)
                    Marshal.Release(unknown2);

            }
        }

        /// <summary>
        /// Retrieve the IUnknown for the managed or COM object passed in.
        /// </summary>
        /// <param name="objToQuery">Managed or COM object.</param>
        /// <returns>Pointer to the IUnknown interface of the object.</returns>
        internal static IntPtr QueryInterfaceIUnknown(object objToQuery)
        {
            bool releaseIt = false;
            IntPtr unknown = IntPtr.Zero;
            IntPtr result;
            try
            {
                if (objToQuery is IntPtr)
                {
                    unknown = (IntPtr)objToQuery;
                }
                else
                {
                    // This is a managed object (or RCW)
                    unknown = Marshal.GetIUnknownForObject(objToQuery);
                    releaseIt = true;
                }

                // We might already have an IUnknown, but if this is an aggregated
                // object, it may not be THE IUnknown until we QI for it.				
                Guid IID_IUnknown = VSConstants.IID_IUnknown;
                ErrorHandler.ThrowOnFailure(Marshal.QueryInterface(unknown, ref IID_IUnknown, out result));
            }
            finally
            {
                if (releaseIt && unknown != IntPtr.Zero)
                {
                    Marshal.Release(unknown);
                }

            }

            return result;
        }

        private static IVsTrackSelectionEx selectionTracker = get_selectionTracker();

        private static IVsTrackSelectionEx get_selectionTracker()
        {
            IVsTrackSelectionEx result;
            ErrorHandler.ThrowOnFailure(((IVsMonitorSelection2)selectionMonitor).GetEmptySelectionContext(out result));
            return result;
        }

        /// <summary>
        /// returns a pointer to the solution explorer window. Used to intialize the static pointer
        /// </summary>
        /// <returns></returns>
        private static IVsUIHierarchyWindow getSolutionExplorer()
        {
            IVsUIShell shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

            object pvar = null;
            IVsWindowFrame frame = null;
            Guid persistenceSlot = new Guid(EnvDTE.Constants.vsWindowKindSolutionExplorer);

            ErrorHandler.ThrowOnFailure(shell.FindToolWindow(0, ref persistenceSlot, out frame));
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));

            return (IVsUIHierarchyWindow)pvar;
        }

    }
}
