using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using System.Xml;
using System.Reflection;
using BuildProject = Microsoft.Build.BuildEngine.Project;

namespace FSharp.ProjectExtender.Project
{
    /// <summary>
    /// This is a VS2008 specific implementation of the IBuildItem interface
    /// </summary>
    public class BuildItemProxy : IBuildItem
    {
        public string Include { get; private set; }
        public string Type { get; private set; }
        
        internal BuildItemProxy(object buildItem)
        {
            instance = (Microsoft.Build.BuildEngine.BuildItem)buildItem;

            // I am not sure what's going on here, but sometimes, in particular when the project is initialized
            // the build item is not what we are getting here, but rather the child element
            // 'get_ParentPersistedItem" gives us what we need
            var persisted_instance = (BuildItem)typeof(BuildItem)
                .InvokeMember("get_ParentPersistedItem", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, instance, new object[] { });
            if (persisted_instance != null)
                instance = persisted_instance;

            buildItemGroup = (BuildItemGroup)typeof(BuildItem)
                .InvokeMember("get_ParentPersistedItemGroup", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, instance, new object[] { });

            int i = -1;
            foreach (BuildItem item in buildItemGroup)
            {
                i++;
                if (item == instance)
                {
                    index = i;
                    break;
                }
            }

            Include = instance.Include;
            Type = instance.Name;
        }

        private BuildItem instance;
        private BuildItemGroup buildItemGroup;
        int index;

        public string GetMetadata(string name)
        {
            return instance.GetMetadata(name);
        }

        public void RemoveMetadata(string name)
        {
            instance.RemoveMetadata(name);
        }

        public void SetMetadata(string name, string value)
        {
            instance.SetMetadata(name,value);
        }

        public void SwapWith(IBuildItem iTarget)
        {
            BuildItemProxy target = (BuildItemProxy)iTarget;
            int this_index = index;
            BuildItemGroup this_group = buildItemGroup;
            MoveTo(target.buildItemGroup, target.index);
            target.MoveTo(this_group, this_index);
        }

        private void MoveTo(BuildItemGroup targetGroup, int index)
        {
            buildItemGroup.RemoveItem(instance);
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
                    node = ((XmlElement)element2.ParentNode).InsertAfter(ItemElement(instance), element2);
                }
                else
                {
                    element2 = ItemElement(big[index]);
                    node = ((XmlElement)element2.ParentNode).InsertBefore(ItemElement(instance), element2);
                }
            }
            else
            {
                node = element.AppendChild(ItemElement(instance));
            }
            object[] args = new object[] { index, instance };
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
