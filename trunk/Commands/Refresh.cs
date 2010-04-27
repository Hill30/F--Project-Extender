using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace FSharp.ProjectExtender.Commands
{
    public class Refresh : OleMenuCommand
    {
        public Refresh()
            : base(Execute, new CommandID(Constants.guidProjectExtenderCmdSet, (int)Constants.cmdidProjectRefresh))
        {
            BeforeQueryStatus += new EventHandler(QueryStatus);
        }

        void QueryStatus(object sender, EventArgs e)
        {
            Visible = GlobalServices.get_current_project() is IProjectManager;
        }

        private static void Execute(object sender, EventArgs e)
        {
            var project = GlobalServices.get_current_project();
            if (project != null)
            {
                ((IProjectManager)project).Refresh();
            }
        }

    }
}
