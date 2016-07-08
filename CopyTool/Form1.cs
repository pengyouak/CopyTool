using System;
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
            if (btnCopy.Text == "停止复制(&S)")
            {
                if (_cts != null)
                    _cts.Cancel();
            }
            var dirList = txtSourceDirs.Lines.ToList<string>();
            try
            {
                foreach (string path in dirList)
                {
                    if (path.Length <= 0)
                        continue;
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
            //btnCopy.Text = "停止复制(&S)";
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

        private void ReadInfo(string strPath="")
        {
            string path = "";
            if (string.IsNullOrEmpty(strPath))
            {
                if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\config\\" ))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\config\\");
                }
                if (System.IO.Directory.GetDirectories(System.IO.Directory.GetCurrentDirectory() + "\\config").Length <= 0)
                {
                    cboConfig.Items.Add("Default");
                    cboConfig.SelectedIndex = 0;
                    path = System.IO.Directory.GetCurrentDirectory() + "\\config\\Default\\CopyConfig.cfg";
                }
                else
                {
                    var dirs = System.IO.Directory.GetDirectories(System.IO.Directory.GetCurrentDirectory() + "\\config");
                    foreach (string dir in dirs)
                    {
                        var tmp = new System.IO.DirectoryInfo(dir);
                        cboConfig.Items.Add(tmp.Name);
                    }
                    cboConfig.SelectedIndex = 0;
                    path = System.IO.Directory.GetCurrentDirectory() + "\\config\\" + cboConfig.Text + "\\CopyConfig.cfg";
                }
            }
            else
                path = System.IO.Directory.GetCurrentDirectory() + "\\config\\" + strPath + "\\CopyConfig.cfg";
            if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\config\\"+ strPath + "\\"))
            {
                System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\config\\"+ strPath + "\\");
            }
            try
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + ": [读取配置]");
                var tmp = System.IO.File.ReadAllLines(path, Encoding.UTF8);
                txtSourceDirs.Lines = tmp.Skip(5).ToArray();
                txtFileName.Text = tmp[1];
                txtDir.Text = tmp[3];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [读取配置异常] {0}", ex.Message));
                return;
            }
        }

        private void ShowLog(string msg,int index,string path="")
        {
            lsvLog.UpdateUI(() => lsvLog.Items.Insert(0, new ListViewItem(msg)
            {
                ImageIndex = index,
                BackColor = index == 2 ? Color.DarkRed : Color.DarkGreen,
                ForeColor = Color.LightGray,
                Tag = path.Length == 0 ? null : path
            }));
        }

        private void SaveInfo()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(DateTime.Now + ": [保存配置]");
                var list = new List<string>();
                list.Add("执行文件");
                list.Add(txtFileName.Text);
                list.Add("目标目录");
                list.Add(txtDir.Text);
                list.Add("源目录");
                list.AddRange(txtSourceDirs.Lines);
                if (cboConfig.Text.Trim().Length == 0)
                    cboConfig.Text = "Default";
                if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\config\\" + cboConfig.Text.Trim() + "\\"))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\config\\" + cboConfig.Text.Trim() + "\\");
                }
                System.IO.File.WriteAllLines(System.IO.Directory.GetCurrentDirectory() + "\\config\\"+(cboConfig.Text.Trim().Length == 0?"Default":cboConfig.Text)+"\\CopyConfig.cfg", list.ToArray(), Encoding.UTF8);
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
            lsvLog.UpdateUI(()=>lsvLog.Items.Clear());
            int count = 0;
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            ShowLog(DateTime.Now + ": 开始复制文件...", 3);
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
                                    UpdateStatus(string.Format("正在复制 {0}", f));
                                    var tmp = new System.IO.FileInfo(f);
                                    System.IO.File.Copy(f, dir + "\\" + tmp.Name, true);
                                    ShowLog(DateTime.Now + string.Format(": [按扩展名] 从文件{0}, 复制到{1}", f, dir + "\\" + tmp.Name),1);
                                    System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [按扩展名] 从文件{0}, 复制到{1}", f, dir + "\\" + tmp.Name));
                                }
                                catch (Exception ex)
                                {
                                    ShowLog(DateTime.Now + string.Format(": [文件被占用或无权限] {0}, 来自{1}", ex.Message, f),2, dir);
                                    System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [文件被占用或无权限] {0}, 来自{1}", ex.Message, f));
                                }
                            }
                        }
                        catch (Exception ex){
                            ShowLog(DateTime.Now + string.Format(": [目录不存在] {0}, 来自目录{1}, 扩展名{2}", ex.Message, path, tmpFile[i]),2);
                            System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [目录不存在] {0}, 来自目录{1}, 扩展名{2}", ex.Message, path, tmpFile[i]));
                        }
                    }
                    else
                    {
                        if (System.IO.File.Exists(path + tmpFile[i]))
                        {
                            System.Threading.Interlocked.Add(ref count, 1);
                            try
                            {
                                UpdateStatus(string.Format("正在复制 {0}", path + tmpFile[i]));
                                System.IO.File.Copy(path + tmpFile[i], dir + "\\" + tmpFile[i], true);
                                ShowLog(DateTime.Now + string.Format(": [按文件名] 从文件{0}, 复制到{1}", path + tmpFile[i], dir + "\\" + tmpFile[i]),1);
                                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [按文件名] 从文件{0}, 复制到{1}", path + tmpFile[i], dir + "\\" + tmpFile[i]));
                            }
                            catch (Exception ex)
                            {
                                ShowLog(DateTime.Now + string.Format(": [文件被占用或无权限] {0}, 来自源目录{1}, 目标目录{2}", ex.Message, path + tmpFile[i], dir + "\\" + tmpFile[i]),2, path);
                                System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [文件被占用或无权限] {0}, 来自源目录{1}, 目标目录{2}", ex.Message, path + tmpFile[i], dir + "\\" + tmpFile[i]));
                            }
                        }
                        else
                        {
                            ShowLog(DateTime.Now + string.Format(": [文件不存在] {0}", path + tmpFile[i]),2);
                            System.Diagnostics.Trace.WriteLine(DateTime.Now + string.Format(": [文件不存在] {0}", path + tmpFile[i]));
                        }
                    }
                }
                lock (_lock)
                {
                    progressBar1.Invoke(new Action(() => { 
                        progressBar1.Value += 1;
                        btnCopy.Invoke(new Action(() => { if (progressBar1.Value == progressBar1.Maximum || token.IsCancellationRequested)btnCopy.Text = "开始复制(&V)"; }));
                        if (progressBar1.Value == progressBar1.Maximum||token.IsCancellationRequested) 
                        {
                            stopwatch.Stop();
                            UpdateStatus("");
                            ShowLog(DateTime.Now + string.Format(": 复制完成, 共复制文件 {0} 个，耗时 {1}", count, stopwatch.Elapsed.Duration()),0);
                            System.Diagnostics.Debug.WriteLine(DateTime.Now + string.Format(": 复制完成, 共复制文件 {0} 个，耗时 {1}\r\n/************************************************************************************/\r\n",count,stopwatch.Elapsed.Duration()));
                            MessageBox.Show("复制完成");
                            progressBar1.Value = 0;
                        } 
                    }));
                   
                }
            });
        }

        private void UpdateStatus(string msg)
        {
            statusStrip1.UpdateUI(() => { lblStatus.Text = msg; });
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            ReadInfo();
        }

        private void cboConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReadInfo(cboConfig.Text);
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            try
            {
                if (System.IO.Directory.Exists(txtDir.Text))
                    if(System.IO.File.Exists(txtDir.Text.TrimEnd('\\')+"\\"+txtFileName.Text))
                        System.Diagnostics.Process.Start(txtDir.Text.TrimEnd('\\') + "\\" + txtFileName.Text);
                    else
                        System.Diagnostics.Process.Start(txtDir.Text);
            }
            catch { }
        }

        private void lsvLog_DoubleClick(object sender, EventArgs e)
        {
            if (lsvLog.SelectedItems.Count == 0)
                return;
            if (lsvLog.SelectedItems[0].Tag.ToString().Length == 0)
                return;
            try
            {
                System.Diagnostics.Process.Start(lsvLog.SelectedItems[0].Tag.ToString());
            }
            catch { }
        }
    }

    public class CopyTaskParam
    {
        public List<string> FileList { get; set; }
        public string CopyToDir { get; set; }
        public System.Threading.CancellationToken CancelToken { get; set; }
    }
}
public static class ExtentedMethod
{
    public static void UpdateUI(this Control control, Action action)
    {
        if (control.InvokeRequired)
            control.Invoke(action);
        else
            action();
    }
}