using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SevenZip;

namespace SmartTaskLib
{
    class SevenZipSharpCompressTask : SharpCompressTask
    {
        
        public SevenZipSharpCompressTask(List<string> paths) : base(paths)
        {
        }

        protected override void UnpackImpl()
        {
            var firstFile = InputFilePaths.FirstOrDefault(); //*.rar or *.r01
            if (firstFile == null)
                return;

            SevenZipExtractor se = new SevenZipExtractor(firstFile);

            se.ExtractArchive("C:\\temp\abc");

        }
    }
}
