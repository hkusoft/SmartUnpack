using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using DiscUtils;
using DiscUtils.Iso9660;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using SmartUnpack.ExtractionTask;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace SmartTaskLib
{
    public class Util
    {
        public static TaskBase CreateTask(List<string> inputPaths, int passwordIndex)
        {
            //return new SharpCompressTask(inputPaths);
            return new SevenZipTask(inputPaths, passwordIndex);
        }
        public static string GetDotLeftString(string input)
        {
            int i = input.LastIndexOf(".");
            return i != -1 ? input.Substring(0, i) : input;
        }

        public static string GetDotRightString(string input)
        {
            int i = input.LastIndexOf(".");
            return i != -1 ? input.Substring(i + 1) : input;
        }


        // C:\ab\cd\ef.rar --> C:\ab\cd\ef\
        public static string GetTargetPath(string filepath)
        {
            string extension = Path.GetExtension(filepath);
            string output = filepath.Substring(0, filepath.Length - extension.Length);
            return output;
        }


        public static bool CheckFilesExist(IEnumerable<string> InputFilePaths)
        {
            foreach (var path in InputFilePaths)
                if (!File.Exists(path))
                    return false;
            return true;
        }


        /// <summary>
        /// This function computes the SHA1 hash value of a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }



        // public static void ExtractISO(string IsoFilePath, string ExtractionPath)
        // {
        //     using (FileStream ISOStream = File.Open(IsoFilePath, FileMode.Open))
        //     {
        //         CDReader Reader = new CDReader(ISOStream, true, true);
        //         ExtractDirectory(Reader.Root, ExtractionPath + Path.GetFileNameWithoutExtension(IsoFilePath) + "\\", "");
        //         Reader.Dispose();
        //     }
        // }


        private static void ExtractDirectory(DiscDirectoryInfo ddi, string RootPath, string PathinISO)
        {
            if (!string.IsNullOrWhiteSpace(PathinISO))
            {
                PathinISO += "\\" + ddi.Name;
            }
            RootPath += "\\" + ddi.Name;
            AppendDirectory(RootPath);
            foreach (DiscDirectoryInfo dinfo in ddi.GetDirectories())
            {
                ExtractDirectory(dinfo, RootPath, PathinISO);
            }
            foreach (DiscFileInfo finfo in ddi.GetFiles())
            {
                using (Stream FileStr = finfo.OpenRead())
                {
                    using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name)) // Here you can Set the BufferSize Also e.g. File.Create(RootPath + "\\" + finfo.Name, 4 * 1024)
                    {
                        FileStr.CopyTo(Fs, 4 * 1024); // Buffer Size is 4 * 1024 but you can modify it in your code as per your need
                    }
                }
            }
        }
        private static void AppendDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Ex2)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
        }

        /// <summary>
        /// Scans all rar files in the input folderm and returns a list of unpack tasks
        /// </summary>
        /// <param name="inputFolderPath"></param>
        /// <returns></returns>
        public static Dictionary<string, TaskBase> ScanDirectory(string inputFolderPath, int passwordIndex)
        {
            var output = new Dictionary<string, TaskBase>();

            // aaa.part1.rar, aaa.part2.rar, bbb.part1.rar
            // or abc.rar, single rar
            var rarFilePaths = System.IO.Directory.GetFiles(inputFolderPath, "*.rar");
            // aaa.part1, aaa.part2, bbb.part1
            var rarFileNames = rarFilePaths.Select(entry => Path.GetFileNameWithoutExtension(entry));

            #region Multi-Volume Archives
            // aaa, bbb
            Regex re = new Regex(@"(.+)\.(part)(\d+)");
            var names = (from name in rarFileNames
                         let match = re.Match(name)
                         where match.Success
                         select match.Groups[1].Value).Distinct();

            // aaa.part*.rar
            foreach (string name in names)
            {
                var elements = rarFileNames.Where(item => item.StartsWith(name + ".part"));
                var paths = elements.Select(entry => Path.Combine(inputFolderPath, entry) + ".rar").ToList();
                var unpackTask = CreateTask(paths, passwordIndex);
                bool bExists = Util.CheckFilesExist(rarFilePaths);
                if (bExists)
                    output[unpackTask.Hash] = unpackTask;
            }
            #endregion

            #region Single archive file
            var singleArchiveFiles = from name in rarFileNames
                                     let match = re.Match(name)
                                     where !match.Success
                                     select name;
            foreach (var entry in singleArchiveFiles)
            {
                var paths = new List<string>() {Path.Combine(inputFolderPath, entry) + ".rar"};
                var unpackTask = new TaskBase(paths, passwordIndex);
                bool bExists = Util.CheckFilesExist(rarFilePaths);
                if (bExists)
                    output[unpackTask.Hash] = unpackTask;
            }

            #endregion


            return output;
        }

        /// <summary>
        /// Creates a Unpack task from a given path, the filePath might not be the *.part01, it might be *.part03 as well
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<string, TaskBase> CreateTaskForFile(string filePath, int passwordIndex)
        {
            var output = new Dictionary<string, TaskBase>();
            var unpackTask = CreateTask(new List<string>() { filePath }, passwordIndex);
            bool bExists = Util.CheckFilesExist(unpackTask.InputFilePaths);
            if (bExists)
                output[unpackTask.Hash] = unpackTask;
            else if (Directory.Exists(filePath))
                return ScanDirectory(filePath, passwordIndex);

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="bMove2RecyclerBin">By default, directly remove the file.</param>
        /// <returns></returns>
        public static bool DeleteFile(string filePath, out string resultMessage, bool bMove2RecyclerBin = false)
        {
            bool bSuccess = false;
            try
            {
                var name = Path.GetFileName(filePath);
                resultMessage = $"File clean up: Removing {name}";

                if (bMove2RecyclerBin)
                    FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else
                    File.Delete(filePath);

                bSuccess = true;
            }
            catch (Exception ex)
            {
                resultMessage = $"Exception: {ex.Message}";
                return false;
            }
            return bSuccess;
        }

        public static string GetTempDirectoryName()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            //Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static bool HasSingleChildFolder(string path)
        {
            return Directory.GetDirectories(path).Length == 1 && Directory.GetFiles(path).Length == 0;
        }

        private static bool HasSingleChildFolder(DirectoryInfo di)
        {
            return HasSingleChildFolder(di.FullName);
        }

        //path = C:\ab\cd , if cd has only one child folder ef, then:
        //C:\ab\cd\ef\* --> C:\ab\cd\*
        public static void MoveSingleFolderToParent(string path)
        {
            var temp = GetTempDirectoryName();
            var di = new DirectoryInfo(path);
            if(HasSingleChildFolder(di.Parent))
            {
                di.MoveTo(temp);
                var newPath = new DirectoryInfo(path).Parent.FullName;
                if(new DirectoryInfo(newPath).Exists)
                    Directory.Delete(newPath);
                Directory.Move(temp, newPath);
            }
        }
    }
}
