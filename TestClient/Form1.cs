using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FileTransmitor;
using System.IO;
using System.Threading;

namespace TestClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        FileTransmiter.SendWorker worker1;
        FileTransmiter.SendWorker worker2;
        FileTransmiter.SendWorker worker3;
        FileTransmiter.SendWorker worker4;
        FileTransmiter.SendWorker worker5;
        FileTransmiter.SendWorker worker6;
        FileTransmiter.SendWorker worker7;
        FileTransmiter.SendWorker worker8;
        FileTransmiter.SendWorker worker9;

        private TextBox GetStatusByPath(string file)
        {
            if (textBox1.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox1;
            else if (textBox2.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox2;
            else if (textBox3.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox3;
            else if (textBox4.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox4;
            else if (textBox5.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox5;
            else if (textBox6.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox6;
            else if (textBox7.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox7;
            else if (textBox8.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox8;
            else if (textBox9.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return textBox9;
            else
                return null;
        }

        private Button GetSpeedByPath(string file)
        {
            if (textBox1.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button10;
            else if (textBox2.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button11;
            else if (textBox3.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button12;
            else if (textBox4.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button13;
            else if (textBox5.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button14;
            else if (textBox6.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button15;
            else if (textBox7.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button16;
            else if (textBox8.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button17;
            else if (textBox9.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return button18;
            else
                return null;
        }

        private Label GetTimeByPath(string file)
        {
            if (textBox1.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label10;
            else if (textBox2.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label11;
            else if (textBox3.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label12;
            else if (textBox4.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label13;
            else if (textBox5.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label14;
            else if (textBox6.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label15;
            else if (textBox7.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label16;
            else if (textBox8.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label17;
            else if (textBox9.Text.Trim().Equals(file, StringComparison.InvariantCultureIgnoreCase))
                return label18;
            else
                return null;
        }

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
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, Color.Green)))
                {
                    if (finished)
                    {
                        brush.Color = Color.FromArgb(50, Color.Blue);
                        Button btn = GetSpeedByPath(file);
                        btn.Text = "发送";
                    }
                    TextBox tb = GetStatusByPath(file);
                    int width = tb.Width * percent / 100;
                    tb.Refresh();
                    using (Graphics g = tb.CreateGraphics())
                    {
                        g.FillRectangle(brush, new Rectangle(0, 0, width, tb.Height));
                    }
                }
                Label lbl = GetTimeByPath(file);
                lbl.Text = Common.MillisecondConvertToSecond(elapsedMilliseconds);
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
                Button btn = GetSpeedByPath(file);
                btn.Text = Common.ByteConvertToGBMBKB(speed) + "/S";
            }
        }

        private void SendFile(string file, FileTransmiter.SendWorker worker)
        {
            if (File.Exists(file))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(Send), new SendArgs(file, worker));
            }
            else
            {
                MessageBox.Show("指定的文件[" + file + "]不存在！");
            }
        }

        public class SendArgs
        {
            string file;

            public string File
            {
                get { return file; }
                set { file = value; }
            }
            FileTransmiter.SendWorker worker;

            public FileTransmiter.SendWorker Worker
            {
                get { return worker; }
                set { worker = value; }
            }
            public SendArgs(string file, FileTransmiter.SendWorker worker)
            {
                this.file = file;
                this.worker = worker;
            }
        }

        private void Send(object o)
        {
            SendArgs arg = o as SendArgs;
            FileTransmiter.SupperSend(FileTransmiter.RealEndPoint, arg.File, arg.Worker, new Action<string, int, bool, double>(ReportStatus), new Action<string, long>(ReportSpeed));
        }

        //private void CancelSend(string file, int id)
        //{
        //    switch (id)
        //    {
        //        case 1:
        //            if (worker1 != null)
        //                worker1.Exit();
        //            break;
        //        case 2:
        //            if (worker2 != null)
        //                worker2.Exit();
        //            break;
        //        case 3:
        //            if (worker3 != null)
        //                worker3.Exit();
        //            break;
        //        case 4:
        //            if (worker4 != null)
        //                worker4.Exit();
        //            break;
        //        case 5:
        //            if (worker5 != null)
        //                worker5.Exit();
        //            break;
        //        case 6:
        //            if (worker6 != null)
        //                worker6.Exit();
        //            break;
        //        case 7:
        //            if (worker7 != null)
        //                worker7.Exit();
        //            break;
        //        case 8:
        //            if (worker8 != null)
        //                worker8.Exit();
        //            break;
        //        case 9:
        //            if (worker9 != null)
        //                worker9.Exit();
        //            break;
        //    }
        //}

        private void button10_Click(object sender, EventArgs e)
        {
            string file = textBox1.Text.Trim();
            if (button10.Text == "发送")
            {
                SendFile(file, worker1);
                button1.Enabled = false;
                button10.Enabled = false;
            }
            //else
            //    CancelSend(file, 1);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string file = textBox2.Text.Trim();
            if (button11.Text == "发送")
            {
                SendFile(file, worker2);
                button2.Enabled = false;
                button11.Enabled = false;
            }
            //else
            //    CancelSend(file, 2);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string file = textBox3.Text.Trim();
            if (button12.Text == "发送")
            {
                SendFile(file, worker3);
                button3.Enabled = false;
                button12.Enabled = false;
            }
            //else
            //    CancelSend(file, 3);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string file = textBox4.Text.Trim();
            if (button13.Text == "发送")
            {
                SendFile(file, worker4);
                button4.Enabled = false;
                button13.Enabled = false;
            }
            //else
            //    CancelSend(file, 4);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            string file = textBox5.Text.Trim();
            if (button14.Text == "发送")
            {
                SendFile(file, worker5);
                button5.Enabled = false;
                button14.Enabled = false;
            }
            //else
            //    CancelSend(file, 5);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            string file = textBox6.Text.Trim();
            if (button15.Text == "发送")
            {
                SendFile(file, worker6);
                button6.Enabled = false;
                button15.Enabled = false;
            }
            //else
            //    CancelSend(file, 6);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            string file = textBox7.Text.Trim();
            if (button16.Text == "发送")
            {
                SendFile(file, worker7);
                button7.Enabled = false;
                button16.Enabled = false;
            }
            //else
            //    CancelSend(file, 7);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            string file = textBox8.Text.Trim();
            if (button17.Text == "发送")
            {
                SendFile(file, worker8);
                button8.Enabled = false;
                button17.Enabled = false;
            }
            //else
            //    CancelSend(file, 8);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            string file = textBox9.Text.Trim();
            if (button18.Text == "发送")
            {
                SendFile(file, worker9);
                button9.Enabled = false;
                button18.Enabled = false;
            }
            //else
            //    CancelSend(file, 9);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox4.Text = openFileDialog1.FileName;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox5.Text = openFileDialog1.FileName;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox6.Text = openFileDialog1.FileName;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox7.Text = openFileDialog1.FileName;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox8.Text = openFileDialog1.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox9.Text = openFileDialog1.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            worker1 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker2 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker3 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker4 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker5 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker6 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker7 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker8 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
            worker9 = new FileTransmiter.SendWorker(FileTransmiter.RealEndPoint);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //string file1 = textBox1.Text.Trim();
            //CancelSend(file1, 1);
            //string file2 = textBox2.Text.Trim();
            //CancelSend(file2, 2);
            //string file3 = textBox3.Text.Trim();
            //CancelSend(file3, 3);
            //string file4 = textBox4.Text.Trim();
            //CancelSend(file4, 4);
            //string file5 = textBox5.Text.Trim();
            //CancelSend(file5, 5);
            //string file6 = textBox6.Text.Trim();
            //CancelSend(file6, 6);
            //string file7 = textBox7.Text.Trim();
            //CancelSend(file7, 7);
            //string file8 = textBox8.Text.Trim();
            //CancelSend(file8, 8);
            //string file9 = textBox9.Text.Trim();
            //CancelSend(file9, 9);
        }
    }
}
