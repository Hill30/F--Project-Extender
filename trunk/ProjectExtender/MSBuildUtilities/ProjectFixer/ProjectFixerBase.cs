using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSharp.ProjectExtender.Project;

namespace FSharp.ProjectExtender.MSBuildUtilities.ProjectFixer
{
    abstract class ProjectFixerBase: IEnumerable<IBuildItem>
    {
        /// <summary>
        /// Adjusts the positions of build elements to ensure the project can be loaded by the FSharp project system
        /// </summary>
        internal static void FixupProject(IEnumerable<IBuildItem> buildItems)
        {

            var fixupDictionary = new Dictionary<string, int>();
            var fixupList = new List<Tuple<IBuildItem, int, int>>();
            var itemList = new List<IBuildItem>();
            var count = 0;

            foreach (var item in buildItems.Where(
                    n => n.Type == "Compile" || n.Type == "Content" || n.Type == "None"
                    ))
            {
                item.RemoveMetadata(Constants.moveByTag);
                itemList.Add(item);
                count++;
                string linkLocation = item.GetMetadata(Constants.link);
                string path = (String.IsNullOrEmpty(linkLocation)) ? Path.GetDirectoryName(item.Include) : Path.GetDirectoryName(linkLocation);
                //if the item is root level item - think as if it is a folder
                if (String.Compare(path, "") == 0)
                    path = item.Include;
                string partialPath = path;
                int location;
                while (true)
                {
                    // The partial path was already encountered in the project file
                    if (fixupDictionary.TryGetValue(partialPath, out location))
                    {
                        var offset = count - 1 - location; // we need to move it up in the build file by this many slots

                        // if offset = 0 this item does not have to be moved
                        if (offset > 0)
                        {
                            item.SetMetadata(Constants.moveByTag, offset.ToString());

                            // add the item to the fixup list
                            fixupList.Add(new Tuple<IBuildItem, int, int>(item, offset, count - 1));

                            // increment item positions in the fixup dictionary to reflect 
                            // change in their position caused by an element inserted in front of them
                            foreach (var dItem in fixupDictionary.ToList())
                            {
                                if (dItem.Value > location)
                                    fixupDictionary[dItem.Key] += 1;
                            }
                        }
                        break;
                    }
                    var ndx = partialPath.LastIndexOf('\\');
                    if (ndx < 0)
                    {
                        location = count - 1;  // this is a brand new path - let us put it in the bottom
                        break;
                    }
                    // Move one step up in the item directory path
                    partialPath = partialPath.Substring(0, ndx);
                }
                partialPath = path;

                // update the fixup dictionary to reflect the positions of the paths we encountered so far
                while (true)
                {
                    fixupDictionary[partialPath] = location + 1; // the index for the slot to put the next item in
                    var ndx = partialPath.LastIndexOf('\\');
                    if (ndx < 0)
                        break;
                    partialPath = partialPath.Substring(0, ndx);
                }
            }
            foreach (var item in fixupList)
            {
                for (int i = 1; i <= item.Item2; i++)
                    item.Item1.SwapWith(itemList[item.Item3 - i]);
                itemList.Remove(item.Item1);
                itemList.Insert(item.Item3 - item.Item2, item.Item1);
            }
        }

        public abstract IEnumerator<IBuildItem> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
