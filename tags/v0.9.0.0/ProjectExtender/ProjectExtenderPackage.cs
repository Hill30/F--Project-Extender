// VsPkg.cs : Implementation of ProjectExtender
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Xml;
using System.Collections.Generic;
using System.IO;

namespace FSharp.ProjectExtender
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the registration utility (regpkg.exe) that this class needs
    // to be registered as package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // A Visual Studio component can be registered under different regitry roots; for instance
    // when you debug your package you want to register it in the experimental hive. This
    // attribute specifies the registry root to use if no one is provided to regpkg.exe with
    // the /root switch.
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(1000, 1)]
    // In order be loaded inside Visual Studio in a machine that has not the VS SDK installed, 
    // package needs to have a valid load key (it can be requested at 
    // http://msdn.microsoft.com/vstudio/extend/). This attributes tells the shell that this 
    // package has a load key embedded in its resources.
    [ProvideLoadKey("Standard", "1.0", "F# Project System Extender", "Hill30 Inc", 100)]
    // Provide the F# project extender project project factory. This is a flavored project and it does not
    // introduce any new templates
    [ProvideProjectFactory(typeof(FSharp.ProjectExtender.Factory), "ProjectExtender", null, null, null, null)]
    // Provide object so it can be created through the ILocalRegistry interface - in this case the new property page
    [ProvideObject(typeof(FSharp.ProjectExtender.Page), RegisterUsing = RegistrationMethod.CodeBase)]

    [Guid(Constants.guidProjectExtenderPkgString)]
    public sealed class ProjectExtenderPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ProjectExtenderPackage() { }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            RegisterProjectFactory(new FSharp.ProjectExtender.Factory(this));
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs.AddCommand(new Commands.ProjectExtender());
            mcs.AddCommand(new Commands.ShowAll());
            mcs.AddCommand(new Commands.Refresh());
        }

        List<IDisposable> handlers = new List<IDisposable>();
        private T RegisterHandler<T>(T handler) where T:IDisposable
        {
            handlers.Add(handler);
            return handler;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var handler in handlers)
                    handler.Dispose();
            base.Dispose(disposing);
        }
    }
}