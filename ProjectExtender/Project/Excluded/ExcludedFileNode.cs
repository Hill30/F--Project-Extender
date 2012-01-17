using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace FSharp.ProjectExtender.Project.Excluded
{
    [ComVisible(true)]
    public class ExcludedFileNode : ExcludedNode
    {
        public ExcludedFileNode(ItemList items, ItemNode parent, string path)
            : base(items, parent, Constants.ItemNodeType.ExcludedFile, path)
        { }

        protected override string SortOrder
        {
            get { return "e"; }
        }

        protected override int ImageIndex
        {
            get { return (int)Constants.ImageName.ExcludedFile; }
        }

        protected override bool QueryStatus(Guid cmdGroup, uint cmdID)
        {
            //if (cmdGroup.Equals(Constants.guidStandardCommandSet97) && cmdID == (uint)VSConstants.VSStd97CmdID.Open)
            //    return true;

            return base.QueryStatus(cmdGroup, cmdID);
        }

        protected override int Exec(Guid cmdGroup, uint cmdID)
        {
            if (cmdGroup.Equals(Constants.guidStandardCommandSet2K) && cmdID == (uint)VSConstants.VSStd2KCmdID.INCLUDEINPROJECT)
                return IncludeItem();

            //if (cmdGroup.Equals(Constants.guidStandardCommandSet97) && cmdID == (uint)VSConstants.VSStd97CmdID.Open)
            //    return OpenFile();

            return VSConstants.S_OK;
        }

        private int IncludeItem()
        {
            var result = Items.IncludeFileItem(this);
            Items.Project.RefreshSolutionExplorer(new ItemNode[] { Items[Path] });
            return result;
        }

        private int OpenFile()
        {
            IVsHierarchy hier;
            uint itemId;
            IntPtr pUnkData;
            uint docCookie;
            var rc = GlobalServices.RDT.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, Path, out hier, out itemId, out pUnkData, out docCookie);
            throw new NotImplementedException();
        }
    }
}
