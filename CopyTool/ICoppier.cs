using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CopyTool
{
    internal interface ICoppier
    {
        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="path">文件目录</param>
        /// <param name="topath">文件目标目录</param>
        void Copy(string path,string topath);
    }
}
