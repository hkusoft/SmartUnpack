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
    ///     |__ABC.part01.rar (FilePath 1)
    ///     |__ABC.part02.rar (FilePath 2)
    ///     ...    
    ///     |__ABC.part0N.rar (FilePath N)
    /// </summary>
    public class SharpCompressTask : TaskBase
    {
        /// <summary>
        /// Constructor: Given a list of files (*.part1.rar, *.part2.rar...)
        /// This constructor extracts all SubTasks where each sub task has info about a file involved, the progress, the title etc.
        /// </summary>
        /// <param name="paths"> A list of files that are involved in this unpacking task</param>
        public SharpCompressTask(List<string> paths)
        {
            var title = Path.GetFileNameWithoutExtension(paths.First()); //*.part1 or *.part01
            Title = StringUtil.GetDotLeftString(title);
            InputFilePaths = paths;
        }
               

        protected override void UnpackImpl()
        {
            var firstFile = InputFilePaths.FirstOrDefault(); //*.rar or *.r01
            if (firstFile == null)
                return;

            TargetExtractionFolder = Path.GetDirectoryName(firstFile);
            var options = new ExtractionOptions() { ExtractFullPath = true, Overwrite = true };

            using (var archive = ArchiveFactory.Open(firstFile))
            {
                if (archive is RarArchive)
                {
                    RarArchive rar = archive as RarArchive;
                    if (rar.IsMultipartVolume() && !rar.IsFirstVolume())
                    {
                        var path = rar.Volumes.FirstOrDefault();
                        CurrentProgressDescription = "Please Unpack from the 1st volume";
                        OnUnpackFinished(false);
                        return;
                    }
                }
                else //Zip or 7z
                {

                }

                if (!archive.IsComplete)
                {
                    CurrentProgressDescription = "Incomplete files: some files are missing.";
                    OnUnpackFinished(false);
                    return;
                }
                bytesUnpacked = 0;

                archive.EntryExtractionBegin += Archive_EntryExtractionBegin;
                archive.EntryExtractionEnd += Archive_EntryExtractionEnd;

                archive.CompressedBytesRead += Archive_CompressedBytesRead;

                bool bSingleChildFolderExists = CheckSingleSubFolderExists(archive);
                if (bSingleChildFolderExists)
                    single_child_folder_to_unpack_to = Path.Combine(TargetExtractionFolder, single_child_folder_to_unpack_to);

                bool bShouldExtractHere = bSingleChildFolderExists || archive.Entries.Count() == 1;

                if (!bShouldExtractHere)
                {
                    TargetExtractionFolder = Path.Combine(TargetExtractionFolder, Title);
                }

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(TargetExtractionFolder, options);
                    }
                }
            }


            if (SingleFileUnpackProgress == 100)
            {
                CleanUp();
                OnUnpackFinished(true);

            }
        }

        private bool UnpackIsoFile(string isoFileContainerFolder)
        {
            var isoFiles = Directory.GetFiles(isoFileContainerFolder, "*.iso");
            foreach (var path in isoFiles)
            {
                //FilePathsToBeUnpacked.Add(path);
                using (var archive = ArchiveFactory.Open(path))
                {

                }
            }
            return true;
        }


        #region Unpacking event handlers, progress updates
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

            if (currentEntrySize != 0)
                SingleFileUnpackProgress = Convert.ToInt32(e.CompressedBytesRead * 100 / currentEntrySize);

        }

        #endregion

        #region Utility Functions

        private void CleanUp()
        {
            foreach (var item in InputFilePaths)
                DeleteFile(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="bMove2RecyclerBin">By default, directly remove the file.</param>
        /// <returns></returns>
        private bool DeleteFile(string filePath, bool bMove2RecyclerBin = false)
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
            foreach (var path in InputFilePaths)
                if (!File.Exists(path))
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
                    if (item.IsDirectory)
                        single_child_folder_to_unpack_to = item.Key;


                    if (item.IsDirectory && !item.Key.Contains(@"\") && !item.Key.Contains(@"/"))
                        nDirectoryItems++;
                }
                if (nDirectoryItems != 1)
                    single_child_folder_to_unpack_to = null;

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

        #endregion
    }
}
