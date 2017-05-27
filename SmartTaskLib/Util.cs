using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using DiscUtils;
using DiscUtils.Iso9660;
using System.IO;

namespace SmartTaskLib
{
    public class StringUtil
    {
        public static string GetDotLeftString(string input)
        {
            int i = input.LastIndexOf(".");
            return i != -1 ? input.Substring(0, i) : input;
        }

        public static string GetDotRightString(string input)
        {
            int i = input.LastIndexOf(".");
            return i != -1 ? input.Substring(i+1) : input;
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

        

    public static void ExtractISO(string IsoFilePath, string ExtractionPath)
    {
        using (FileStream ISOStream = File.Open(IsoFilePath, FileMode.Open))
        {
            CDReader Reader = new CDReader(ISOStream, true, true);
            ExtractDirectory(Reader.Root, ExtractionPath + Path.GetFileNameWithoutExtension(IsoFilePath) + "\\", "");
            Reader.Dispose();
        }
    }
    private static void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
    {
        if (!string.IsNullOrWhiteSpace(PathinISO))
        {
            PathinISO += "\\" + Dinfo.Name;
        }
        RootPath += "\\" + Dinfo.Name;
        AppendDirectory(RootPath);
        foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
        {
            ExtractDirectory(dinfo, RootPath, PathinISO);
        }
        foreach (DiscFileInfo finfo in Dinfo.GetFiles())
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
        catch (PathTooLongException Exx)
        {
            AppendDirectory(Path.GetDirectoryName(path));
        }
    }
}
}
