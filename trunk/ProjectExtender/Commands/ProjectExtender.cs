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
        private const string disable_warning = "For projects with subdirectories disabling extender can produce a project file incompatible with the F# project system.\n Press OK to proceed or Cancel to cancel";

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
                ErrorHandler.ThrowOnFailure(GlobalServices.shell.ShowMessageBox(0, ref guid,
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
        private static void ModifyProject(IVsProject vsProject, Action<XmlDocument> effector)
        {
            var project = GlobalServices.getFSharpProjectNode(vsProject);
            var MSBuildProject = project.BuildProject;

            // method get_XmlDocument on the MSBuild project is internal
            // We will have to use reflection to call it
            var minfo = typeof(Microsoft.Build.BuildEngine.Project)
                .GetMethod("get_XmlDocument", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // apply modifications to XML
            effector((XmlDocument)minfo.Invoke(MSBuildProject, new object[] { }));

            // Set dirty flag to true to force project save
            project.SetProjectFileDirty(true);

            // Unload the project - also saves the modifications
            ErrorHandler.ThrowOnFailure(GlobalServices.solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, project, 0));

            // Reload the project
            GlobalServices.dte.ExecuteCommand("Project.ReloadProject", "");
        }

        private const string msBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private static XmlNamespaceManager namespace_manager = NamespaceManager();

        private static XmlNamespaceManager NamespaceManager()
        {
            var result = new XmlNamespaceManager(new NameTable());
            result.AddNamespace("default", msBuildNamespace);
            return result;
        }

        /// <summary>
        /// Modifies the XML to enable the extender
        /// </summary>
        /// <param name="project"></param>
        private static void enable_extender(XmlDocument project)
        {
            // Locate the ProjectTypeGuids node
            var projectTypeGuids = project.SelectSingleNode("//default:Project/default:PropertyGroup/default:ProjectTypeGuids", namespace_manager);
            if (projectTypeGuids == null)
            {
                // Not found - create a new one
                projectTypeGuids = project.CreateElement("ProjectTypeGuids", msBuildNamespace);
                var projectGuid = project.SelectSingleNode("//default:Project/default:PropertyGroup/default:ProjectGuid", namespace_manager);
                // insert it after the ProjectGuid node
                projectGuid.ParentNode.InsertAfter(projectTypeGuids, projectGuid);
                // initialize the project type guid list
                projectTypeGuids.InnerText = "{" + Constants.guidProjectExtenderFactoryString + "};{" + Constants.guidFSharpProjectString + "}";
            }
            else
            {
                // parse the existing guid list
                var types = new List<string>(projectTypeGuids.InnerText.Split(';'));

                // prepend the guid list with the extender project type 
                types.Insert(0, '{' + Constants.guidProjectExtenderFactoryString + '}');

                // format the guid list
                var typestring = "";
                types.ForEach(type => typestring += ';' + type);
                // replace the guid list
                projectTypeGuids.InnerText = typestring.Substring(1);
            }
        }

        /// <summary>
        /// Modifies XML to disable extender
        /// </summary>
        /// <param name="project"></param>
        private static void disable_extender(XmlDocument project)
        {
            // locate the ProjectTypeGuids node
            var projectTypeGuids = project.SelectSingleNode("//default:Project/default:PropertyGroup/default:ProjectTypeGuids", namespace_manager);
            // remove the extender guid from the list
            if (projectTypeGuids != null)
                projectTypeGuids.InnerText =
                    projectTypeGuids.InnerText.Replace('{' + Constants.guidProjectExtenderFactoryString + "};", "");
        }
    }
}
