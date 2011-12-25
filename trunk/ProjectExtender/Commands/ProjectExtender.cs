using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.ComponentModel.Design;
using System.Xml;
using System.Runtime.InteropServices;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using Microsoft.Build.BuildEngine;
using Microsoft.VisualStudio.FSharp.ProjectSystem;

namespace FSharp.ProjectExtender.Commands
{
    public class ProjectExtender : OleMenuCommand
    {
        public ProjectExtender()
            : base(Execute, new CommandID(Constants.guidProjectExtenderCmdSet, (int)Constants.cmdidProjectExtender))
        {
            BeforeQueryStatus += new EventHandler(QueryStatus);
        }

        private const string enable_extender_text = "Enable F# project extender";
        private const string disable_extender_text = "Disable F# project extender";
        private const string disable_warning = "For projects with subdirectories it may or may not be possible to keep compilation order as defined in the extender.\n\n Press OK to proceed or Cancel to cancel";

        /// <summary>
        /// Modifies caption on the project extender command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void QueryStatus(object sender, EventArgs e)
        {
            if (GlobalServices.get_current_project() is IProjectManager)
                ((OleMenuCommand)sender).Text = disable_extender_text;
            else
                ((OleMenuCommand)sender).Text = enable_extender_text;
        }

        private static void Execute(object sender, EventArgs e)
        {
            var project = GlobalServices.get_current_project();
            if (project is IProjectManager)
            {
                Guid guid = Guid.Empty;
                int result;
                ErrorHandler.ThrowOnFailure(GlobalServices.Shell.ShowMessageBox(0, ref guid,
                    null,
                    disable_warning,
                    null,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND,
                    OLEMSGICON.OLEMSGICON_WARNING,
                    0,
                    out result));
                if (result == NativeMethods.IDOK)
                    ModifyProject(project, disable_extender);
            }
            else
                ModifyProject(project, enable_extender);
        }

        /// <summary>
        /// Modifies the loaded project by changing the project's proj file
        /// </summary>
        /// <param name="vsProject">project to be modified</param>
        /// <param name="effector"></param>
        private static void ModifyProject(IVsProject vsProject, Func<string, string> effector)
        {
            var project = GlobalServices.getFSharpProjectNode(vsProject);

            set_ProjectTypeGuids(
                project, 
                effector(
                    get_ProjectTypeGuids(project)
                    ));

            // Set dirty flag to true to force project save
            project.SetProjectFileDirty(true);

            // Unload the project - also saves the modifications
            ErrorHandler.ThrowOnFailure(GlobalServices.Solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, project, 0));

            // Reload the project
            GlobalServices.DTE.ExecuteCommand("Project.ReloadProject", "");
        }

        private static string get_ProjectTypeGuids(ProjectNode project)
        {
#if VS2008
            foreach (BuildPropertyGroup group in project.BuildProject.PropertyGroups)
            {
                foreach (BuildProperty property in group)
                {
                    if (property.Name == "ProjectTypeGuids")
                    {
                        group.RemoveProperty(property);
                        return property.Value;
                    }
                }
            }
            return null;
#elif VS2010
            var property = project.BuildProject.Properties.FirstOrDefault(prop => prop.Name == "ProjectTypeGuids");
            if (property == null)
                return null;
            return property.EvaluatedValue;
#endif
        }

        private static void set_ProjectTypeGuids(ProjectNode project, string value)
        {
#if VS2008
            foreach (BuildPropertyGroup group in project.BuildProject.PropertyGroups)
            {
                foreach (BuildProperty property in group)
                {
                    if (property.Name == "ProjectGuid")
                    {
                        group.AddNewProperty("ProjectTypeGuids", value);
                        return;
                    }
                }
            }
#elif VS2010
            project.BuildProject.Xml.AddProperty("ProjectTypeGuids", value);
#endif
        }

        /// <summary>
        /// Modifies the XML to enable the extender
        /// </summary>
        /// <param name="project"></param>
        private static string enable_extender(string old_types)
        {
            if (old_types == null)
                return "{" + Constants.guidProjectExtenderFactoryString + "};{" + Constants.guidFSharpProjectString + "}";

            // parse the existing guid list
            var types = new List<string>(old_types.Split(';'));

            // prepend the guid list with the extender project type 
            types.Insert(0, '{' + Constants.guidProjectExtenderFactoryString + '}');

            // format the guid list
            var typestring = "";
            types.ForEach(type => typestring += ';' + type);

            return typestring.Substring(1);
        }

        private static string disable_extender(string old_types)
        {
            return old_types.Replace('{' + Constants.guidProjectExtenderFactoryString + "};", "");
        }

    }
}
