using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 mainform = new WindowsFormsApp1.Form1();
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.AddAppender(mainform);
            BasicConfigurator.Configure(mainform);
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Level.Info;

            Application.Run(mainform);
        }
    }
}
