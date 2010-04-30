using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;

namespace FSharp.ProjectExtender.Project
{
    
    /// <summary>
    /// This class is used to switch between BuildItem and ProjectItem types 
    /// depending on whether the package is build for VS2008 or VS2010
    /// </summary>
    public class BuildItemProxy : IBuildItem
    {

        public string Include { get { return instance.Include; } }

        public string Name { get { return instance.ItemType; } }

        public string GetMetadata(string name)
        {
            var metadata = instance.Metadata.FirstOrDefault(item => item.Name == name);
            if (metadata == null)
                return null;
            return metadata.Value;
        }

        public void RemoveMetadata(string name)
        {
            var metadata = instance.Metadata.FirstOrDefault(item => item.Name == name);
            if (metadata != null)
                instance.RemoveChild(metadata);
        }

        public void SetMetadata(string name, string value)
        {
            RemoveMetadata(name);
            instance.AddMetadata(name, value);
        }

        void IBuildItem.SwapWith(IBuildItem iTarget)
        {
            var anchor = instance.ContainingProject.CreateItemElement("Anchor", Guid.NewGuid().ToString());
            instance.Parent.InsertAfterChild(anchor, instance);
            instance.Parent.RemoveChild(instance);

            var target = (BuildItemProxy)iTarget;
            target.instance.Parent.InsertAfterChild(instance, target.instance);
            target.instance.Parent.RemoveChild(target.instance);
            anchor.Parent.InsertAfterChild(target.instance, anchor);
            anchor.Parent.RemoveChild(anchor);
        }

        internal BuildItemProxy(ProjectItemElement instance)
        {
            this.instance = instance;
        }

        internal BuildItemProxy(object bi)
        {
            instance = ((Microsoft.Build.Evaluation.ProjectItem)bi).Xml;
        }

        public ProjectItemElement instance;

    }
}
