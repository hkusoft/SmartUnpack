using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

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
    }
}
