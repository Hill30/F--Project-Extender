using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.FSharp.ProjectSystem;
using Microsoft.VisualStudio;
using BuildProject = Microsoft.Build.BuildEngine.Project;
using System.Reflection;
using Microsoft.Build.BuildEngine;

namespace FSharp.ProjectExtender.Project
{
    class ProjectNodeProxy
    {
        ProjectNode projectNode;
        public ProjectNodeProxy(IVsProject innerProject)
        {
            projectNode = GlobalServices.getFSharpProjectNode(innerProject);
            BuildProject = projectNode.BuildProject;
        }

        public BuildProject BuildProject { get; private set; }

        ///// <summary>
        ///// Gets the specified metadata element for a given build item
        ///// </summary>
        ///// <param name="itemId">ID for the item</param>
        ///// <param name="property">metadata element name</param>
        ///// <returns>metadata element value</returns>
        //internal string GetMetadata(uint itemId, string property)
        //{
        //    object browseObject;
        //    ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out browseObject));
        //    return (string)browseObject.GetType().GetMethod("GetMetadata").Invoke(browseObject, new object[] { property });
        //}

        ///// <summary>
        ///// Sets the specified metadata element for a given build item
        ///// </summary>
        ///// <param name="itemId">ID for the item</param>
        ///// <param name="property">metadata element name</param>
        ///// <param name="value">new element value</param>
        //internal void SetMetadata(uint itemId, string property, string value)
        //{
        //    object browseObject;
        //    ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out browseObject));
        //    browseObject.GetType().GetMethod("SetMetadata").Invoke(browseObject, new object[] { property, value });
        //}

        /// <summary>
        /// Invalidates the solution explorer by calling OnInvalidateItems 
        /// For parents of every node on the list
        /// </summary>
        /// <param name="itemIds">list of nodes impacted</param>
        /// <returns></returns>
        internal uint InvalidateParentItems(IEnumerable<uint> itemIds)
        {
            var updates = new Dictionary<HierarchyNode, HierarchyNode>();
            foreach (var itemId in itemIds)
            {
                var hierarchyNode = projectNode.NodeFromItemId(itemId);
                updates[hierarchyNode.Parent] = hierarchyNode;
            }

            uint lastItemId = VSConstants.VSITEMID_NIL;
            foreach (var item in updates)
            {
                item.Value.OnInvalidateItems(item.Key);
                lastItemId = item.Value.ID;
            }

            return lastItemId;
        }

        public void RefreshSolutionExplorer()
        {
            // as kludgy as it looks this is the only way I found to force the
            // refresh of the solution explorer window
            projectNode.FirstChild.OnInvalidateItems(projectNode);
        }
        /// <summary>
        /// Adds an existing file to the project in response to the "Include In Project" command
        /// </summary>
        /// <param name="parentID">Hierarchy ItemID for the parent</param>
        /// <param name="Path">absolute path to the file to add</param>
        /// <returns></returns>
        internal int AddFileItem(uint parentID, string Path)
        {
            Microsoft.VisualStudio.FSharp.ProjectSystem.HierarchyNode parent;
            if (parentID == VSConstants.VSITEMID_ROOT)
                parent = projectNode;
            else
                parent = projectNode.NodeFromItemId(parentID);

            var node = projectNode.AddNewFileNodeToHierarchy(parent, Path);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Adds an existing folder to the project in response to the "Include In Project" command
        /// </summary>
        /// <param name="Path">Path to the directory</param>
        /// <returns>ItemID for the Hierarchy created for the folder</returns>
        internal uint AddFolderItem(string Path)
        {
            return projectNode.CreateFolderNodes(Path).ID;
        }

        /// <summary>
        /// Moves the build item for a given file node in the specified direction
        /// </summary>
        /// <param name="itemNode"></param>
        /// <param name="direction"></param>
        internal void Move(ItemNode itemNode, ItemNode.Direction direction)
        {
            var node = projectNode.NodeFromItemId(itemNode.ItemId);
            node.GetType().InvokeMember(
                (direction == ItemNode.Direction.Down) ? "MoveDown" : "MoveUp",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod,
                null, null, new object[] {node, projectNode});
        }

        internal BuildItem GetBuildItem(ShadowFileNode shadowFileNode)
        {
            var node = projectNode.NodeFromItemId(shadowFileNode.ItemId);
            var node_property = node.GetType().GetProperty("ItemNode", BindingFlags.Instance | BindingFlags.NonPublic);
            var itemNode = node_property.GetValue(node, new object[] { });
            var build_item_property = itemNode.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
            return (BuildItem)build_item_property.GetValue(itemNode, new object[] { });
        }
    }
}
