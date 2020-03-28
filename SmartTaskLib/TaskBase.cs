using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTaskLib
{
    public class TaskBase : INotifyPropertyChanged
    {
        #region Fields and Properties

        protected List<string> InputFilePaths { get; set; } //A.r01, A.r02
        public string TargetExtractionFolder { get; set; } //Where to unpack these file?
        //Total byte unpacked so far
        protected long bytesUnpacked = 0;
        //The active file under unpacking, its size is updated when a new entry beings unpacked
        protected long currentEntrySize = 0;

        /// <summary>
        /// Used to further create a new unpack task when a single child folder is unpacked,
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
        /// Used to check if a task is already in a list of unpacking tasks
        /// The hash is defined as the SHA1 of the concatenated string of all file paths
        /// </summary>
        public string Hash
        {
            get
            {
                var str = InputFilePaths.Aggregate((a, b) => a + b); // Concatentate all paths of files involved
                return StringUtil.Hash(str);
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

        protected void OnUnpackFinished(bool bSuccessful)
        {
            OnTaskFinished?.Invoke(this, bSuccessful);
        }

        public void Unpack()
        {
            Task.Run(() =>
            {
                UnpackImpl();
            });
        }

        protected virtual void UnpackImpl() { 

        }

      
    }

}
