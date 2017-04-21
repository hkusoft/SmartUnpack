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

        int progress = 0;
        long bytesUnpacked = 0;
        public int Progress
        {
            get { return progress; }
            set {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private string currentProgressDescription;
        private long TotalSizeInByte = 0;

        //Used to keep the source files to be unpacked, e.g. A.r00, .r01 etc, for deletion purposes
        private List<string> FilePathsToBeUnpacked = new List<string>();

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

        private bool InternalUnpackImpl()
        {
            var firstFile = SubTasks.FirstOrDefault(); //*.rar or *.r01
            if (firstFile == null)
                return false;


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
                        return false;
                    }
                }
                else //Zip or 7z
                {

                }

                if (!archive.IsComplete)
                {
                    CurrentProgressDescription = "Incomplete files: some files are missing.";
                    return false;
                }
                bytesUnpacked = 0;
                FilePathsToBeUnpacked.Clear();

                archive.EntryExtractionBegin += Archive_EntryExtractionBegin;
                archive.EntryExtractionEnd += Archive_EntryExtractionEnd;
                archive.FilePartExtractionBegin += Archive_FilePartExtractionBegin;

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

                return true;
            }
        }
        public void Unpack()
        {
            Task.Run(() =>
            {
                bool bSuccess = InternalUnpackImpl();
                if (bSuccess)
                {
                    bSuccess = CleanUp();
                    if (bSuccess)
                        CurrentProgressDescription = "All Success";
                }
            });
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

                //Console.WriteLine(sub);
            }

          
        }

        private void Archive_EntryExtractionEnd(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            CurrentProgressDescription = "Done";

            IArchive archive = sender as IArchive;
            long totalSize = archive.TotalUncompressSize;
            bytesUnpacked += e.Item.Size;

            Progress = Convert.ToInt32(bytesUnpacked * 100 / totalSize);
        }

        private bool CleanUp()
        {
            bool bSuccess = true;
            foreach (var item in FilePathsToBeUnpacked)
            {
                //Console.WriteLine(item);
                bSuccess &= MoveToRecycleBin(item);
            }
            return bSuccess;
        }

        private bool MoveToRecycleBin(string filePath)
        {
            bool bSuccess = false;
            try
            {
                var name = Path.GetFileName(filePath);
                CurrentProgressDescription = $"File clean up: Removing {name}";
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                bSuccess = true;
            }
            catch (Exception ex)
            {
                CurrentProgressDescription = $"Exception: {ex.Message}";
                return false;
            }
            return bSuccess;
        }

        private void Archive_EntryExtractionBegin(object sender, SharpCompress.Common.ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            var extractedFile = Path.GetFileName(e.Item.Key);
            CurrentProgressDescription = $"--> Extracting {extractedFile}";
            
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
