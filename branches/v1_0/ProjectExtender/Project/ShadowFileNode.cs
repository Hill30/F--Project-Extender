using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSharp.ProjectExtender.Project
{
    class ShadowFileNode : ItemNode
    {
        public ShadowFileNode(ItemList items, ItemNode parent, uint itemId, string path)
            : base(items, parent, itemId, Constants.ItemNodeType.PhysicalFile, path)
        {
        }

        protected override string SortOrder
        {
            get { return "e"; }
        }
    }
}
