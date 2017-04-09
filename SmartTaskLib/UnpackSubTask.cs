using System.IO;

namespace SmartTaskLib
{
    public class UnpackSubTask
    {
        public int ImageResId { get; set; }
        public string FilePath { get; set; }

        public string Title { get; private set; }
        public int CurrentProgress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"> *.part1.rar, *.part2.rar ... </param>
        public UnpackSubTask(string filePath)
        {
            FilePath = filePath;
            Title = Path.GetFileNameWithoutExtension(FilePath);
            //*.part1 --> part1
            Title = StringUtil.GetDotRightString(Title);            
        }

        public bool FileExists()
        {
            return File.Exists(FilePath);
        }

        public override string ToString()
        {
            return Title;
        }
    }

}
