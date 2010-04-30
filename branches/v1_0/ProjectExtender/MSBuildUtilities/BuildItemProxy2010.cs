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
    public class BuildItemProxy
    {
#if VS2008
        public Microsoft.Build.BuildEngine.BuildItem instance;
#elif VS2010
        public Microsoft.Build.Evaluation.ProjectItem instance;
#endif
        public string Include { get; private set; }
        public string Name { get; private set; }
        
        internal BuildItemProxy(object bi )
        {
#if VS2008
            instance = (Microsoft.Build.BuildEngine.BuildItem)bi;
            Include = instance.Include;
            Name = instance.Name;
#elif VS2010
            instance = (Microsoft.Build.Evaluation.ProjectItem)bi;
            Include = instance.EvaluatedInclude;
            Name = instance.ItemType;
#endif

        }
        
        public string GetMetadata(string name)
        {
#if VS2008
            return instance.GetMetadata(name);
#elif VS2010
            var metadata = instance.GetMetadata(name);
            if (metadata == null)
                return null;
            return metadata.EvaluatedValue;
#endif
        }

        public void RemoveMetadata(string name)
        {
            instance.RemoveMetadata(name);
        }

        public void SetMetadata(string name, string value)
        {
#if VS2008
            instance.SetMetadata(name,value);
#elif VS2010
            instance.SetMetadataValue(name, value);
#endif
        }

        public override string ToString()
        {
            return Include;
        }

#if VS2008
        internal void Move(ItemNode.Direction direction)
        {
            throw new NotImplementedException();
        }
#elif VS2010
        internal void Move(ItemNode.Direction direction)
        {
            var element = instance.Xml;
            var parent = element.Parent;
            ProjectElement reference;
            switch (direction)
            {
                case ItemNode.Direction.Up:
                    reference = element;
                    do
                        reference = reference.PreviousSibling;
                    while (!(reference is ProjectItemElement) || ((ProjectItemElement)reference).ItemType != "Compile");

                    parent.RemoveChild(element);
                    parent.InsertBeforeChild(element, reference);
                    break;
                case ItemNode.Direction.Down:
                    reference = element;
                    do
                        reference = reference.NextSibling;
                    while (!(reference is ProjectItemElement) || ((ProjectItemElement)reference).ItemType != "Compile");

                    parent.RemoveChild(element);
                    parent.InsertAfterChild(element, reference);
                    break;
            }
        }
#endif

    }
}
