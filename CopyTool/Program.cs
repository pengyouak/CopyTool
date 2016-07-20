using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CopyTool
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var config = System.Configuration.ConfigurationManager.AppSettings["LOG"];
            if (config != null && config.ToString() == "1")
            {
                System.Diagnostics.Trace.Listeners.Clear();
                System.Diagnostics.Trace.AutoFlush = true;
                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener("CopyTool.log"));
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
