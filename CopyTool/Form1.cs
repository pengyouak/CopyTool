﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CopyTool
{
    public partial class Form1 : Form
    {
        private System.Threading.Tasks.Task _task;
        private object _lock = new object();
        private System.Threading.CancellationTokenSource _cts;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (btnCopy.Text == "停止复制")
            {
                if (_cts != null)
                    _cts.Cancel();
            }
            var dirList = txtSourceDirs.Lines.ToList<string>();
            try
            {
                foreach (string path in dirList)
                {
                    var dir = new System.IO.DirectoryInfo(path.Substring(0, path.LastIndexOf("\\") + 1));
                    if (!System.IO.Directory.Exists(dir.FullName))
                    {
                        MessageBox.Show("目录：" + dir + " 不存在");
                        return;
                    }
                }
                if (!System.IO.Directory.Exists(txtDir.Text.Trim()))
                {
                    if (MessageBox.Show("目标目录不存在，需要自动创建吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;
                    else
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(txtDir.Text.Trim());
                        }
                        catch (Exception ex) { MessageBox.Show(ex.Message); return; }
                    }
                }
            }
            catch (Exception ex){
                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [初始化异常] {0}", ex.Message));
                return;
            }

            SaveInfo();
            btnCopy.Text = "停止复制";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Maximum = dirList.Count;
            _cts = new System.Threading.CancellationTokenSource();
            var param=new CopyTaskParam{
                FileList=dirList,
                CopyToDir=txtDir.Text,
                CancelToken=_cts.Token
            };
            _task = new System.Threading.Tasks.Task(new Action<object>(Copy), param, _cts.Token);
            
            _task.Start();
            //_task.Wait();
            //MessageBox.Show("复制完成");
        }

        private void ReadInfo()
        {
            if(!System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory()+ "\\CopyConfig.cfg"))
                return;
            try
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + ": [读取配置]}");
                var tmp = System.IO.File.ReadAllLines(System.IO.Directory.GetCurrentDirectory() + "\\CopyConfig.cfg", Encoding.UTF8);
                txtSourceDirs.Lines = tmp.Skip(3).ToArray();
                txtDir.Text = tmp[1];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [读取配置异常] {0}", ex.Message));
                return;
            }
        }

        private void SaveInfo()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + ": [保存配置]}");
                var list = new List<string>();
                list.Add("目标目录");
                list.Add(txtDir.Text);
                list.Add("源目录");
                list.AddRange(txtSourceDirs.Lines);
                System.IO.File.WriteAllLines(System.IO.Directory.GetCurrentDirectory() + "\\CopyConfig.cfg", list.ToArray(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [保存配置异常] {0}", ex.Message));
                return;
            }
        }

        private void Copy(object obj)
        {
            var param = obj as CopyTaskParam;
            Copy(param.FileList, param.CopyToDir,param.CancelToken);
        }
        private void Copy(List<string> sourceDir,string dir,System.Threading.CancellationToken token)
        {
            int count = 0;
            sourceDir.AsParallel().ForAll(file => {
                if (file.IndexOf("\\") < 0)
                    return;
                var path = file.Substring(0, file.LastIndexOf("\\") + 1);
                var fileName = file.Replace(path, "");
                var tmpFile = fileName.Split('|');

                for (int i = 0; i < tmpFile.Length; i++)
                {
                    if (token.IsCancellationRequested)
                        break;
                    if (tmpFile[i].IndexOf( "*")>-1)
                    {
                        try
                        {
                            var files = System.IO.Directory.GetFiles(path, tmpFile[i]);
                            foreach (string f in files)
                            {
                                if (token.IsCancellationRequested)
                                    break;
                                System.Threading.Interlocked.Add(ref count, 1);
                                try
                                {
                                    var tmp = new System.IO.FileInfo(f);
                                    System.IO.File.Copy(f, dir + "\\" + tmp.Name, true);
                                    System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [按扩展名] 从文件{0}, 复制到{1}", f, dir + "\\" + tmp.Name));
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [文件异常] {0}\r\n来自{1}", ex.Message, f));
                                }
                            }
                        }
                        catch (Exception ex){
                            System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [目录异常] {0}\r\n来自目录{1}\r\n扩展名{2}", ex.Message, path, tmpFile[i]));
                        }
                    }
                    else
                    {
                        if (System.IO.File.Exists(path + tmpFile[i]))
                        {
                            System.Threading.Interlocked.Add(ref count, 1);
                            try
                            {
                                System.IO.File.Copy(path + tmpFile[i], dir + "\\" + tmpFile[i], true);
                                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [按文件名] 从文件{0}, 复制到{1}", path + tmpFile[i], dir + "\\" + tmpFile[i]));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [文件异常] {0}\r\n来自源目录{1}\r\n目标目录{2}", ex.Message, path + tmpFile[i], dir + "\\" + tmpFile[i]));
                            }
                        }
                    }
                }
                lock (_lock)
                {
                    progressBar1.Invoke(new Action(() => { 
                        progressBar1.Value += 1;
                        btnCopy.Invoke(new Action(() => { if (progressBar1.Value == progressBar1.Maximum || token.IsCancellationRequested)btnCopy.Text = "开始复制"; }));
                        if (progressBar1.Value == progressBar1.Maximum||token.IsCancellationRequested) 
                        {
                            System.Diagnostics.Debug.WriteLine(DateTime.Now + string.Format(": 复制完成, 共复制文件{0}个\r\n/************************************************************************************/\r\n",count));
                            MessageBox.Show("复制完成");
                            progressBar1.Value = 0; 
                        } 
                    }));
                   
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ReadInfo();
        }
    }

    public class CopyTaskParam
    {
        public List<string> FileList { get; set; }
        public string CopyToDir { get; set; }
        public System.Threading.CancellationToken CancelToken { get; set; }
    }
}