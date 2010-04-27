using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using System.Xml;
using System.Reflection;

namespace FSharp.ProjectExtender
{
    /// <summary>
    /// Represents an element of the MSBuild project file. 
    /// Keep in mind that as elements are added/removed the Build Element can loose its synchronization
    /// with the build file - the BuildItem can be moved to a different BuildGroup or its index can be changed
    /// </summary>
    public class BuildElement
    {
        private BuildItemGroup buildItemGroup;
        int index;
        public BuildElement(BuildItemGroup BuildItemGroup, int index, BuildItem BuildItem)
        {
            this.BuildItem = BuildItem;
            this.index = index;
            this.buildItemGroup = BuildItemGroup;
        }
        public BuildItem BuildItem { get; private set; }

        /// <summary>
        /// Overrides the standard ToString() method to give the path value as the result. Used in the EditDependencie dialog
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return BuildItem.Include;
        }

        public string Path { get { return BuildItem.Include; } }

        internal string GetDependencies()
        {
            return BuildItem.GetMetadata(Constants.DependsOn);
        }

        internal void UpdateDependencies(List<BuildElement> dependencies)
        {
            if (dependencies.Count == 0)
                BuildItem.RemoveMetadata(Constants.DependsOn);
            else
                BuildItem.SetMetadata(Constants.DependsOn, dependencies.ConvertAll(elem => elem.ToString()).Aggregate("", (a, item) => a + ',' + item).Substring(1));
        }

        internal void SwapWith(BuildElement target)
        {
            int this_index = index;
            BuildItemGroup this_group = buildItemGroup;
            MoveTo(target.buildItemGroup, target.index);
            target.MoveTo(this_group, this_index);
        }

        private void MoveTo(BuildItemGroup targetGroup, int index)
        {
            buildItemGroup.RemoveItem(BuildItem);
            AddItemAt(targetGroup, index);
            buildItemGroup = targetGroup;
            this.index = index;
        }

        // The code below is ripped off from the FSharp project system

        /// <summary>
        /// Adds an existing BuildItem to a BuildItemGroup at given location
        /// </summary>
        /// <param name="big"></param>
        /// <param name="itemToAdd"></param>
        /// <param name="index"></param>
        private void AddItemAt(BuildItemGroup big, int index)
        {
            XmlNode node;
            XmlElement element = (XmlElement)typeof(BuildItemGroup)
                .InvokeMember("get_ItemGroupElement", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, big, new object[] { });
            if (big.Count > 0)
            {
                XmlElement element2;
                if (index == big.Count)
                {
                    element2 = ItemElement(big[big.Count - 1]);
                    node = ((XmlElement)element2.ParentNode).InsertAfter(ItemElement(BuildItem), element2);
                }
                else
                {
                    element2 = ItemElement(big[index]);
                    node = ((XmlElement)element2.ParentNode).InsertBefore(ItemElement(BuildItem), element2);
                }
            }
            else
            {
                node = element.AppendChild(ItemElement(BuildItem));
            }
            object[] args = new object[] { index, BuildItem };
            object obj2 = typeof(BuildItemGroup)
                .InvokeMember("AddExistingItemAt", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, big, args);
        }

        private static XmlElement ItemElement(BuildItem bi)
        {
            return (XmlElement)typeof(BuildItem).
                InvokeMember("get_ItemElement", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, bi, new object[] { });
        }
    }

}
