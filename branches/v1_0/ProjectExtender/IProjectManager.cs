using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using FSharp.ProjectExtender.Project;
using System.Collections;

namespace FSharp.ProjectExtender
{
    [ComVisible(true)]
    public interface IProjectManager
    {
        Project.ItemList Items {get;}
        string ProjectDir { get; set; }

        IEnumerable BuildItems { get; }
        void FlipShowAll();
        void Refresh();
        void FixupProject();
    }
}
