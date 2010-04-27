using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSharp.ProjectExtender
{
    public static class Constants
    {
        public const string guidProjectExtenderPkgString = "5a8f5a4d-d1eb-403e-85b3-63df607fa07c";
        public const string guidProjectExtenderCmdSetString = "7bc39d92-dded-46ae-b491-075f3ded76aa";
        public static readonly Guid guidProjectExtenderCmdSet = new Guid(guidProjectExtenderCmdSetString);

        public const uint cmdidProjectExtender = 0x2001;
        public const uint cmdidProjectRefresh = 0x2002;
        public const uint cmdidProjectShowAll = 0x2003;

        public const string guidProjectExtenderFactoryString = "5B89FCC2-C9F6-49a8-8F8D-EDDCC3FDC9E9";
        public const string guidCompilerOrderPageString = "A5FC3BBD-8795-42d0-AA55-E65FE992378E";

        public const string Application = "Application";
        public const string RootNamespaceDescription = "RootNamespaceDescription";
        public const string CompileOrder = "Compilation Order";
        public const string DependsOn = "DependsOn";

        public const string guidFSharpProjectString = "f2a71f9b-5d33-465a-a702-920d77279786";
        public static readonly Guid guidFSharpProject = new Guid(guidFSharpProjectString);
        public static readonly Guid guidStandardCommandSet97 = new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819");
        public static readonly Guid guidStandardCommandSet2K = new Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2");
        public static readonly Guid guidFSharpProjectCmdSet = new Guid("75AC5611-A912-4195-8A65-457AE17416FB");

        public const uint cmdidExploreFolderInWindows = 0x663;
        public const uint cmdidNewFolder = 0xf5;

        public enum ImageName
        {
            ExcludedFolder = 9,
            OpenExcludedFolder = 10,
            ExcludedFile = 11,
        }

        public enum ItemNodeType
        {
            Root, // 0
            Reference, // 1
            SubProject, // 2
            VirtualFolder, // 3
            PhysicalFolder, // 4
            PhysicalFile, // 5
            ExcludedFolder, //4
            ExcludedFile,  // 5
            Unknown
        }
    };

}
