using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer;
using FSharp.ProjectExtender.Project;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.FSharp.ProjectSystem;
using Microsoft.VisualStudio;
using System.Reflection;

namespace FSharp.ProjectExtender.MSBuildUtilities
{
    class BuildProjectProxy : ProjectFixerBase
    {
        
#if VS2008
        struct Tuple<T1, T2, T3>
        {
            public Tuple(T1 element, T2 moveBy, T3 index)
            {
                this.element = element;
                this.moveBy = moveBy;
                this.index = index;
            }
            T1 element;
            T2 moveBy;
            T3 index;
            public T1 Item1 { get { return element; } }
            public T2 Item2 { get { return moveBy; } }
            public T3 Item3 { get { return index; } }
        }
#endif

        readonly ProjectNode projectNode;
        public Dictionary<string, string> IncludeToCanonical { get; set; }
        public BuildProjectProxy(IVsProject innerProject)
        {
            projectNode = GlobalServices.getFSharpProjectNode(innerProject);
            IncludeToCanonical = new Dictionary<string, string>();
            var itemList = new List<IBuildItem>();
            var fixupList = new List<Tuple<IBuildItem, int, int>>();

            foreach (var item in this)
            {
                itemList.Add(item);
                switch (item.Type)
                {
                    case "Compile":
                    case "Content":
                    case "None":
                        int offset;
                        if (int.TryParse(item.GetMetadata(Constants.moveByTag), out offset))
                            fixupList.Insert(0, new Tuple<IBuildItem, int, int>(item, offset, itemList.Count - 1));
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in fixupList)
            {
                for (int i = 1; i <= item.Item2; i++)
                    item.Item1.SwapWith(itemList[item.Item3 + i]);
                itemList.Remove(item.Item1);
                itemList.Insert(item.Item3 + item.Item2, item.Item1);
            }
        }

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
        /// <param name="parentId">Hierarchy ItemID for the parent</param>
        /// <param name="path">absolute path to the file to add</param>
        /// <returns></returns>
        internal int AddFileItem(uint parentId, string path)
        {
            projectNode.AddNewFileNodeToHierarchy(
                parentId == VSConstants.VSITEMID_ROOT 
                    ? projectNode 
                    : projectNode.NodeFromItemId(parentId)
                , path);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Adds an existing folder to the project in response to the "Include In Project" command
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>ItemID for the Hierarchy created for the folder</returns>
        internal uint AddFolderItem(string path)
        {
            return projectNode.CreateFolderNodes(path).ID;
        }

        internal IBuildItem GetBuildItem(ShadowFileNode shadowFileNode)
        {
            var node = projectNode.NodeFromItemId(shadowFileNode.ItemId);
            var nodeProperty = node.GetType().GetProperty("ItemNode", BindingFlags.Instance | BindingFlags.NonPublic);
            var itemNode = nodeProperty.GetValue(node, new object[] { });
            var buildItemProperty = itemNode.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
            return new BuildItemProxy(buildItemProperty.GetValue(itemNode, new object[] { }));
        }

        internal override void FixupProject()
        {
            base.FixupProject();
#if VS2008
            projectNode.BuildProject.Save(projectNode.BuildProject.FullFileName);
#elif VS2010
            projectNode.BuildProject.Save();
#endif
        }

        override public IEnumerator<IBuildItem> GetEnumerator()
        {
#if VS2008
            foreach (Microsoft.Build.BuildEngine.BuildItemGroup group in projectNode.BuildProject.ItemGroups)
                foreach (var item in group.ToArray())
                    yield return new BuildItemProxy(item);
#elif VS2010
            return 
                projectNode.BuildProject.Xml.Items
                    .Select(item => new BuildItemProxy(item)).Cast<IBuildItem>().GetEnumerator();
#endif
        }

    }
}
