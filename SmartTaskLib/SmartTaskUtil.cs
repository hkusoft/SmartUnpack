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
        public static List<UnpackTask> ScanDirectory(string inputFolderPath)
        {
            var output = new List<UnpackTask>();           
            
            // aaa.part1.rar, aaa.part2.rar, bbb.part1.rar
            var rarFilePaths = System.IO.Directory.GetFiles(inputFolderPath, "*.rar");
            // aaa.part1, aaa.part2, bbb.part1
            var rarFileNames = rarFilePaths.Select(entry => Path.GetFileNameWithoutExtension(entry));

            // aaa, bbb
            Regex re = new Regex(@"(.+)\.(part)(\d+)");
            var names = (from name in rarFileNames
                        let match = re.Match(name)
                        where match.Success
                        select match.Groups[1].Value).Distinct();

            // aaa.part*.rar
            foreach(string name in names)
            {
                var elements = rarFileNames.Where(item => item.StartsWith(name + ".part"));
                var unpackTask = new UnpackTask(elements.Select(entry => Path.Combine(inputFolderPath, entry)+".rar").ToList());
                bool bExists = unpackTask.CheckFilesExist();
                if(bExists)
                    output.Add(unpackTask);                
            }

            return output;
        }
    }
}
