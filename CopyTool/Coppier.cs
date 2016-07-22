using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CopyTool
{
    internal class Coppier : ICoppier
    {
        /// <summary>
        /// 文件具体路径集合
        /// </summary>
        IEnumerable<string> _path;
        int _copyCount = 0;
        public int CopyCount { get { return _copyCount; } }
        public int CopyDirCount { get; private set; }
        internal delegate void CopyHandler(object c, CoppierInfo info);
        /// <summary>
        /// 全部完成时触发
        /// </summary>
        internal event CopyHandler OnCompletedCopy;
        /// <summary>
        /// 完成时触发
        /// </summary>
        internal event CopyHandler OnFinishedCopy;
        /// <summary>
        /// 正在复制
        /// </summary>
        internal event CopyHandler OnCoping;
        /// <summary>
        /// 复制失败时触发
        /// </summary>
        internal event CopyHandler OnCopyFailed;
        /// <summary>
        /// 复制成功时触发
        /// </summary>
        internal event CopyHandler OnCopySuccessed;
        /// <summary>
        /// 进度反馈
        /// </summary>
        internal event CopyHandler OnProcReport;

        internal Coppier()
        {
            _path = new List<string>();
        }

        public void Copy(string path,string topath)
        {
            CoppierInfo copyInfo=null;
            try {
                copyInfo = new CoppierInfo
                {
                    FileInfo = new System.IO.FileInfo(path),
                    FileToInfo = new System.IO.FileInfo(topath),
                    IsSuccess = false
                };
                OnProcReport?.Invoke(this, copyInfo);
                OnCoping?.Invoke(this, copyInfo);
                System.IO.File.Copy(path, topath, true);
                copyInfo.IsSuccess = true;
                OnCopySuccessed?.Invoke(this, copyInfo);
            }
            catch(Exception e)
            {
                OnCopyFailed?.Invoke(e, copyInfo);
            }
            finally
            {
                System.Threading.Interlocked.Add(ref _copyCount, 1);
                OnFinishedCopy?.Invoke(this, copyInfo);
            }
        }

        public void Copy(IEnumerable<string> path, string topath,System.Threading.CancellationToken token)
        {
            CopyDirCount = path.ToArray().Length;
            //path.AsParallel().ForAll(p=> {
            foreach(string p in path) {
                if (token.IsCancellationRequested)
                    break;
                if (p.IndexOf("\\") < 0)
                    continue;
                var filepath = p.Substring(0, p.LastIndexOf("\\") + 1);
                var fileName = p.Replace(filepath, "");
                var tmpFile = fileName.Split('|');
                for (int i = 0; i < tmpFile.Length; i++)
                {
                    if (token.IsCancellationRequested)
                        break;
                    if (tmpFile[i].IndexOf("*") > -1)
                    {
                        var files = System.IO.Directory.GetFiles(filepath, tmpFile[i]);
                        foreach (string f in files)
                        {
                            if (token.IsCancellationRequested)
                                break;
                            var tmp = new System.IO.FileInfo(f);
                            Copy(f, topath + "\\" + tmp.Name);
                        }
                    }
                    else
                    {
                        if (System.IO.File.Exists(path + tmpFile[i]))
                        {
                            Copy(path + tmpFile[i], topath + "\\" + tmpFile[i]);
                        }
                    }
                }
            };
            OnCompletedCopy?.Invoke(this, null);
            //);
        }

        internal void Move(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查是否按通配符匹配文件
        /// </summary>
        void CheckMul()
        {

        }
    }
}
