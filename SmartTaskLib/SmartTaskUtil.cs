using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartTaskLib
{
    public class SmartTaskUtil
    {
        /// <summary>
        /// Scans all rar files in the input folderm and returns a list of unpack tasks
        /// </summary>
        /// <param name="inputFolderPath"></param>
        /// <returns></returns>
        public static List<UnpackTask> ScanDirectory(string inputFolderPath)
        {
            var output = new List<UnpackTask>();

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
                var unpackTask = new UnpackTask(elements.Select(entry => Path.Combine(inputFolderPath, entry) + ".rar").ToList());
                bool bExists = unpackTask.CheckFilesExist();
                if (bExists)
                    output.Add(unpackTask);
            }
            #endregion

            #region Single archive file
            var singleArchiveFiles = from name in rarFileNames
                                     let match = re.Match(name)
                                     where !match.Success
                                     select name;
            foreach (var entry in singleArchiveFiles)
            {
                var unpackTask = new UnpackTask(new List<string>() { Path.Combine(inputFolderPath, entry) + ".rar" });
                bool bExists = unpackTask.CheckFilesExist();
                if (bExists)
                    output.Add(unpackTask);
            }

            #endregion


            return output;
        }

        /// <summary>
        /// Creates a Unpack task from a given path, the filePath might not be the *.part01, it might be *.part03 as well
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<UnpackTask> CreateTaskForFile(string filePath)
        {
            var output = new List<UnpackTask>();
            var unpackTask = new UnpackTask(new List<string>() { filePath});
            bool bExists = unpackTask.CheckFilesExist();
            if (bExists)
                output.Add(unpackTask);

            return output;

        }

        
    }
        
}
