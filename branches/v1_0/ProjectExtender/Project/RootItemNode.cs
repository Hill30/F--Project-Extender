using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;

namespace FSharp.ProjectExtender.Project
{
    class RootItemNode : ShadowFolderNode
    {
        public RootItemNode(ItemList items, string path)
            : base(items, null, VSConstants.VSITEMID_ROOT, Constants.ItemNodeType.Root, path)
        { }

        protected override string SortOrder
        {
            get { return "a"; }
        }

    }
}
