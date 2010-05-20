using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FSharp.ProjectExtender.Project.Excluded
{
    [ComVisible(true)]
    public class ExcludedFileNode : ExcludedNode
    {
        public ExcludedFileNode(ItemList items, ItemNode parent, string path)
            : base(items, parent, Constants.ItemNodeType.ExcludedFile, path)
        { }

        protected override string SortOrder
        {
            get { return "e"; }
        }

        protected override int ImageIndex
        {
            get { return (int)Constants.ImageName.ExcludedFile; }
        }

        protected override int IncludeItem()
        {
            return Items.IncludeFileItem(this);
        }

    }
}
