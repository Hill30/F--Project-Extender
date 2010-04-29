using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;

namespace FSharp.ProjectExtender.Project
{
    class ShadowFileNode : ItemNode
    {
        public ShadowFileNode(ItemList items, ItemNode parent, uint itemId, string path)
            : base(items, parent, itemId, Constants.ItemNodeType.PhysicalFile, path)
        {
            buildItem = Items.Project.ProjectProxy.GetBuildItem(this);
        }
        BuildItemProxy buildItem;

        protected override string SortOrder
        {
            get { return "e"; }
        }

        public override BuildItemProxy BuildItem
        {
            get
            {
                return buildItem;
            }
        }
    }
}
