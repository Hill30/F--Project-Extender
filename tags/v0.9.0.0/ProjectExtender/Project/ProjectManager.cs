using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.ComponentModel.Design;
using Microsoft.Build.BuildEngine;
using System.IO;
using FSharp.ProjectExtender.Project;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using ShellConstants = Microsoft.VisualStudio.Shell.Interop.Constants;

namespace FSharp.ProjectExtender
{
    /// <summary>
    /// Represents the root node of the extended project node tree
    /// </summary>
    [ComVisible(true)]
    public class ProjectManager : FlavoredProjectBase, IProjectManager, IOleCommandTarget, IVsTrackProjectDocumentsEvents2
    {

        uint hierarchy_event_cookie = (uint)ShellConstants.VSCOOKIE_NIL;
        uint document_tracker_cookie = (uint)ShellConstants.VSCOOKIE_NIL;
        ItemList itemList;
        Microsoft.VisualStudio.FSharp.ProjectSystem.ProjectNode FSProjectManager;
        IOleCommandTarget innerTarget;
        IVsProject innerProject;
        bool show_all = false;
        bool renaimng_in_progress = false;
        bool exclude_in_progress = false;

        public ProjectManager()
            : base()
        { }

        public bool ExcludeInProgress { get { return exclude_in_progress; } }

        /// <summary>
        /// Sets the service provider from which to access the services. 
        /// </summary>
        /// <param name="site">An instance to an Microsoft.VisualStudio.OLE.Interop object</param>
        /// <returns>A success or failure value.</returns>
        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider site)
        {
            serviceProvider = new ServiceProvider(site);
            return VSConstants.S_OK;
        }

        #region overridden Flavored Project methods
        /// <summary>
        /// Completes the project manager initialization process
        /// </summary>
        protected override void OnAggregationComplete()
        {
            base.OnAggregationComplete();

            FSProjectManager = GlobalServices.getFSharpProjectNode(innerProject);
            BuildManager = new MSBuildManager(FSProjectManager.BuildProject);

            itemList = new ItemList(this);
            hierarchy_event_cookie = AdviseHierarchyEvents(itemList);
            ErrorHandler.ThrowOnFailure(GlobalServices.documentTracker.AdviseTrackProjectDocumentsEvents(this, out document_tracker_cookie));
        }

        /// <summary>
        /// Modify the command execution process for the commands routed to the IVsUIHierarchy
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        protected override int ExecCommand(uint itemId, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Run commands on Excluded nodes (if any) first
            int result;
            if (itemList.ExecCommand(itemId, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, out result))
                return result;

            // It is not enough to hide the F# project commands in the context menu - the can come from keyboard as well
            // disable the FSharp project commands on the file nodes (moveup movedown, add above, add below)
            if (pguidCmdGroup.Equals(Constants.guidFSharpProjectCmdSet))
                return VSConstants.S_OK;

            // the rest should be processed by the base project manager
            return base.ExecCommand(itemId, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// Modify how properties are calculated
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="propId"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected override int GetProperty(uint itemId, int propId, out object property)
        {
            if (itemId != VSConstants.VSITEMID_ROOT && itemId >= ItemList.ExcludedNodeStart)
                // It is a Excluded node - itemList will take care of it
                return itemList.GetProperty(itemId, propId, out property);

            // we should not interfere with F# project system when renaiming is in progress
            // we will get back to (our) normal when it is completed
            if (!renaimng_in_progress)
                switch ((__VSHPROPID)propId)
                {
                    case __VSHPROPID.VSHPROPID_FirstChild:
                    case __VSHPROPID.VSHPROPID_FirstVisibleChild:
                        return itemList.GetFirstChild(itemId, out property);
                    case __VSHPROPID.VSHPROPID_NextSibling:
                    case __VSHPROPID.VSHPROPID_NextVisibleSibling:
                        return itemList.GetNextSibling(itemId, out property);
                    default:
                        break;
                }

            int result = base.GetProperty(itemId, propId, out property);
            if (result != VSConstants.S_OK)
                return result;

            // Adjust the results returned by the base project
            if (itemId == VSConstants.VSITEMID_ROOT)
            {
                switch ((__VSHPROPID2)propId)
                {
                    case __VSHPROPID2.VSHPROPID_PropertyPagesCLSIDList:
                        {
                            //Add the CompileOrder property page.
                            var properties = new List<string>(property.ToString().Split(';'));
                            properties.Add(typeof(Page).GUID.ToString("B"));
                            property = properties.Aggregate("", (a, next) => a + ';' + next).Substring(1);
                            return VSConstants.S_OK;
                        }
                    case __VSHPROPID2.VSHPROPID_PriorityPropertyPagesCLSIDList:
                        {
                            // set the order for the project property pages
                            var properties = new List<string>(property.ToString().Split(';'));
                            properties.Insert(1, typeof(Page).GUID.ToString("B"));
                            property = properties.Aggregate("", (a, next) => a + ';' + next).Substring(1);
                            return VSConstants.S_OK;
                        }
                    default:
                        break;
                }
            }
            return result;
        }

        protected override void SetInnerProject(IntPtr innerIUnknown)
        {
            base.SetInnerProject(innerIUnknown);
            // is the innerIUnknown Addref'ed before it is passed here?
            // should I Release it after I cast it to objects? I am not sure
            innerTarget = (IOleCommandTarget)Marshal.GetObjectForIUnknown(innerIUnknown);
            innerProject = (IVsProject)innerTarget;
        }

        protected override void Close()
        {
            if (hierarchy_event_cookie != (uint)ShellConstants.VSCOOKIE_NIL)
                UnadviseHierarchyEvents(hierarchy_event_cookie);
            if (document_tracker_cookie != (uint)ShellConstants.VSCOOKIE_NIL)
                GlobalServices.documentTracker.UnadviseTrackProjectDocumentsEvents(document_tracker_cookie);
            base.Close();
        }

        #endregion

        /// <summary>
        /// Returns the first child for the node as provided by base F# project manager
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal uint GetNodeChild(uint itemId)
        {
            object result;
            ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_FirstChild, out result));
            return (uint)(int)result;
        }

        /// <summary>
        /// Returns the next sibling for the node as provided by base F# project manager
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal uint GetNodeSibling(uint itemId)
        {
            object result;
            ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_NextSibling, out result));
            return (uint)(int)result;
        }

        /// <summary>
        /// Gets the specified metadata element for a given build item
        /// </summary>
        /// <param name="itemId">ID for the item</param>
        /// <param name="property">metadata element name</param>
        /// <returns>metadata element value</returns>
        internal string GetMetadata(uint itemId, string property)
        {
            object browseObject;
            ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out browseObject));
            return (string)browseObject.GetType().GetMethod("GetMetadata").Invoke(browseObject, new object[] { property });
        }

        /// <summary>
        /// Sets the specified metadata element for a given build item
        /// </summary>
        /// <param name="itemId">ID for the item</param>
        /// <param name="property">metadata element name</param>
        /// <param name="value">new element value</param>
        internal void SetMetadata(uint itemId, string property, string value)
        {
            object browseObject;
            ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out browseObject));
            browseObject.GetType().GetMethod("SetMetadata").Invoke(browseObject, new object[] { property, value });
        }

        internal void InvalidateParentItems(IEnumerable<uint> itemIds)
        {
            var updates = new Dictionary<Microsoft.VisualStudio.FSharp.ProjectSystem.HierarchyNode, Microsoft.VisualStudio.FSharp.ProjectSystem.HierarchyNode>(); 
            foreach (var itemId in itemIds)
            {
                var hierarchyNode = FSProjectManager.NodeFromItemId(itemId);
                updates[hierarchyNode.Parent] = hierarchyNode;
            }

            uint lastItemId = VSConstants.VSITEMID_NIL;
            foreach (var item in updates)
            {
                item.Value.OnInvalidateItems(item.Key);
                lastItemId = item.Value.ID;
            }

            if (lastItemId != VSConstants.VSITEMID_NIL)
                GlobalServices.solutionExplorer.ExpandItem(this, lastItemId, EXPANDFLAGS.EXPF_SelectItem);

        }

        private void InvalidateParentItems(string[] oldFileNames, string[] newFileNames)
        {
            int pfFound;
            VSDOCUMENTPRIORITY[] pdwPriority = new VSDOCUMENTPRIORITY[1];
            uint pItemid;
            List<uint> itemIds = new List<uint>();
            for (int i = 0; i < newFileNames.Length; i++)
            {
                if (Path.GetFileName(newFileNames[i]) == Path.GetFileName(oldFileNames[i]))
                    continue;
                ErrorHandler.ThrowOnFailure(innerProject.IsDocumentInProject(newFileNames[i], out pfFound, pdwPriority, out pItemid));
                if (pfFound == 0)
                    continue;
                itemIds.Add(pItemid);
            }
            InvalidateParentItems(itemIds);
        }

        /// <summary>
        /// Refreshes the solution explorer tree
        /// </summary>
        /// <param name="nodes">A list of nodes which were originally selected</param>
        /// <remarks>
        /// Refreshing the tree cancels the selection the nodes list is used to restore the
        /// selection. The items on the list could have been changed/ recreated as a side
        /// effect of the operation, so the list of the nodes is re-mapped
        /// </remarks>
        public void RefreshSolutionExplorer(IEnumerable<ItemNode> nodes)
        {
            // as kludgy as it looks this is the only way I found to force the
            // refresh of the solution explorer window
            FSProjectManager.FirstChild.OnInvalidateItems(FSProjectManager);

            bool first = true;
            foreach (var node in itemList.RemapNodes(nodes))
                if (first)
                {
                    GlobalServices.solutionExplorer.ExpandItem(this, node.ItemId, EXPANDFLAGS.EXPF_SelectItem);
                    first = false;
                }
                else
                    GlobalServices.solutionExplorer.ExpandItem(this, node.ItemId, EXPANDFLAGS.EXPF_AddSelectItem);
        }

        /// <summary>
        /// Adds an existing file to the project in response to the "Include In Project" command
        /// </summary>
        /// <param name="parentID"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        internal int AddFileItem(uint parentID, string Path)
        {
            Microsoft.VisualStudio.FSharp.ProjectSystem.HierarchyNode parent;
            if (parentID == VSConstants.VSITEMID_ROOT)
                parent = FSProjectManager;
            else
                parent = FSProjectManager.NodeFromItemId(parentID);

            var node = FSProjectManager.AddNewFileNodeToHierarchy(parent, Path);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Adds an existing folder to the project in response to the "Include In Project" command
        /// </summary>
        /// <param name="parentID"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        internal uint AddFolderItem(string Path)
        {
            return FSProjectManager.CreateFolderNodes(Path).ID;
        }

        #region IProjectManager Members

        public MSBuildManager BuildManager { get; private set; }
        
        /// <summary>
        /// In response to click on the Show All Files button changes the state of the button
        /// </summary>
        public void FlipShowAll()
        {
            show_all = !show_all;
            itemList.SetShowAll(show_all);
            RefreshSolutionExplorer(itemList.GetSelectedNodes());
        }

        /// <summary>
        /// Refershes the solution explorer to reflect the up-to-date excluded files
        /// </summary>
        public void Refresh()
        {
            if (show_all)
            {
                itemList.SetShowAll(show_all);
                RefreshSolutionExplorer(itemList.GetSelectedNodes());
            }
        }

        #endregion

        #region IOleCommandTarget Members

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            List<ItemNode> excludedNodes = null;
            if (pguidCmdGroup.Equals(Constants.guidStandardCommandSet2K) && nCmdID == (uint)VSConstants.VSStd2KCmdID.EXCLUDEFROMPROJECT && show_all)
            {
                exclude_in_progress = true;
                excludedNodes = itemList.GetSelectedNodes();
            }

            int result = innerTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            exclude_in_progress = false;

            if (excludedNodes != null)
                RefreshSolutionExplorer(excludedNodes);

            // In certain situations the F# project manager throws an exception while adding files
            // to subdirectories. We are lucky that this is happening after all the job of adding the file
            // to the project is completed. Whereas we are handling the file ordering ourselves anyway
            // all we need to do is to supress the error message
            if ((uint)result == 0x80131509) // Invalid Operation Exception
            {
                System.Diagnostics.Debug.Write("\n***** Supressing COM exception *****\n");
                System.Diagnostics.Debug.Write(Marshal.GetExceptionForHR(result));
                System.Diagnostics.Debug.Write("\n***** Supressed *****\n");
                return VSConstants.S_OK;
            }
            if ((uint)result == 0x80004003) // Null Pointer Exception
            {
                System.Diagnostics.Debug.Write("\n***** Supressing COM exception *****\n");
                System.Diagnostics.Debug.Write(Marshal.GetExceptionForHR(result));
                System.Diagnostics.Debug.Write("\n***** Supressed *****\n");
                return VSConstants.S_OK;
            }
            return result;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {

            if (pguidCmdGroup.Equals(Constants.guidProjectExtenderCmdSet) && prgCmds[0].cmdID == (uint)Constants.cmdidProjectShowAll)
            {
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                if (show_all)
                    prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                return VSConstants.S_OK;
            }

            int result = innerTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            if (result != VSConstants.S_OK)
                return result;

            // hide the FSharp project commands on the file nodes (moveup movedown, add above, add below)
            if (pguidCmdGroup.Equals(Constants.guidFSharpProjectCmdSet))
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_INVISIBLE;

            // show the Add new folder command on the project node
            if (pguidCmdGroup.Equals(Constants.guidStandardCommandSet97) && prgCmds[0].cmdID == (uint)VSConstants.VSStd97CmdID.NewFolder)
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;

            return VSConstants.S_OK;
        }

        #endregion

        #region IVsTrackProjectDocumentsEvents2 Members

        public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            renaimng_in_progress = false;
            return VSConstants.S_OK;
        }

        public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            renaimng_in_progress = false;
            InvalidateParentItems(rgszMkOldNames, rgszMkNewNames);
            return VSConstants.S_OK;
        }

        public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            renaimng_in_progress = true;
            return VSConstants.S_OK;
        }

        public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            renaimng_in_progress = true;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
