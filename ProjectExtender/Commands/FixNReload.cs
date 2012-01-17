using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace FSharp.ProjectExtender.Commands
{
    public class FixNReload : OleMenuCommand
    {
        public FixNReload()
            : base(Execute, new CommandID(Constants.guidProjectExtenderCmdSet, (int)Constants.cmdidProjectFixNReload))
        {
            BeforeQueryStatus += QueryStatus;
        }

        void QueryStatus(object sender, EventArgs e)
        {
            var fixer = new ProjectFixerXml(GlobalServices.get_current_project_stub());
            Visible = fixer.IsExtenderProject && fixer.NeedsFixing;
        }

        private static void Execute(object sender, EventArgs e)
        {
            var fixer = new ProjectFixerXml(GlobalServices.get_current_project_stub());
            if (fixer.IsExtenderProject && fixer.NeedsFixing)
                fixer.FixupProject();

            var cmdsetid = Constants.guidStandardCommandSet97;
            GlobalServices.Shell.PostExecCommand(ref cmdsetid, (uint)VSConstants.VSStd97CmdID.ReloadProject, 0,
                                                 null);
        }
    }
}
