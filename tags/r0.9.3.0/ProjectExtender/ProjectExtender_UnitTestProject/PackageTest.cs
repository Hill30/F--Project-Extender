/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VsSDK.UnitTestLibrary;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FSharp.ProjectExtender;

namespace UnitTestProject
{
    [TestClass()]
    public class PackageTest
    {
        [TestMethod()]
        public void CreateInstance()
        {
            ProjectExtenderPackage package = new ProjectExtenderPackage();
        }

        [TestMethod()]
        public void IsIVsPackage()
        {
            ProjectExtenderPackage package = new ProjectExtenderPackage();
            Assert.IsNotNull(package as IVsPackage, "The object does not implement IVsPackage");
        }

        [TestMethod()]
        public void SetSite()
        {
            // Create the package
            IVsPackage package = new ProjectExtenderPackage() as IVsPackage;
            Assert.IsNotNull(package, "The object does not implement IVsPackage");
            // Create a basic service provider
            OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();
            object mock = Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsRegisterProjectTypes));
            serviceProvider.AddService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsRegisterProjectTypes), mock, true);
            // Site the package
            Assert.AreEqual(0, package.SetSite(serviceProvider), "SetSite did not return S_OK");
            // Unsite the package
            Assert.AreEqual(0, package.SetSite(null), "SetSite(null) did not return S_OK");
        }
    }
}
