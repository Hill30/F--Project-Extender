using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.Build.BuildEngine;
using Microsoft.VisualStudio.Shell.Interop;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;
using FSharp.ProjectExtender.Project.Excluded;
using System.Diagnostics;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;


namespace FSharp.ProjectExtender.Project
{
    /// <summary>
    /// Maintains a list of project items 
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to maintain a shadow list of all project items for the project
    /// When the Solution Explorer displays the project tree it traverses the project using
    /// the IVsHierarchy.GetProperty method. The ProjectExtender redirects the GetProperty method calls to 
    /// provide them in the order defined by ItemList rather than the order of F# Project Manager
    /// </remarks>
    public class ItemList : IVsHierarchyEvents
    {
        ProjectManager project;
        IVsHierarchy root_hierarchy;
        Dictionary<uint, ItemNode> itemMap = new Dictionary<uint, ItemNode>();
        Dictionary<string, ItemNode> itemKeyMap = new Dictionary<string, ItemNode>();
        ItemNode root;
        public const int ExcludedNodeStart = 0x0100000;
        uint nextItemId = ExcludedNodeStart;


        /// <summary>
        /// Initalizes a new instance of the itemlist
        /// </summary>
        /// <param name="project"></param>
        public ItemList(ProjectManager project)
        {
            this.project = project;
            root_hierarchy = (IVsHierarchy)project;
            root = CreateNode(VSConstants.VSITEMID_ROOT);
        }

        public void AddChild(ItemNode itemNode)
        {
            itemNode.Parent.AddChildNode(itemNode);
        }

        /// <summary>
        /// Creates a new (shadow) ItemNode given its itemID.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ItemNode CreateNode(uint itemId)
        {
            object caption;
            string path;
            object parentID;
            ItemNode parent;
            if (itemId == VSConstants.VSITEMID_ROOT)
                parent = null;
            else
            {
                ErrorHandler.ThrowOnFailure(root_hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Parent, out parentID));
                if (!itemMap.TryGetValue((uint)(int)parentID, out parent))
                    throw new Exception("Unexpected node parent for node " + itemId);
            }

            switch (get_node_type(itemId))
            {
                case Constants.ItemNodeType.Root:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(itemId, out path));
                    return new RootItemNode(this, Path.GetDirectoryName(path));
                case Constants.ItemNodeType.PhysicalFolder:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(itemId, out path));
                    return new PhysicalFolderNode(this, parent, itemId, path);
                case Constants.ItemNodeType.VirtualFolder:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Caption, out caption));
                    return new VirtualFolderNode(this, parent, itemId, caption.ToString());
                case Constants.ItemNodeType.SubProject:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(itemId, out path));
                    return new SubprojectNode(this, parent, itemId, path);
                case Constants.ItemNodeType.Reference:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Caption, out caption));
                    return new ShadowFileNode(this, parent, itemId, caption.ToString());
                case Constants.ItemNodeType.PhysicalFile:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(itemId, out path));
                    return new ShadowFileNode(this, parent, itemId, path);
                default:
                    ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(itemId, out path));
                    throw new Exception("Unexpected node type for node " + itemId + "(" + path + ")");
            }
        }

        private Constants.ItemNodeType get_node_type(uint itemId)
        {
            Guid type;
            try
            {
                ErrorHandler.ThrowOnFailure(root_hierarchy.GetGuidProperty(itemId, (int)__VSHPROPID.VSHPROPID_TypeGuid, out type));
            }
            catch (COMException e)
            {
                // FSharp project returns Guid.Empty as the type guid for reference nodes, which causes the WAP to throw an exception
                var pinfo = e.GetType().GetProperty("HResult", BindingFlags.Instance | BindingFlags.NonPublic);
                if ((int)pinfo.GetValue(e, new object[] { }) == VSConstants.DISP_E_MEMBERNOTFOUND)
                    type = Guid.Empty;
                else
                    throw;
            }

            // set the sort order based on the item type
            if (type == Guid.Empty)
                return Constants.ItemNodeType.Reference;
            else if (type == VSConstants.GUID_ItemType_PhysicalFile)
                return Constants.ItemNodeType.PhysicalFile;
            else if (type == VSConstants.GUID_ItemType_PhysicalFolder)
                return Constants.ItemNodeType.PhysicalFolder;
            else if (type == VSConstants.GUID_ItemType_SubProject)
                return Constants.ItemNodeType.SubProject;
            else if (type == VSConstants.GUID_ItemType_VirtualFolder)
                return Constants.ItemNodeType.VirtualFolder;
            else if (type == Constants.guidFSharpProject)
                return Constants.ItemNodeType.Root;
            return Constants.ItemNodeType.Unknown;
        }

        public int GetNextSibling(uint itemId, out object value)
        {
            ItemNode n;
            value = null;
            if (itemMap.TryGetValue(itemId, out n))
            {
                value = n.NextSibling;
                return VSConstants.S_OK;
            }
            else
                return VSConstants.E_INVALIDARG;
        }

        public int GetFirstChild(uint itemId, out object value)
        {
            ItemNode n;
            value = null;
            if (itemMap.TryGetValue(itemId, out n))
            {
                value = n.FirstChild;
                return VSConstants.S_OK;
            }
            else
                return VSConstants.E_INVALIDARG;
        }

        internal uint GetNodeFirstChild(uint itemId)
        {
            return project.GetNodeChild(itemId);
        }

        internal int GetProperty(uint itemId, int propId, out object property)
        {
            ItemNode node;
            if (itemMap.TryGetValue(itemId, out node) && node is ExcludedNode)
                return ((ExcludedNode)node).GetProperty(propId, out property);
            property = null;
            return VSConstants.DISP_E_MEMBERNOTFOUND;
        }

        internal uint GetNextItemID()
        {
            return nextItemId++;
        }

        internal uint GetNodeSibling(uint itemId)
        {
            return project.GetNodeSibling(itemId);
        }

        internal void Register(ItemNode itemNode)
        {
            itemMap.Add(itemNode.ItemId, itemNode);
            // from the standpoint of IVsHierarchy rename is handled by the F# project 
            // as remove/add, therefore item path is never changed during item lifetime
            // and can considered to be immutable
            itemKeyMap.Add(itemNode.Path, itemNode);
        }

        internal void Unregister(uint itemId)
        {
            ItemNode n;
            if (itemMap.TryGetValue(itemId, out n))
            {
                itemMap.Remove(itemId);
                itemKeyMap.Remove(n.Path);
            }
        }

        internal void SetShowAll(bool show_all)
        {
            root.SetShowAll(show_all);
        }

        public virtual bool ToBeHidden(string file)
        {
            string project_file;
            ErrorHandler.ThrowOnFailure(root_hierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out project_file));
            if (file == project_file)
                return true;
            string solution_directory;
            string solution_file;
            string solution_options_file;
            GlobalServices.solution.GetSolutionInfo(out solution_directory, out solution_file, out solution_options_file);
            if (file == solution_file)
                return true;
            if (file == solution_options_file)
                return true;
            return false;
        }

        #region IVsHierarchyEvents Members

        int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon)
        {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent)
        {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
        {
            // for some reason during rename OnItemAdded is called twice - let us ignore the second one
            if (itemMap.ContainsKey(itemidAdded))
                return VSConstants.S_OK;

            ItemNode n;
            if (!itemMap.TryGetValue(itemidParent, out n))
                return VSConstants.E_INVALIDARG;
            n.CreatenMapChildNode(itemidAdded);
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemDeleted(uint itemid)
        {
            ItemNode n;
            if (itemMap.TryGetValue(itemid, out n))
            {
                n.Delete();
                if (project.ExcludeInProgress)
                    n.Parent.SetShowAll(true);
            }
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemsAppended(uint itemidParent)
        {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags)
        {

            if (propid == (int)__VSHPROPID.VSHPROPID_Caption)
            {
                ItemNode n;
                if (!itemMap.TryGetValue(itemid, out n))
                    return VSConstants.E_INVALIDARG;

                n.Remap();
                project.InvalidateParentItems(new uint[] { itemid });
            }
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// Builds a list of nodes currently selectde in the solution explorer
        /// </summary>
        /// <returns></returns>
        public List<ItemNode> GetSelectedNodes()
        {
            var selected_nodes = new List<ItemNode>();
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;
            try
            {
                // Get the current project hierarchy, project item, and selection container for the current selection
                // If the selection spans multiple hierachies, hierarchyPtr is Zero
                uint itemid;
                IVsMultiItemSelect multiItemSelect = null;
                ErrorHandler.ThrowOnFailure(GlobalServices.selectionMonitor.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainer));
                // We only care if there are one ore more nodes selected in the tree
                if (itemid != VSConstants.VSITEMID_NIL && hierarchyPtr != IntPtr.Zero)
                {
                    IVsHierarchy hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;

                    if (itemid != VSConstants.VSITEMID_SELECTION)
                    {
                        // This is a single selection. Compare hirarchy with our hierarchy and get node from itemid
                        ItemNode node;
                        if (GlobalServices.IsSameComObject(project, hierarchy) && itemMap.TryGetValue(itemid, out node))
                        {
                            selected_nodes.Add(node);
                        }
                    }
                    else if (multiItemSelect != null)
                    {
                        // This is a multiple item selection.

                        //Get number of items selected and also determine if the items are located in more than one hierarchy
                        uint numberOfSelectedItems;
                        int isSingleHierarchyInt;
                        ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectionInfo(out numberOfSelectedItems, out isSingleHierarchyInt));
                        bool isSingleHierarchy = (isSingleHierarchyInt != 0);

                        // Now loop all selected items and add to the list only those that are selected within this hierarchy
                        if (!isSingleHierarchy || (isSingleHierarchy && GlobalServices.IsSameComObject(project, hierarchy)))
                        {
                            Debug.Assert(numberOfSelectedItems > 0, "Bad number of selected itemd");
                            VSITEMSELECTION[] vsItemSelections = new VSITEMSELECTION[numberOfSelectedItems];
                            uint flags = (isSingleHierarchy) ? (uint)__VSGSIFLAGS.GSI_fOmitHierPtrs : 0;
                            ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectedItems(flags, numberOfSelectedItems, vsItemSelections));
                            foreach (VSITEMSELECTION vsItemSelection in vsItemSelections)
                            {
                                if (isSingleHierarchy || GlobalServices.IsSameComObject(project, vsItemSelection.pHier))
                                {
                                    ItemNode node;
                                    if (itemMap.TryGetValue(vsItemSelection.itemid, out node))
                                    {
                                        selected_nodes.Add(node);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
                if (selectionContainer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainer);
                }
            }
            return selected_nodes;
        }

        /// <summary>
        /// Shows the context menu on the excluded items
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal bool ExecCommand(uint itemId, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, out int result)
        {
            result = (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
            if (itemId < ItemList.ExcludedNodeStart || itemId == VSConstants.VSITEMID_ROOT || itemId == VSConstants.VSITEMID_SELECTION)
                return false;

            var nodes = GetSelectedNodes();
            if (nodes.Count == 0 || pguidCmdGroup != VsMenus.guidVsUIHierarchyWindowCmds)
                return false;

            switch (nCmdID)
            {
                case (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_RightClick:
                    // The UIHWCMDID_RightClick is what tells an IVsUIHierarchy in a UIHierarchyWindow 
                    // to put up the context menu.  Since the mouse may have moved between the 
                    // mouse down and the mouse up, GetCursorPos won't tell you the right place 
                    // to put the context menu (especially if it came through the keyboard).  
                    // So we pack the proper menu position into pvaIn by
                    // memcpy'ing a POINTS struct into the VT_UI4 part of the pvaIn variant.  The
                    // code to unpack it looks like this:
                    //			ULONG ulPts = V_UI4(pvaIn);
                    //			POINTS pts;
                    //			memcpy((void*)&pts, &ulPts, sizeof(POINTS));
                    // You then pass that POINTS into DisplayContextMenu.

                    object variant = Marshal.GetObjectForNativeVariant(pvaIn);
                    UInt32 pointsAsUint = (UInt32)variant;
                    short x = (short)(pointsAsUint & 0x0000ffff);
                    short y = (short)((pointsAsUint & 0xffff0000) / 0x10000);

                    POINTS[] pnts = new POINTS[1];
                    pnts[0].x = x;
                    pnts[0].y = y;
                    Guid menu = VsMenus.guidSHLMainMenu;// Constants.guidProjectExtenderCmdSet;
                    result = GlobalServices.shell.ShowContextMenu(0, ref menu, VsMenus.IDM_VS_CTXT_ITEMNODE, pnts,
                        (Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget)nodes[0]);
                    return true;
                default:
                    return false;
            }
        }

        internal int IncludeFileItem(ItemNode node)
        {
            node.Delete();
            int result = project.AddFileItem(node.Parent.ItemId, node.Path);
            project.RefreshSolutionExplorer(new ItemNode[] { node });
            return result;
        }

        internal int IncludeFolderItem(ItemNode node)
        {
            var files = new List<string>();
            foreach (var child in node)
            {
                switch (child.Type)
                {
                    case Constants.ItemNodeType.ExcludedFile:
                        files.Add(child.Path);
                        break;
                    case Constants.ItemNodeType.ExcludedFolder:
                        IncludeFolderItem(child);
                        break;
                    default:
                        break;
                }
            }
            node.Delete();
            uint parent = project.AddFolderItem(node.Path);
            foreach (var file in files)
                ErrorHandler.ThrowOnFailure(project.AddFileItem(parent, file));
            project.RefreshSolutionExplorer(new ItemNode[] { node });
            return VSConstants.S_OK;
        }

        internal IEnumerable<ItemNode> RemapNodes(IEnumerable<ItemNode> nodes)
        {
            List<ItemNode> result = new List<ItemNode>();
            foreach (var node in nodes)
            {
                ItemNode newNode;
                if (itemKeyMap.TryGetValue(node.Path, out newNode))
                    result.Add(newNode);
            }
            return result;
        }
    }

}
