using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.FSharp.ProjectSystem;
using Microsoft.VisualStudio;
using BuildProject = Microsoft.Build.BuildEngine.Project;
using System.Reflection;
using System.IO;

namespace FSharp.ProjectExtender.Project
{
    class BuildProjectProxy : IEnumerable<IBuildItem>
    {
        
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
            public T1 Element { get { return element; } }
            public T2 MoveBy { get { return moveBy; } }
            public T3 Index { get { return index; } }
        }
        
        ProjectNode projectNode;
        public Dictionary<string, string> IncludeToCanonical { get; set; }
        public BuildProjectProxy(IVsProject innerProject)
        {
            projectNode = GlobalServices.getFSharpProjectNode(innerProject);
            IncludeToCanonical = new Dictionary<string, string>();
            var item_list = new List<IBuildItem>();
            var fixup_list = new List<Tuple<IBuildItem, int, int>>();

            foreach (var item in this)
            {
                item_list.Add(item);
                switch (item.Type)
                {
                    case "Compile":
                    case "Content":
                    case "None":
                        int offset;
                        if (int.TryParse(item.GetMetadata(Constants.moveByTag), out offset))
                            fixup_list.Insert(0, new Tuple<IBuildItem, int, int>(item, offset, item_list.Count - 1));
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in fixup_list)
            {
                for (int i = 1; i <= item.MoveBy; i++)
                    item.Element.SwapWith(item_list[item.Index + i]);
                item_list.Remove(item.Element);
                item_list.Insert(item.Index + item.MoveBy, item.Element);
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

        internal IBuildItem GetBuildItem(ShadowFileNode shadowFileNode)
        {
            var node = projectNode.NodeFromItemId(shadowFileNode.ItemId);
            var node_property = node.GetType().GetProperty("ItemNode", BindingFlags.Instance | BindingFlags.NonPublic);
            var itemNode = node_property.GetValue(node, new object[] { });
            var build_item_property = itemNode.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
            return new BuildItemProxy(build_item_property.GetValue(itemNode, new object[] { }));
        }
        /// <summary>
        /// Adjusts the positions of build elements to ensure the project can be loaded by the FSharp project system
        /// </summary>
        internal void FixupProject()
        {

            var fixup_dictionary = new Dictionary<string, int>();
            var fixup_list = new List<Tuple<IBuildItem, int, int>>();
            var itemList = new List<IBuildItem>();
            int count = 0;

            foreach (var item in this.Where(
                    n => n.Type == "Compile" || n.Type == "Content" || n.Type == "None"
                    ))
            {
                item.RemoveMetadata(Constants.moveByTag);
                itemList.Add(item);
                count++;
                string path = Path.GetDirectoryName(item.Include);
                //if the item is root level item - think as if it is a folder
                if (String.Compare(path, "") == 0)
                    path = item.Include;
                string partial_path = path;
                int location;
                while (true)
                {
                    // The partial path was already encountered in the project file
                    if (fixup_dictionary.TryGetValue(partial_path, out location))
                    {
                        int offset = count - 1 - location; // we need to move it up in the build file by this many slots

                        // if offset = 0 this item does not have to be moved
                        if (offset > 0)
                        {
                            item.SetMetadata(Constants.moveByTag, offset.ToString());

                            // add the item to the fixup list
                            fixup_list.Add(new Tuple<IBuildItem, int, int>(item, offset, count - 1));

                            // increment item positions in the fixup dictionary to reflect 
                            // change in their position caused by an element inserted in front of them
                            foreach (var d_item in fixup_dictionary.ToList())
                            {
                                if (d_item.Value > location)
                                    fixup_dictionary[d_item.Key] += 1;
                            }
                        }
                        break;
                    }
                    var ndx = partial_path.LastIndexOf('\\');
                    if (ndx < 0)
                    {
                        location = count - 1;  // this is a brand new path - let us put it in the bottom
                        break;
                    }
                    // Move one step up in the item directory path
                    partial_path = partial_path.Substring(0, ndx);
                }
                partial_path = path;

                // update the fixup dictionary to reflect the positions of the paths we encountered so far
                while (true)
                {
                    fixup_dictionary[partial_path] = location + 1; // the index for the slot to put the next item in
                    var ndx = partial_path.LastIndexOf('\\');
                    if (ndx < 0)
                        break;
                    partial_path = partial_path.Substring(0, ndx);
                }
            }
            foreach (var item in fixup_list)
            {
                for (int i = 1; i <= item.MoveBy; i++)
                    item.Element.SwapWith(itemList[item.Index - i]);
                itemList.Remove(item.Element);
                itemList.Insert(item.Index - item.MoveBy, item.Element);
            }
#if VS2008
            projectNode.BuildProject.Save(projectNode.BuildProject.FullFileName);
#elif VS2010
            projectNode.BuildProject.Save();
#endif
        }
        public IEnumerator<IBuildItem> GetEnumerator()
        {
#if VS2008
            foreach (Microsoft.Build.BuildEngine.BuildItemGroup group in projectNode.BuildProject.ItemGroups)
                foreach (var item in group.ToArray())
                    yield return new BuildItemProxy(item);
#elif VS2010
            foreach (var item in projectNode.BuildProject.Xml.Items)
                yield return new BuildItemProxy(item);
#endif
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
