using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FileTransmitor;
using System.Configuration;
using System.Threading;
using System.Net.Sockets;

namespace TestServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        FileTransmiter.ReceiveWorker worker;

        private void ReportStatus(string id, int percent, bool finished, double elapsedMilliseconds)
        {
            if (this.IsDisposed)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, int, bool, double>(ReportStatus), id, percent, finished, elapsedMilliseconds);
            }
            else
            {
                string file = id.Split('|')[1];
                progressBar1.Value = percent;
                using (SolidBrush time = new SolidBrush(Color.Red))
                {
                    progressBar1.Refresh();
                    using (Graphics g = progressBar1.CreateGraphics())
                    {
                        g.DrawString(Common.MillisecondConvertToSecond(elapsedMilliseconds), new Font("宋体", 9.0F), time, new PointF(4, 10));
                    }
                }
                if (finished)
                {
                    MessageBox.Show("文件[" + file + "]接收完毕！");
                }
            }
        }

        private void ReportSpeed(string id, long speed)
        {
            if (this.IsDisposed)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, long>(ReportSpeed), id, speed);
            }
            else
            {
                string file = id.Split('|')[1];
                label6.Text = file;
                label3.Text = Common.ByteConvertToGBMBKB(speed);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            Listen();
        }

        private void Listen()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(FileTransmiter.RealEndPoint);
                listener.Listen(FileTransmiter.MaxThreadCount);
                while (true)
                {
                    if (listener.Blocking)
                    {
                        Socket client = listener.Accept();
                        if (this.InvokeRequired)
                            this.Invoke(new Action<Socket>(AddClient), client);
                        else
                            AddClient(client);
                        worker = new FileTransmiter.ReceiveWorker(client);
                        FileTransmiter.SupperReceive(client, ConfigurationManager.AppSettings["path"], worker, new Action<string, int, bool, double>(ReportStatus), new Action<string, long>(ReportSpeed));
                    }
                }
            });
        }

        private void AddClient(Socket client)
        {
            listBox1.Items.Add(client.LocalEndPoint.ToString() + "["+client.Handle.ToInt32()+"]");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (worker != null)
                worker.Exit();
        }
    }
}
