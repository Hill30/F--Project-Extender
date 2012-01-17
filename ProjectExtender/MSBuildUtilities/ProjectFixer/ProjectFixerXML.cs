using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FSharp.ProjectExtender.Project;

namespace FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer
{
    class ProjectFixerXml : ProjectFixerBase
    {
        private readonly XmlDocument projectFile = new XmlDocument();
        private readonly XmlNamespaceManager nsmgr;
        private readonly string path;
        private bool needsFixing;
        public ProjectFixerXml(string path)
        {
            this.path = path;
            projectFile.PreserveWhitespace = true;
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
                base.FixupProject();
                return needsFixing;
            }
        }

        class XmlItem : IBuildItem
        {
            private readonly ProjectFixerXml parent;
            private readonly XmlElement node;

            public XmlItem(ProjectFixerXml parent, XmlElement node)
            {
                this.parent = parent;
                this.node = node;
            }

            #region IBuildItem Members

            public string Include
            {
                get
                {
                    var include = node.Attributes["Include"];
                    return include == null ? null : include.InnerText;
                }
            }

            public string Type
            {
                get { return node.LocalName; }
            }

            public void SwapWith(IBuildItem target)
            {
                var t = (XmlItem) target;
                var targetLocation = t.node.PreviousSibling;
                var targetParent = t.node.ParentNode;
                var mylocation = node.PreviousSibling;
                var myParent = node.ParentNode;
// ReSharper disable PossibleNullReferenceException
                myParent.InsertAfter(t.node, mylocation);
                targetParent.InsertAfter(node, targetLocation);
// ReSharper restore PossibleNullReferenceException
                parent.needsFixing = true;
            }

            public string GetMetadata(string name)
            {
                var meta = node[name];
                if (meta == null)
                    return null;
                return meta.InnerText;
            }

            public void RemoveMetadata(string name)
            {
                var meta = node[name];
                if (meta != null)
                    node.RemoveChild(meta);
            }

            public void SetMetadata(string name, string value)
            {
                var meta = node[name];
                if (meta == null)
                {
// ReSharper disable PossibleNullReferenceException
                    meta = node.OwnerDocument.CreateElement(name, node.NamespaceURI);
// ReSharper restore PossibleNullReferenceException
                    node.AppendChild(meta);
                }
                meta.InnerXml = value;
            }

            #endregion
        }

        public override IEnumerator<IBuildItem> GetEnumerator()
        {
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable PossibleNullReferenceException
            foreach (var node in projectFile.SelectNodes("x:Project/x:ItemGroup/x:Compile|x:Project/x:ItemGroup/x:None|x:Project/x:ItemGroup/x:Content", nsmgr))
// ReSharper restore PossibleNullReferenceException
// ReSharper restore LoopCanBeConvertedToQuery
                yield return new XmlItem(this, (XmlElement)node);
        }

        internal override void FixupProject()
        {
            projectFile.Save(path);
        }
    }
}
