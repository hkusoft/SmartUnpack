using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTaskLib.Tests
{
    [TestClass()]
    public class SmartTaskUtilTests
    {
        [TestMethod()]
        public void ScanDirectoryTest()
        {
            var output = SmartTaskUtil.ScanDirectory(@"c:\Users\David\Downloads\Compressed\");

            Assert.IsTrue(output.Count >0);
        }
    }
}