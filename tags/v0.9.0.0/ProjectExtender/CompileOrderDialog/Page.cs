using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System.Drawing;
using Microsoft.VisualStudio.Shell.Interop;

//using NativeMethods = Hill30Inc.ProjectExtender.ProjectBase.NativeMethods;
using MSProject = Microsoft.Build.BuildEngine.Project;

namespace FSharp.ProjectExtender
{
    [CLSCompliant(false), ComVisible(true), Guid(Constants.guidCompilerOrderPageString)]
    public class Page : IPropertyPage, IVsHierarchyEvents
    {

        CompileOrderViewer control;
        bool dirty = false;
        uint eventCookie;
        IVsHierarchy item;

        protected bool IsDirty
        {
            get
            {
                return this.dirty;
            }
            set
            {
                if (this.dirty != value)
                {
                    this.dirty = value;
                    if (this.site != null)
                        site.OnStatusChange((uint)(this.dirty ? StructuresEnums.PropPageStatus.Dirty : StructuresEnums.PropPageStatus.Clean));
                }
            }
        }

        #region IPropertyPage Members

        public void Activate(IntPtr parent, RECT[] pRect, int bModal)
        {
            if (this.control == null)
            {
                this.control = new CompileOrderViewer(((IProjectManager)item));
                this.control.Size = new Size(pRect[0].right - pRect[0].left, pRect[0].bottom - pRect[0].top);
                this.control.Visible = false;
                this.control.Size = new Size(550, 300);
                this.control.CreateControl();
                NativeMethods.SetParent(this.control.Handle, parent);
                this.control.OnPageUpdated += (sender, args) => IsDirty = true;
            }
        }

        public int Apply()
        {
            IsDirty = false;
            return VSConstants.S_OK;
        }

        public void Deactivate()
        {
            if (null != this.control)
            {
                this.control.Dispose();
                this.control = null;
            }
        }

        public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
        {
            PROPPAGEINFO info = new PROPPAGEINFO();

            info.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
            info.dwHelpContext = 0;
            info.pszDocString = null;
            info.pszHelpFile = null;
            info.pszTitle = Constants.CompileOrder;
            info.SIZE.cx = 550;
            info.SIZE.cy = 300;
            pPageInfo[0] = info;
        }

        public void Help(string pszHelpDir)
        {
        }

        public int IsPageDirty()
        {
            // Note this returns an HRESULT not a Bool.
            return (dirty ? (int)VSConstants.S_OK : (int)VSConstants.S_FALSE);
        }

        public void Move(RECT[] pRect)
        {
            RECT r = pRect[0];
            this.control.Location = new Point(r.left, r.top);
            this.control.Size = new Size(r.right - r.left, r.bottom - r.top);
        }

        public void SetObjects(uint count, object[] ppunk)
        {
            if (count > 0)
                if (ppunk[0] is IVsBrowseObject)
                    try
                    {
                        uint itemId;
                        ErrorHandler.ThrowOnFailure((ppunk[0] as IVsBrowseObject).GetProjectItem(out item, out itemId));
                        if (itemId != VSConstants.VSITEMID_ROOT)
                            throw new ArgumentException("Set Object should be given the root hierarchy");

                        string name;
                        ErrorHandler.ThrowOnFailure(item.GetCanonicalName(VSConstants.VSITEMID_ROOT, out name));
                        item.AdviseHierarchyEvents(this, out eventCookie);
                        return;
                    }
                    catch (Exception)
                    {
                        if (item != null)
                            item.UnadviseHierarchyEvents(eventCookie);
                        item = null;
                        throw;
                    }

            // if we could not get our hands on the project let us clear whatever we already have there
            if (item != null)
                item.UnadviseHierarchyEvents(eventCookie);
            item = null;
        }

        IPropertyPageSite site;
        public void SetPageSite(IPropertyPageSite pPageSite)
        {
            this.site = pPageSite;
        }

        public void Show(uint nCmdShow)
        {
            this.control.Visible = true; // TODO: pass SW_SHOW* flags through      
            this.control.Show();
        }

        public int TranslateAccelerator(MSG[] pMsg)
        {
            MSG msg = pMsg[0];

            if ((msg.message < NativeMethods.WM_KEYFIRST || msg.message > NativeMethods.WM_KEYLAST) && (msg.message < NativeMethods.WM_MOUSEFIRST || msg.message > NativeMethods.WM_MOUSELAST))
                return 1;

            return (NativeMethods.IsDialogMessageA(this.control.Handle, ref msg)) ? 0 : 1;
        }

        #endregion

        #region IVsHierarchyEvents Members

        int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon)
        {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent)
        {
            control.refresh_file_list();
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
        {
            // OnItemsAppended will be also called so there is no need to do refresh here 
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemDeleted(uint itemid)
        {
            // OnInvalidateItems will be also called so there is no need to do refresh here 
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemsAppended(uint itemidParent)
        {
            control.refresh_file_list();
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
