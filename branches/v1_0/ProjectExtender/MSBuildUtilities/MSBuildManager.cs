using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using System.IO;
using BuildProject = Microsoft.Build.BuildEngine.Project;

namespace FSharp.ProjectExtender
{
    public class MSBuildManager
    {
        struct Tuple<T1, T2, T3>
        {
            public Tuple(T1 element, T2 moveBy, T3 index)
            {
                this.element = element;
                this.moveBy = moveBy;
                this.index = index;
            }
            T1 element;
            T2 moveBy;
            T3 index;
            public T1 Element { get { return element; } }
            public T2 MoveBy { get { return moveBy; } }
            public T3 Index { get { return index; } }
        }


        private const string moveByTag = "move-by";
        private BuildProject project;

        /// <summary>
        /// Creates an instance of the MSBuildManager. As a part of instance creation moves back project items  
        /// positions of which were adjusted when the project file was saved
        /// </summary>
        /// <param name="projectFile">the name of the project file</param>
        /// <remarks>
        /// When the compilation order is modified through the properties dialog, such modifications are
        /// reflected by changing the order of project elements (BuildItem objects) in the project file.
        /// In certain situations such changes can cause the FSharp base project system to refuse to load the project.
        /// To prevent this from happening MSBuildManager adjusts the positions of offending elements when saving the project
        /// <see cref="M:FixupProject"/> method. After the FSharp base project system is done loading the project these 
        /// elements are moevd back to their original positions
        /// </remarks>
        public MSBuildManager(BuildProject project)
        {
            this.project = project;

            var item_list = new List<BuildElement>();
            var fixup_list = new List<Tuple<BuildElement, int, int>>();

            foreach (var item in GetElements(
                    n => n.Name == "Compile" || n.Name == "Content" || n.Name == "None"
                    ))
            {
                item_list.Add(item);
                int offset;
                if (int.TryParse(item.BuildItem.GetMetadata(moveByTag), out offset))
                    fixup_list.Insert(0, new Tuple<BuildElement,int,int>(item, offset, item_list.Count - 1));
            }

            foreach (var item in fixup_list)
            {
                for (int i = 1; i <= item.MoveBy; i++)
                    item.Element.SwapWith(item_list[item.Index + i]);
                item_list.Remove(item.Element);
                item_list.Insert(item.Index + item.MoveBy, item.Element);
            }
        }

        /// <summary>
        /// Gets an enumarator for all build elements satisfying provided condition
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IEnumerable<BuildElement> GetElements(Predicate<BuildItem> condition)
        {
            foreach (BuildItemGroup group in project.ItemGroups)
            {
                int i = -1;
                foreach (BuildItem item in group)
                {
                    i++;
                    if (condition(item))
                        yield return new BuildElement(group, i, item);
                }
            }
        }

        /// <summary>
        /// Adjusts the positions of build elements to ensure the project can be loaded by the FSharp project system
        /// </summary>
        internal void FixupProject()
        {

            var fixup_dictionary = new Dictionary<string, int>();
            var fixup_list = new List<Tuple<BuildElement, int, int>>();
            var itemList = new List<BuildElement>();
            int count = 0;

            foreach (BuildElement item in GetElements(
                    n => n.Name == "Compile" || n.Name == "Content" || n.Name == "None"
                    ))
            {
                item.BuildItem.RemoveMetadata(moveByTag);
                itemList.Add(item);
                count++;
                string path = Path.GetDirectoryName(item.Path);
                if (String.Compare(path, "") == 0) 
                    path  = item.Path; 
                string partial_path = path;
                int location;
                while (true)
                {
                    // The partial path was already encountered in the project file
                    if (fixup_dictionary.TryGetValue(partial_path, out location))
                    {
                        int offset = count - 1 - location; // we need to move it up in the build file by this many slots

                        // if offset = 0 this item does not have to be moved
                        if (offset > 0)
                        {
                            // we only will need to moev back the items to be compiled - the rest of them will be moved permanently
                            //if (item.BuildItem.Name == "Compile")
                                item.BuildItem.SetMetadata(moveByTag, offset.ToString());

                            // add the item to the fixup list
                            fixup_list.Add(new Tuple<BuildElement, int, int>(item, offset, count - 1));

                            // increment item positions in the fixup dictionary to reflect 
                            // change in their position caused by an element inserted in front of them
                            foreach (var d_item in fixup_dictionary.ToList())
                            {
                                if (d_item.Value > location)
                                    fixup_dictionary[d_item.Key] += 1;
                            }
                        }
                        break;
                    }
                    var ndx = partial_path.LastIndexOf('\\');
                    if (ndx < 0)
                    {
                        location = count - 1;  // this is a brand new path - let us put it in the bottom
                        break;
                    }
                    // Move one step up in the item directory path
                    partial_path = partial_path.Substring(0, ndx);
                }
                partial_path = path;

                // update the fixup dictionary to reflect the positions of the paths we encountered so far
                while (true)
                {
                    fixup_dictionary[partial_path] = location + 1; // the index for the slot to put the next item in
                    var ndx = partial_path.LastIndexOf('\\');
                    if (ndx < 0)
                        break;
                    partial_path = partial_path.Substring(0, ndx);
                }
            }
            foreach (var item in fixup_list)
            {
                for (int i = 1; i <= item.MoveBy; i++)
                    item.Element.SwapWith(itemList[item.Index - i]);
                itemList.Remove(item.Element);
                itemList.Insert(item.Index - item.MoveBy, item.Element);
            }
            project.Save(project.FullFileName);
        }
    }
}
