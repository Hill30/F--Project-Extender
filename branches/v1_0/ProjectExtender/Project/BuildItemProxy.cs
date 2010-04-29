#define VS2008
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if VS2008
using Microsoft.Build.BuildEngine;
#elif VS2010
using Microsoft.Build.Evaluation;
#endif
namespace FSharp.ProjectExtender.Project
{
    /// <summary>
    /// This class is used to switch between BuildItem and ProjectItem types 
    /// depending on whether the package is build for VS2008 or VS2010
    /// </summary>
    public class BuildItemProxy
    {
        public object Instance{get; private set;}
        public string Include { get; private set; }
        public string Name { get; private set; }
        
        internal BuildItemProxy(object bi )
        {
#if VS2008
            Instance = bi;
            Include = ((BuildItem)bi).Include;
            Name = ((BuildItem)bi).Name;
#elif VS2010
            Instance = pi;
            Include = ((ProjectItem)pi).EvaluatedInclude;
            Name = ((ProjectItem)pi).ItemType;
#endif

        }
        
        public string GetMetadata(string name)
        {
#if VS2008
            return ((BuildItem)Instance).GetMetadata(name);
#elif VS2010
            ProjectMetadata metadata = ((ProjectItem)Instance).GetMetadata(name);
            return metadata.EvaluatedValue;
#endif
        }
        public void RemoveMetadata(string name)
        {
#if VS2008
            ((BuildItem)Instance).RemoveMetadata(name);
#elif VS2010
            ((ProjectItem)Instance).RemoveMetadata(name);
#endif
        }
        public void SetMetadata(string name, string value)
        {
#if VS2008
            ((BuildItem)Instance).SetMetadata(name,value);
#elif VS2010
            ((ProjectItem)Instance).SetMetadataValue(name, value);
#endif
        }
        public override string ToString()
        {
            return Include;
        }
    }
}
