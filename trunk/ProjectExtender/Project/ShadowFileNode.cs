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

            //buildItem.Include for linked files OnAddFile is different from the one after save,
            //so it is better to keep canonical names too and associate 'Include' value with unique canonical name
            buildItem = Items.Project.ProjectProxy.GetBuildItem(this);
            //Admit 'Include' values for files with the same name in different folders are different-there will be no override 
            Items.Project.ProjectProxy.IncludeToCanonical[buildItem.Include] = path;
        }
        IBuildItem buildItem;

        protected override string SortOrder
        {
            get { return "e"; }
        }
  
        internal void SwapWith(ShadowFileNode target)
        {
            this.buildItem.SwapWith(target.buildItem);
        }

        internal string GetDependencies()
        {
            return buildItem.GetMetadata(Constants.DependsOn);
        }

        internal void UpdateDependencies(List<ShadowFileNode> dependencies)
        {
            if (dependencies.Count == 0)
                buildItem.RemoveMetadata(Constants.DependsOn);
            else
                buildItem.SetMetadata(Constants.DependsOn, dependencies.ConvertAll(elem => elem.buildItem.ToString()).Aggregate("", (a, item) => a + ',' + item).Substring(1));
        }

        public enum Direction { Up, Down }

    }
}
