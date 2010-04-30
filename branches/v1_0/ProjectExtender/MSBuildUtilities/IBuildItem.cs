using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSharp.ProjectExtender.Project
{
    public interface IBuildItem
    {
        string Include { get; }
        string Name { get; }
        string GetMetadata(string p);
        void SwapWith(IBuildItem iBuildItemProxy);
        void RemoveMetadata(string p);
        void SetMetadata(string p, string p_2);
    }
}
