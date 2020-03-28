using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTaskLib
{
    class DotnetZipTask : TaskBase
    {
        public DotnetZipTask(List<string> paths) : base(paths)
        {
        }

        protected override void UnpackImpl()
        {

        }
    }
}
