using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using System.IO;
using FSharp.ProjectExtender.Project.Excluded;

namespace FSharp.ProjectExtender.Project
{
    abstract class ShadowFolderNode : ItemNode
    {
        protected ShadowFolderNode(ItemList items, ItemNode parent, uint itemId, Constants.ItemNodeType type, string path)
            : base(items, parent, itemId, type, path)
        {
            uint child = items.GetNodeFirstChild(itemId);
            while (child != VSConstants.VSITEMID_NIL)
            {
                CreateChildNode(child);
                child = items.GetNodeSibling(child);
            }
            MapChildren();
        }

        internal override void SetShowAll(bool show_all)
        {
            if (show_all && Directory.Exists(Path))
            {
                foreach (var file in Directory.GetFiles(Path))
                {
                    if (ChildExists("e;" + file))
                        continue;
                    if (Items.ToBeHidden(file))
                        continue;
                    if ((new FileInfo(file).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;
                    AddChildNode(new ExcludedFileNode(Items, this, file));
                }
                foreach (var directory in Directory.GetDirectories(Path))
                {
                    if (ChildExists("d;" + directory + '\\'))
                        continue;
                    if ((new DirectoryInfo(directory).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;
                    AddChildNode(new ExcludedFolderNode(Items, this, directory + '\\'));
                }
                foreach (var child in new List<ItemNode>(this))
                {
                    if (child is ExcludedFileNode && !File.Exists(child.Path))
                        child.Delete();
                    if (child is ExcludedFolderNode && !Directory.Exists(child.Path))
                        child.Delete();
                }
                MapChildren();
            }
            else
            {
                foreach (var child in new List<ItemNode>(this))
                    if (child is ExcludedNode)
                        child.Delete();
            }
            foreach (var child in this)
                child.SetShowAll(show_all);
        }

    }

    class PhysicalFolderNode : ShadowFolderNode
    {
        public PhysicalFolderNode(ItemList items, ItemNode parent, uint itemId, string path)
            : base(items, parent, itemId, Constants.ItemNodeType.PhysicalFolder, path)
        { }

        protected override string SortOrder
        {
            get { return "d"; }
        }
    }

    class VirtualFolderNode : ShadowFolderNode
    {
        public VirtualFolderNode(ItemList items, ItemNode parent, uint itemId, string path)
            : base(items, parent, itemId, Constants.ItemNodeType.VirtualFolder, path)
        { }

        protected override string SortOrder
        {
            get { return "c"; }
        }

        internal override void SetShowAll(bool show_all)
        {
        }
    }

    class SubprojectNode : ShadowFolderNode
    {
        public SubprojectNode(ItemList items, ItemNode parent, uint itemId, string path)
            : base(items, parent, itemId, Constants.ItemNodeType.SubProject, path)
        { }

        protected override string SortOrder
        {
            get { return "b"; }
        }
    }
}
