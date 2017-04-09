﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTaskLib
{
    /// <summary>
    /// Each UnpackTask has a collection of SubTasks, where each SubTask has its own file path involved and unpacking progress etc.
    /// </summary>
    public partial class UnpackTask
    {
        public List<UnpackSubTask> SubTasks { get; set; }
        public string Title { get; set; } //Task Title

        public EventHandler<string> OnFileUnpackStarted;
        public EventHandler<string> OnFileUnpackEnded;


        /// <summary>
        /// Constructor: Given a list of files (*.part1.rar, *.part2.rar...)
        /// This constructor extracts all SubTasks where each sub task has info about a file involved, the progress, the title etc.
        /// </summary>
        /// <param name="paths"> A list of files that are involved in this unpacking task</param>
        public UnpackTask(List<string> paths)
        {
            SubTasks = new List<UnpackSubTask>();

            Title = Path.GetFileNameWithoutExtension(paths.First()); //*.part1 or *.part01
            Title =  StringUtil.GetDotLeftString(Title);
            
            foreach (var path in paths)            
                SubTasks.Add(new UnpackSubTask(path));            
        }

        public void Unpack()
        {

        }

        internal bool CheckFilesExist()
        {
            foreach (var sub in SubTasks)
                if (!sub.FileExists())
                    return false;

            return true;
        }
    }
}