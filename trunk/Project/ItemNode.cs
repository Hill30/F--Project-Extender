using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace FSharp.ProjectExtender.Project
{
    public abstract class ItemNode: IEnumerable<ItemNode>
    {
        protected ItemNode(ItemList items, ItemNode parent, uint itemId, Constants.ItemNodeType type, string path)
        {
            Items = items;
            Parent = parent;
            ItemId = itemId;
            Type = type;
            Path = path;
            items.Register(this);
        }

        public ItemNode Parent { get; private set; }
        public uint ItemId { get; private set; }

        protected ItemList Items { get; private set; }
        public Constants.ItemNodeType Type { get; private set; }
        public string Path { get; private set; }
        string sort_key { get { return SortOrder + ';' + Path; } }
        SortedList<string, ItemNode> children = new SortedList<string, ItemNode>();
        Dictionary<uint, int> childrenMap;

        protected void CreateChildNode(uint child)
        {
            AddChildNode(Items.CreateNode(child));
        }

        public void AddChildNode(ItemNode child)
        {
            children.Add(child.sort_key, child);
        }

        protected bool ChildExists(string key)
        {
            return children.ContainsKey(key);
        }

        internal void CreatenMapChildNode(uint itemidAdded)
        {
            CreateChildNode(itemidAdded);
            MapChildren();
        }

        protected abstract string SortOrder { get; }

        protected void MapChildren()
        {
            if (childrenMap == null)
                childrenMap = new Dictionary<uint, int>(children.Count);
            else
                childrenMap.Clear();
            int i = 0;
            foreach (var item in children)
                childrenMap.Add(item.Value.ItemId, i++);
        }

        public uint NextSibling
        {
            get
            {
                if (Parent == null)
                    return VSConstants.VSITEMID_NIL;
                int index = Parent.childrenMap[ItemId];
                if (index + 1 < Parent.children.Count)
                    return Parent.children.Values[index + 1].ItemId;
                return VSConstants.VSITEMID_NIL;
            }
        }

        public uint FirstChild
        {
            get
            {
                if (children.Count == 0)
                    return VSConstants.VSITEMID_NIL;
                return children.Values[0].ItemId;
            }
        }

        internal void Delete()
        {
            Parent.children.RemoveAt(Parent.childrenMap[ItemId]);
            Parent.childrenMap.Remove(ItemId);
            Parent.MapChildren();
            Items.Unregister(ItemId);
            foreach (var child in new List<ItemNode>(this))
                child.Delete();
        }

        internal void Remap()
        {
            Parent.children.RemoveAt(Parent.childrenMap[ItemId]);
            Parent.childrenMap.Remove(ItemId);
            Parent.children.Add(sort_key, this);
            Parent.MapChildren();
        }

        internal virtual void SetShowAll(bool show_all)
        {
        }

        #region IEnumerable<ItemNode> Members

        public IEnumerator<ItemNode> GetEnumerator()
        {
            return children.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return children.Values.GetEnumerator();
        }

        #endregion
    }
}
