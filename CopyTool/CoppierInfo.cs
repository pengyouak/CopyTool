using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CopyTool
{
    internal class CoppierInfo
    {
        public System.IO.FileInfo FileInfo { get; set; }
        public System.IO.FileInfo FileToInfo { get; set; }
        public bool IsSuccess { get; set; }
    }
}
