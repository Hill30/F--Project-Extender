using System;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;

namespace FSharp.ProjectExtender.Commands
{
    public class ShowAll : OleMenuCommand
    {

        public ShowAll()
            : base(Execute, new CommandID(Constants.guidProjectExtenderCmdSet, (int)Constants.cmdidProjectShowAll))
        {
            BeforeQueryStatus += QueryStatus;
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
