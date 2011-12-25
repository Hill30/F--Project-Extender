using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer
{
    class ProjectFixerXml : ProjectFixerBase
    {
        private readonly XmlDocument projectFile = new XmlDocument();
        private readonly XmlNamespaceManager nsmgr;
        public ProjectFixerXml(string path)
        {
            projectFile.Load(path);
            nsmgr = new XmlNamespaceManager(projectFile.NameTable);
            if (projectFile.DocumentElement != null) 
                nsmgr.AddNamespace("x", projectFile.DocumentElement.NamespaceURI);
        }

        public bool IsExtenderProject
        {
            get
            {
                var typeGuids = projectFile.SelectSingleNode("x:Project/x:PropertyGroup/x:ProjectTypeGuids", nsmgr);
                return 
                    typeGuids != null 
                    && 
                        typeGuids.InnerText.ToUpper().Split(';').Contains('{' + Constants.guidProjectExtenderFactoryString.ToUpper() + '}');
            }
        }

        public bool NeedsFixing
        {
            get
            {
                return true;
            }
        }

        public override IEnumerator<Project.IBuildItem> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal void Fixup()
        {
            //throw new NotImplementedException();
        }
    }
}
