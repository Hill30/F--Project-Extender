using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace FSharp.ProjectExtender.Project.Excluded
{
    [ComVisible(true)]
    public class ExcludedFolderNode : ExcludedNode
    {
        public ExcludedFolderNode(ItemList items, ItemNode parent, string path)
            : base(items, parent, Constants.ItemNodeType.ExcludedFile, path)
        {
            SetShowAll(true);
        }

        internal override void SetShowAll(bool show_all)
        {
            if (show_all && Directory.Exists(Path))
            {
                foreach (var file in Directory.GetFiles(Path))
                {
                    if (ChildExists("e;" + file))
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
                foreach (var child in this)
                    child.SetShowAll(show_all);
            }
        }

        protected override string SortOrder
        {
            get { return "d"; }
        }

        protected override int ImageIndex
        {
            get { return (int)Constants.ImageName.ExcludedFolder; }
        }

        protected override int IncludeItem()
        {
            return Items.IncludeFolderItem(this);
        }

        internal override int GetProperty(int propId, out object property)
        {
            switch ((__VSHPROPID)propId)
            {

                case __VSHPROPID.VSHPROPID_Expandable:
                    property = true;
                    return VSConstants.S_OK;

                case __VSHPROPID.VSHPROPID_OpenFolderIconIndex:
                    property = ImageIndex + 1;
                    return VSConstants.S_OK;

                case __VSHPROPID.VSHPROPID_Caption:
                    property = System.IO.Path.GetFileName(Path.Substring(0, Path.Length-1));
                    return VSConstants.S_OK;

                default:
                    break;
            }
            return base.GetProperty(propId, out property);
        }
    }
}
