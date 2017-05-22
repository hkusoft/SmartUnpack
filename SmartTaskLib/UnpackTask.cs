using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

using SharpCompress.Common.Rar;
using SharpCompress.Archives.SevenZip;

namespace SmartTaskLib
{
    /// <summary>
    /// Each UnpackTask has a collection of SubTasks, where each SubTask has its own file path involved and unpacking progress etc.
    /// UnpackTask
    ///     |__ABC.part01.rar (SubTask)
    ///     |__ABC.part02.rar (SubTask)
    ///     ...    
    ///     |__ABC.part0N.rar (SubTask)
    /// </summary>
    public partial class UnpackTask : INotifyPropertyChanged
    {
        public List<UnpackSubTask> SubTasks { get; set; }
        public string Title { get; set; } //Task Title

        int single_file_unpack_progress = 0;
        int overall_progress;

        long bytesUnpacked = 0;
        long currentEntrySize = 0;
        

        /// <summary>
        /// The progress when unpacking a single file, this is useful when
        /// one of the entry file is very large
        /// </summary>
        public int SingleFileUnpackProgress
        {
            get { return single_file_unpack_progress; }
            set {
                single_file_unpack_progress = value;
                OnPropertyChanged("SingleFileUnpackProgress");                                
            }
        }


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



        //Used to check if a task is already in a list of unpacking tasks
        public string Hash 
        {
            get
            {
                var str= SubTasks.Select(item => item.FilePath).Aggregate((a, b) => a + b); // Concatentate all paths of files involved
                return StringUtil.Hash(str);
            }
        }

        private string currentProgressDescription;
        private long TotalSizeInByte = 0;

        //Used to keep the source files to be unpacked, e.g. A.r00, .r01 etc, for deletion purposes
        private List<string> FilePathsToBeUnpacked = new List<string>();


        public delegate void TaskFinished(UnpackTask task, bool bSuccessful);
        public event TaskFinished OnTaskFinished;
        public event PropertyChangedEventHandler PropertyChanged;

        

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


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

        private void InternalUnpackImpl()
        {            
            var firstFile = SubTasks.FirstOrDefault(); //*.rar or *.r01
            if (firstFile == null)
                return;


            var targetFolder = Path.GetDirectoryName(firstFile.FilePath);
            var options = new ExtractionOptions() { ExtractFullPath = true, Overwrite = true };

            using (var archive = ArchiveFactory.Open(firstFile.FilePath))
            {
                if (archive is RarArchive)
                {
                    RarArchive rar = archive as RarArchive;
                    if (rar.IsMultipartVolume() && !rar.IsFirstVolume())
                    {
                        var path = rar.Volumes.FirstOrDefault();
                        CurrentProgressDescription = "Please Unpack from the 1st volume";
                        return;
                    }
                }
                else //Zip or 7z
                {

                }

                if (!archive.IsComplete)
                {
                    CurrentProgressDescription = "Incomplete files: some files are missing.";
                    return;
                }
                bytesUnpacked = 0;
                FilePathsToBeUnpacked.Clear();

                archive.EntryExtractionBegin += Archive_EntryExtractionBegin;
                archive.EntryExtractionEnd += Archive_EntryExtractionEnd;
                archive.FilePartExtractionBegin += Archive_FilePartExtractionBegin;
                archive.CompressedBytesRead += Archive_CompressedBytesRead;

                if (archive.Volumes.Count() == 1) // 7zip or zip files, not multiple volumen rar files
                    FilePathsToBeUnpacked.Add(firstFile.FilePath);

                //SetupEventHandler(archive);

                bool bShouldExtractHere = CheckSingleSubFolderExists(archive) || archive.Entries.Count() == 1;

                if (!bShouldExtractHere)
                    targetFolder = Path.Combine(targetFolder, Title);

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(targetFolder, options);
                    }
                }             
            }

            
            if (SingleFileUnpackProgress == 100)
            {
                CleanUp();
                CurrentProgressDescription = "All Success";
                OnTaskFinished?.Invoke(this, true);
            }
        }

       
        public void Unpack()
        {
            Task.Run(() =>
            {
                InternalUnpackImpl();                
            });
    }

        private bool UnpackIsoFile(string isoFileContainerFolder)
        {
            var isoFiles = Directory.GetFiles(isoFileContainerFolder, "*.iso");
            foreach (var path in isoFiles)
            {
                FilePathsToBeUnpacked.Add(path);
                using (var archive = ArchiveFactory.Open(path))
                {

                }
            }
            return true;
            
            
        }

        
        private void Archive_FilePartExtractionBegin(object sender, FilePartExtractionBeginEventArgs e)
        {            
            if(e.Name.StartsWith("Rar File: "))
            {
                var sub = e.Name.Substring("Rar File: ".Length);
                int i = sub.IndexOf("File Entry:");
                if (i != -1)
                    sub = sub.Substring(0, i).Trim();

                if (!FilePathsToBeUnpacked.Contains(sub))
                    FilePathsToBeUnpacked.Add(sub);

                CurrentProgressDescription = $"--> Extracting {Path.GetFileName(e.Name)}";

                
                //Console.WriteLine(sub);
            }          
        }

        

        private void Archive_EntryExtractionBegin(object sender, SharpCompress.Common.ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            var extractedFile = Path.GetFileName(e.Item.Key);

            currentEntrySize = e.Item.Size;

            CurrentProgressDescription = $"--> Extracting {extractedFile}";

        }

        private void Archive_EntryExtractionEnd(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            IArchive archive = sender as IArchive;
            long totalSize = archive.TotalUncompressSize;
            bytesUnpacked += e.Item.Size;
            OverallProgress = Convert.ToInt32(bytesUnpacked * 100 / totalSize);
        }

        private void Archive_CompressedBytesRead(object sender, CompressedBytesReadEventArgs e)
        {
            IArchive archive = sender as IArchive;
            //long totalSize = archive.TotalUncompressSize;
            //bytesUnpacked += e.CompressedBytesRead;

            if(currentEntrySize !=0)
                SingleFileUnpackProgress = Convert.ToInt32(e.CompressedBytesRead * 100 / currentEntrySize);

        }


        private void CleanUp()
        {            
            foreach (var item in FilePathsToBeUnpacked)
            {
                //Console.WriteLine(item);
                DeleteFile(item);
            }
         
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="bMove2RecyclerBin">By default, directly remove the file.</param>
        /// <returns></returns>
        private bool DeleteFile(string filePath, bool bMove2RecyclerBin=false)
        {
            bool bSuccess = false;
            try
            {
                var name = Path.GetFileName(filePath);
                CurrentProgressDescription = $"File clean up: Removing {name}";

                if (bMove2RecyclerBin)
                    FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else
                    File.Delete(filePath);

                bSuccess = true;
            }
            catch (Exception ex)
            {
                CurrentProgressDescription = $"Exception: {ex.Message}";
                return false;
            }
            return bSuccess;
        }

       

        internal bool CheckFilesExist()
        {
            foreach (var sub in SubTasks)
                if (!sub.FileExists())
                    return false;

            return true;
        }

        /// <summary>
        /// Checks if a single child folder exists in the unpacked archive
        /// if yes, the archive can be extracted without create a subfolder first
        /// 
        /// This is done by traversing all entries to see if the number of directory entry is 1
        /// to filter out subdirectories, check if the entry name contains '\'
        /// </summary>
        /// <param name="archive"></param>
        /// <returns></returns>
        internal bool CheckSingleSubFolderExists(IArchive archive)
        {
            int nDirectoryItems = 0;
            if (archive is RarArchive)
            {
                //For rar files, a directy is considered as an entry
                foreach (RarArchiveEntry item in archive.Entries)
                {
                    //Console.WriteLine(item.Key);
                    if (item.IsDirectory && !item.Key.Contains(@"\") && !item.Key.Contains(@"/"))
                        nDirectoryItems++;
                }
                return nDirectoryItems == 1;
            }
            else if (archive is SevenZipArchive)
            {
                //All elements in the hashsets are unique
                HashSet<int> firstIndexPositionOfSlash = new HashSet<int>();

                //For 7Zip, all files are considered as file entry, and no directory entry exists
                // ABC/aaa.txt, ABC/bbb.txt, ABC/DEF/Aaaa
                // if all entries have root folder of ABC, then single folder exists
                foreach (var item in archive.Entries)
                {
                    int i = item.Key.IndexOf(@"/");
                    if (i != -1)
                        firstIndexPositionOfSlash.Add(i);
                }
                return firstIndexPositionOfSlash.Count == 1;
            }
            else
                return false;
                     
        }
    }
}
