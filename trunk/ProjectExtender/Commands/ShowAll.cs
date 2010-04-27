using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace FSharp.ProjectExtender.Commands
{
    public class ShowAll : OleMenuCommand
    {

        public ShowAll()
            : base(Execute, new CommandID(Constants.guidProjectExtenderCmdSet, (int)Constants.cmdidProjectShowAll))
        {
            BeforeQueryStatus += new EventHandler(QueryStatus);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void QueryStatus(object sender, EventArgs e)
        {
            Visible = GlobalServices.get_current_project() is IProjectManager;
        }

        private static void Execute(object sender, EventArgs e)
        {
            var project = GlobalServices.get_current_project();
            if (project != null)
                ((IProjectManager)project).FlipShowAll();
        }

    }
}
