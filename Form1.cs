using log4net.Appender;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net.Core;
using log4net;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form, IAppender
    {
        private  ILog _logger = LogManager.GetLogger(typeof(Form1));
        private string extension;
        private string path;
        private string paths = "D:\\转译文件";
        private string currentMp3 = "";
        public Form1()
        {
            InitializeComponent();
            this.linkLabel1.Click += LinkLabel1_Click;
        }

        private void LinkLabel1_Click(object sender, EventArgs e)
        {
            Process.Start(this.currentMp3);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;//设置拖动操作
        }
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            paths = textBox1.Text;//输入保存路径给paths
        }



        private void Form1_DragDrop(object sender, DragEventArgs e)
        {


            path = (((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());//获取拖动的文件路径
            _logger.Debug(path);

            new Thread(() =>
            {
                Decryptor.Instance.AutoRename = true;
                Decryptor.Instance.TargetDirectory = this.paths;
                int success = Decryptor.Instance.Process(path);
                _logger.Debug("成功转换" + success + "个文件");
            }).Start();
           
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = folderBrowserDialog1.SelectedPath;
                paths = textBox1.Text;
            }
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            richTextBox1.BeginInvoke((Action)(() =>
            {
                richTextBox1.AppendText(loggingEvent.MessageObject.ToString() + Environment.NewLine);
            }));
           
        }
    }
}

