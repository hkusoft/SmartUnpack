using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace SmartTaskLib
{
    public class TaskBase : INotifyPropertyChanged
    {
        #region Fields and Properties

        public List<string> InputFilePaths { get; set; } //A.r01, A.r02
        public string TargetExtractionFolder { get; set; } //Where to unpack these file?
        //Total byte unpacked so far
        protected long bytesUnpacked = 0;
        //The active file under unpacking, its size is updated when a new entry beings unpacked
        protected long currentEntrySize = 0;

        /// <summary>
        /// Used to further create a new unpack sharpCompressTask when a single child folder is unpacked,
        /// then we check if there is other rar files to be unpacked, used in OnTaskFinished() callback
        /// </summary>
        protected string single_child_folder_to_unpack_to = null;
        public string SingleChildFolder2UnpackTo
        {
            get
            {
                return single_child_folder_to_unpack_to;
            }
        }

        public bool HasSoleChildFolder2Unpack
        {
            get
            {
                return !string.IsNullOrEmpty(SingleChildFolder2UnpackTo);
            }
        }




        #region Single File Unpack Progress
        //When unpacking a single file, the progress value
        int single_file_unpack_progress = 0;
        /// <summary>
        /// The progress when unpacking a single file, this is useful when
        /// one of the entry file is very large
        /// </summary>
        public int SingleFileUnpackProgress
        {
            get { return single_file_unpack_progress; }
            set
            {
                single_file_unpack_progress = value;
                OnPropertyChanged("SingleFileUnpackProgress");
            }
        }
        #endregion

        #region Overall File Unpack Progress

        //When unpacking multiple volume files, the overall progress 
        protected int overall_progress;
        /// <summary>
        /// The progress when unpacking a single file, this is useful when
        /// one of the entry file is very large
        /// </summary>
        public int OverallProgress
        {
            get { return overall_progress; }
            set
            {
                overall_progress = value;
                OnPropertyChanged("OverallProgress");
            }
        }
        #endregion

        #region Current Progress Description

        /// <summary>
        
        /// The string to be displayed when unpacking a file
        /// </summary>
        /// 
        protected string currentProgressDescription;
        public string CurrentProgressDescription
        {
            get
            {
                return currentProgressDescription;
            }
            set
            {
                currentProgressDescription = value;
                OnPropertyChanged("CurrentProgressDescription");
            }
        }

        #endregion


        /// <summary>
        /// Used to check if a sharpCompressTask is already in a list of unpacking tasks
        /// The hash is defined as the SHA1 of the concatenated string of all file paths
        /// </summary>
        public string Hash
        {
            get
            {
                var str = InputFilePaths.Aggregate((a, b) => a + b); // Concatentate all paths of files involved
                return Util.Hash(str);
            }
        }
        public string Title { get; set; } //Task Title

        #endregion

        #region INotifyPropertyChanged

        // Create the OnPropertyChanged method to raise the event
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        #region Events exposed                
        public delegate void TaskFinished(TaskBase task, bool bSuccessful);
        public event TaskFinished OnTaskFinished;
        #endregion


        /// <summary>
        /// Constructor: Given a list of files (*.part1.rar, *.part2.rar...)
        /// This constructor extracts all SubTasks where each sub sharpCompressTask has info about a file involved, the progress, the title etc.
        ///
        /// Only the first file name is used
        /// </summary>
        /// <param name="paths"> A list of files that are involved in this unpacking sharpCompressTask</param>
        public TaskBase(List<string> paths)
        {
            var title = Path.GetFileNameWithoutExtension(paths.First()); //*.part1 or *.part01
            Title = Util.GetDotLeftString(title);  //Used for UI binding
            InputFilePaths = paths;
        }
        
        protected void OnUnpackFinished(bool bSuccessful)
        {
            OnTaskFinished?.Invoke(this, bSuccessful);
        }

        public void Unpack()
        {
            Task.Run(() =>
            {
                UnpackImpl();
                if (SingleFileUnpackProgress == 100)
                {
                    OnUnpackFinished(true);
                    CleanUp();
                }
            });
        }

        protected virtual void UnpackImpl() { 

        }

        private void CleanUp()
        {
            String message = "";            
            Util.MoveSingleFolderToParent(TargetExtractionFolder);

            foreach (var item in InputFilePaths)
                Util.DeleteFile(item, out message);
        }



    }

}
