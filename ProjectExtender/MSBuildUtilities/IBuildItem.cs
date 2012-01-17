using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSharp.ProjectExtender.Project
{
    public interface IBuildItem
    {
        /// <summary>
        /// The value of the Include attribute of the corresponding project element 
        /// </summary>
        string Include { get; }

        /// <summary>
        /// Build Item type - i.e. Compile, Content, etc.
        /// </summary>
        string Type { get; }
        
        /// <summary>
        /// <b>this</b> and <b>target</b> trade places in the project file 
        /// </summary>
        /// <param name="target"></param>
        void SwapWith(IBuildItem target);

        /// <summary>
        /// gets the value of a metadata element
        /// </summary>
        /// <param name="name">element name</param>
        /// <returns>element value</returns>
        string GetMetadata(string name);
        
        /// <summary>
        /// removes a metadata element
        /// </summary>
        /// <param name="name">element name</param>
        void RemoveMetadata(string name);
        
        /// <summary>
        /// sets a value of a metadata element
        /// </summary>
        /// <param name="name">element name</param>
        /// <param name="value">new element value</param>
        void SetMetadata(string name, string value);
    }
}
