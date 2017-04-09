using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
