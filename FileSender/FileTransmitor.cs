#define Sleep
#undef Sleep
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;

namespace FileTransmitor
{
    public static class FileTransmiter
    {
        #region NestedType
        public class SendWorker : IWorker
        {
            private long totalSent, totalSend;
            private byte[] buffer;
            private Socket sock;
            private FileStream reader;
            private Thread thread;
            private bool isFinished;
            //volatile bool exit;

            //public void Exit()
            //{
            //    exit = true;
            //}

            public long TotalSent
            {
                get { return totalSent; }
            }
            public long TotalSend
            {
                get { return totalSend; }
            }
            public byte[] Buffer
            {
                get { return buffer; }
            }
            public Socket Client
            {
                get { return sock; }
            }
            public bool IsFinished
            {
                get { return isFinished; }
            }

            public SendWorker(IPEndPoint ip)
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                buffer = new byte[BufferSize];
            }
            public void Initialize(string path, long position, long length)
            {
                Initialize(path, position, length, 0L, length);
            }
            public void Initialize(string path, long position, long length, long worked, long total)
            {
                reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                reader.Position = position + worked;
                totalSent = worked;
                totalSend = total;
                thread = new Thread(new ParameterizedThreadStart(Work));
                thread.IsBackground = true;
            }
            private void Work(object obj)
            {
                try
                {
                    int read, sent;
                    bool flag;
                    while (totalSent < totalSend)
                    {
                        //if (exit)
                        //{
                        //    Console.WriteLine("发送端退出...");
                        //    break;
                        //}
                        read = reader.Read(buffer, 0, Math.Min(BufferSize, (int)(totalSend - totalSent)));
                        sent = 0;
                        flag = true;
                        while ((sent += sock.Send(buffer, sent, read, SocketFlags.None)) < read)
                        {
                            flag = false;
                            totalSent += (long)sent;
#if Sleep
                            Thread.Sleep(100);
#endif
                        }
                        if (flag)
                        {
                            totalSent += (long)read;
#if Sleep
                            Thread.Sleep(100);
#endif
                        }
                    }
                    reader.Dispose();
                    //sock.Shutdown(SocketShutdown.Send);
                    //sock.Close();
                    EventWaitHandle waitHandle = obj as EventWaitHandle;
                    if (waitHandle != null)
                    {
                        waitHandle.Set();
                    }
                    isFinished = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("发送线程出错：" + e.Message);
                }
            }

            public void ReportProgress(out long worked, out long total)
            {
                worked = totalSent;
                total = totalSend;
            }

            public void RunWork(EventWaitHandle waitHandle)
            {
                thread.Start(waitHandle);
            }
        }

        public class ReceiveWorker : IWorker
        {
            private long offset, totalReceived, totalReceive;
            private byte[] buffer;
            private Socket sock;
            private FileStream writer;
            private Thread thread;
            private bool isFinished;
            volatile bool exit;

            public void Exit()
            {
                exit = true;
            }

            public long TotalReceived
            {
                get { return totalReceived; }
            }
            public long TotalReceive
            {
                get { return totalReceive; }
            }
            public byte[] Buffer
            {
                get { return buffer; }
            }
            public Socket Client
            {
                get { return sock; }
            }
            public bool IsFinished
            {
                get { return isFinished; }
            }

            public ReceiveWorker(Socket client)
            {
                sock = client;
                
                buffer = new byte[BufferSize];
            }
            public void Initialize(string path, long position, long length)
            {
                Initialize(path, position, length, 0L, length);
            }
            public void Initialize(string path, long position, long length, long worked, long total)
            {
                writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Write);
                writer.Position = position + worked;
                writer.Lock(position, length);
                offset = position;
                totalReceived = worked;
                totalReceive = total;
                thread = new Thread(new ParameterizedThreadStart(Work));
                thread.IsBackground = true;
            }
            private void Work(object obj)
            {
                try
                {
                    int received;
                    while (totalReceived < totalReceive)
                    {
                        if (exit)
                        {
                            Console.WriteLine("接收端退出...");
                            break;
                        }
                        if ((received = sock.Receive(buffer)) == 0)
                        {
                            break;
                        }
                        writer.Write(buffer, 0, received);
                        writer.Flush();
                        totalReceived += (long)received;
#if Sleep
                        Thread.Sleep(100);
#endif
                    }
                    writer.Unlock(offset, totalReceive);
                    writer.Dispose();
                    //sock.Shutdown(SocketShutdown.Both);
                    //sock.Close();
                    EventWaitHandle waitHandle = obj as EventWaitHandle;
                    if (waitHandle != null)
                    {
                        waitHandle.Set();
                    }
                    isFinished = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("接收线程出错：" + e.Message);
                }
            }

            public void ReportProgress(out long worked, out long total)
            {
                worked = totalReceived;
                total = totalReceive;
            }

            public void RunWork(EventWaitHandle waitHandle)
            {
                thread.Start(waitHandle);
            }
        }

        private interface IWorker
        {
            bool IsFinished { get; }
            void Initialize(string path, long position, long length);
            void Initialize(string path, long position, long length, long worked, long total);
            void ReportProgress(out long worked, out long total);
            void RunWork(EventWaitHandle waitHandle);
        }
        #endregion

        #region Field
        public const int BufferSize = 1024;
        public const int PerLongCount = sizeof(long);
        public const int MinThreadCount = 1;
        public const int MaxThreadCount = 9;
        public const string PointExtension = ".dat";
        public const string TempExtension = ".temp";
        private const long SplitSize = 1024L * 1024L * 100L;//文件分块：100MB为一块
        public static readonly IPEndPoint TestEndPoint;
        public static IPEndPoint RealEndPoint
        {
            get
            {
                string ip = ConfigurationManager.AppSettings["ip"];
                string port = ConfigurationManager.AppSettings["port"];
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                    return ep;
                }
                catch
                {
                    return TestEndPoint;
                }
            }
        }
        #endregion

        #region Constructor
        static FileTransmiter()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            TestEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 520);
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StreamWriter writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), true, Encoding.Default);
            writer.Write("Time:");
            writer.Write(DateTime.Now.ToShortTimeString());
            writer.Write(". ");
            writer.WriteLine(e.ExceptionObject);
            writer.WriteLine("CLR IsTerminating:" + e.IsTerminating);
            writer.Dispose();
        }
        #endregion
        
        #region Single
        public static void Send(IPEndPoint ip, string path)
        {
            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            sock.Connect(ip);
            byte[] buffer = new byte[BufferSize];
            using (FileStream reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                long send, length = reader.Length;
                Buffer.BlockCopy(BitConverter.GetBytes(length), 0, buffer, 0, PerLongCount);
                string fileName = Path.GetFileName(path);
                sock.Send(buffer, 0, PerLongCount + Encoding.Default.GetBytes(fileName, 0, fileName.Length, buffer, PerLongCount), SocketFlags.None);
                Console.WriteLine("Sending file:" + fileName + ".Plz wait...");
                sock.Receive(buffer);
                reader.Position = send = BitConverter.ToInt64(buffer, 0);
                int read, sent;
                bool flag;
                while ((read = reader.Read(buffer, 0, BufferSize)) != 0)
                {
                    sent = 0;
                    flag = true;
                    while ((sent += sock.Send(buffer, sent, read, SocketFlags.None)) < read)
                    {
                        flag = false;
                        send += (long)sent;
#if Sleep
                        Thread.Sleep(100);
#endif
                    }
                    if (flag)
                    {
                        send += (long)read;
#if Sleep
                        Thread.Sleep(100);
#endif
                    }
                }
            }
            sock.Shutdown(SocketShutdown.Send);
            //sock.Close();
            watcher.Stop();
            Console.WriteLine("Send finish.Span Time:" + watcher.Elapsed.TotalMilliseconds + " ms.");
        }

        public static void Receive(IPEndPoint ip, string path)
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ip);
            listener.Listen(MinThreadCount);
            Socket client = listener.Accept();
            
            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            byte[] buffer = new byte[BufferSize];
            int received = client.Receive(buffer);
            long receive, length = BitConverter.ToInt64(buffer, 0);
            string fileName = Encoding.Default.GetString(buffer, PerLongCount, received - PerLongCount);
            Console.WriteLine("Receiveing file:" + fileName + ".Plz wait...");
            FileInfo file = new FileInfo(Path.Combine(path, fileName));
            using (FileStream writer = file.Open(file.Exists ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                receive = writer.Length;
                client.Send(BitConverter.GetBytes(receive));
                while (receive < length)
                {
                    if ((received = client.Receive(buffer)) == 0)
                    {
                        Console.WriteLine("Send Stop.");
                        return;
                    }
                    writer.Write(buffer, 0, received);
                    writer.Flush();
                    receive += (long)received;
#if Sleep
                    Thread.Sleep(100);
#endif
                }
            }
            //client.Shutdown(SocketShutdown.Both);
            //client.Close();
            watcher.Stop();
            Console.WriteLine("Receive finish.Span Time:" + watcher.Elapsed.TotalMilliseconds + " ms.");
        }
        #endregion

        #region Supper
        #region Extensions
        private static int ReportProgress(this IWorker[] workers, out long worked, out long total)
        {
            worked = total = 0L;
            long w, t;
            foreach (IWorker worker in workers)
            {
                worker.ReportProgress(out w, out t);
                worked += w;
                total += t;
            }
            return (int)Math.Round(worked * 100.0 / total);
        }
        private static long ReportSpeed(this IWorker[] workers, ref long oldValue, out int percent)
        {
            long w, t;
            percent = workers.ReportProgress(out w, out t);
            long speed = (w - oldValue);
            oldValue = w;
            return speed;
        }
        private static bool IsAllFinished(this IWorker[] workers)
        {
            bool flag = true;
            foreach (IWorker worker in workers)
            {
                if (!worker.IsFinished)
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }
        #endregion

        #region Helper
        public static void Write(long value, byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)value;
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 0x10);
            buffer[offset++] = (byte)(value >> 0x18);
            buffer[offset++] = (byte)(value >> 0x20);
            buffer[offset++] = (byte)(value >> 40);
            buffer[offset++] = (byte)(value >> 0x30);
            buffer[offset] = (byte)(value >> 0x38);
        }
        public static void Read(out long value, byte[] buffer, int offset)
        {
            uint num = (uint)(((buffer[offset++] | (buffer[offset++] << 8)) | (buffer[offset++] << 0x10)) | (buffer[offset++] << 0x18));
            uint num2 = (uint)(((buffer[offset++] | (buffer[offset++] << 8)) | (buffer[offset++] << 0x10)) | (buffer[offset] << 0x18));
            value = (long)((num2 << 0x20) | num);
        }
        #endregion

        public static int GetThreadCount(long fileSize)
        {
            int count = (int)(fileSize / SplitSize);
            if (count < MinThreadCount)
            {
                count = MinThreadCount;
            }
            else if (count > MaxThreadCount)
            {
                count = MaxThreadCount;
            }
            return count;
        }

        public static void SupperSend(IPEndPoint ip, string path, SendWorker worker, Action<string, int, bool, double> statusHandler = null, Action<string, long> speedHandler = null)
        {
            try
            {
                Stopwatch watcher = new Stopwatch();
                watcher.Start();
                FileInfo file = new FileInfo(path);
#if DEBUG
                if (!file.Exists)
                {
                    throw new FileNotFoundException();
                }
#endif
                long fileLength = file.Length;
                if (!worker.Client.Connected)
                    worker.Client.Connect(ip);
                Buffer.BlockCopy(BitConverter.GetBytes(fileLength), 0, worker.Buffer, 0, PerLongCount);
                string fileName = file.Name;
                worker.Client.Send(worker.Buffer, 0, PerLongCount + Encoding.Default.GetBytes(fileName, 0, fileName.Length, worker.Buffer, PerLongCount), SocketFlags.None);
                Console.WriteLine("Sending file:" + fileName + ".Plz wait...");
                int threadCount = GetThreadCount(fileLength);
                SendWorker[] workers = new SendWorker[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    workers[i] = i == 0 ? worker : new SendWorker(ip);
                }

                int perPairCount = PerLongCount * 2, count = perPairCount * threadCount;
                byte[] bufferInfo = new byte[count];
                long oddSize, avgSize = Math.DivRem(fileLength, (long)threadCount, out oddSize);
                if (worker.Client.Receive(bufferInfo) == 4)
                {
                    for (int i = 0; i < threadCount; i++)
                    {
                        workers[i].Initialize(path, i * avgSize, i == threadCount - 1 ? avgSize + oddSize : avgSize);
                    }
                }
                else
                {
                    long w, t;
                    for (int i = 0; i < threadCount; i++)
                    {
                        Read(out w, bufferInfo, i * perPairCount);
                        Read(out t, bufferInfo, i * perPairCount + PerLongCount);
                        workers[i].Initialize(path, i * avgSize, i == threadCount - 1 ? avgSize + oddSize : avgSize, w, t);
                    }
                }

                AutoResetEvent reset = new AutoResetEvent(true);
                for (int i = 0; i < threadCount; i++)
                {
                    workers[i].RunWork(i == threadCount - 1 ? reset : null);
                }
                reset.WaitOne();

                string id = ip.ToString() + "|" + path;
                long speed;
                long diff;
                long value = 0L;
                int percent;
                double ms0 = watcher.ElapsedMilliseconds;
                do
                {
                    Thread.Sleep(500);
                    diff = workers.ReportSpeed(ref value, out percent);
                    double ms1 = watcher.ElapsedMilliseconds;
                    speed = (long)((diff * 1000) / (ms1 - ms0));
                    Console.WriteLine("waiting for other threads. Progress:" + value + "/" + fileLength + ";Speed:" + Common.ByteConvertToGBMBKB(speed) + "/S.");
                    //int percent = (int)Math.Round(value * 100.0 / fileLength);
                    if (speedHandler != null)
                        speedHandler.Invoke(id, speed);
                    if (statusHandler != null)
                        statusHandler.Invoke(id, percent, false, ms1);
                    ms0 = watcher.ElapsedMilliseconds;
                }
                while (!workers.IsAllFinished());

                watcher.Stop();
                Console.WriteLine("Send finish.Span Time:" + watcher.Elapsed.TotalMilliseconds + " ms.");
                if (speedHandler != null)
                    speedHandler.Invoke(id, speed);
                if (statusHandler != null)
                    statusHandler.Invoke(id, 100, true, watcher.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine("发送出错：" + e.Message);
            }
        }

        public static void SupperReceive(Socket client, string path, ReceiveWorker worker, Action<string, int, bool, double> statusHandler = null, Action<string, long> speedHandler = null)
        {
            try
            {
                Stopwatch watcher = new Stopwatch();
                watcher.Start();

                int recv = worker.Client.Receive(worker.Buffer);
                long fileLength = BitConverter.ToInt64(worker.Buffer, 0);
                string fileName = Encoding.Default.GetString(worker.Buffer, PerLongCount, recv - PerLongCount);
                Console.WriteLine("Receiveing file:" + fileName + ".Plz wait...");
                int threadCount = GetThreadCount(fileLength);
                ReceiveWorker[] workers = new ReceiveWorker[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    if (i == 0)
                    {
                        workers[i] = worker;
                    }
                    else
                    {
                        workers[i] = new ReceiveWorker(client);
                    }
                }

                int perPairCount = PerLongCount * 2, count = perPairCount * threadCount;
                byte[] bufferInfo = new byte[count];
                string filePath = Path.Combine(path, fileName), pointFilePath = filePath + PointExtension, tempFilePath = filePath + TempExtension;
                FileStream pointStream;
                long oddSize, avgSize = Math.DivRem(fileLength, (long)threadCount, out oddSize);
                if (File.Exists(pointFilePath) && File.Exists(tempFilePath))
                {
                    pointStream = new FileStream(pointFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    pointStream.Read(bufferInfo, 0, count);
                    long w, t;
                    for (int i = 0; i < threadCount; i++)
                    {
                        Read(out w, bufferInfo, i * perPairCount);
                        Read(out t, bufferInfo, i * perPairCount + PerLongCount);
                        workers[i].Initialize(tempFilePath, i * avgSize, i == threadCount - 1 ? avgSize + oddSize : avgSize, w, t);
                    }
                    worker.Client.Send(bufferInfo);
                }
                else
                {
                    pointStream = new FileStream(pointFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    FileStream stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                    stream.SetLength(fileLength);
                    stream.Flush();
                    stream.Dispose();
                    for (int i = 0; i < threadCount; i++)
                    {
                        workers[i].Initialize(tempFilePath, i * avgSize, i == threadCount - 1 ? avgSize + oddSize : avgSize);
                    }
                    worker.Client.Send(bufferInfo, 0, 4, SocketFlags.None);
                }
                Timer timer = new Timer(state =>
                {
                    long w, t;
                    for (int i = 0; i < threadCount; i++)
                    {
                        workers[i].ReportProgress(out w, out t);
                        Write(w, bufferInfo, i * perPairCount);
                        Write(t, bufferInfo, i * perPairCount + PerLongCount);
                    }
                    pointStream.Position = 0L;
                    pointStream.Write(bufferInfo, 0, count);
                    pointStream.Flush();

                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));                

                AutoResetEvent reset = new AutoResetEvent(true);
                for (int i = 0; i < threadCount; i++)
                {
                    workers[i].RunWork(i == threadCount - 1 ? reset : null);
                }
                reset.WaitOne();

                string id = client.LocalEndPoint.ToString() + "|" + fileName;
                long speed;
                long diff;
                long value = 0L;
                int percent;
                double ms0 = watcher.ElapsedMilliseconds;
                do
                {
                    Thread.Sleep(500);
                    diff = workers.ReportSpeed(ref value, out percent);
                    double ms1 = watcher.ElapsedMilliseconds;
                    speed = (long)((diff * 1000) / (ms1 - ms0));
                    Console.WriteLine("waiting for other threads. Progress:" + value + "/" + fileLength + ";Speed:" + Common.ByteConvertToGBMBKB(speed) + "/S.");
                    //int percent = (int)Math.Round(value * 100.0 / fileLength);
                    if (speedHandler != null)
                        speedHandler.Invoke(id, speed);
                    if (statusHandler != null)
                        statusHandler.Invoke(id, percent, false, ms1);
                    ms0 = watcher.ElapsedMilliseconds;
                }
                while (!workers.IsAllFinished());

                timer.Dispose();
                pointStream.Dispose();
                File.Delete(pointFilePath);
                File.Copy(tempFilePath, filePath, true);
                File.Delete(tempFilePath);

                watcher.Stop();
                Console.WriteLine("Receive finish.Span Time:" + watcher.Elapsed.TotalMilliseconds + " ms.");
                if (speedHandler != null)
                    speedHandler.Invoke(id, speed);
                if (statusHandler != null)
                    statusHandler.Invoke(id, 100, true, watcher.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine("接收出错：" + e.Message);
            }
        }
        #endregion
    }
    public static class Common
    {
        const int GB = 1024 * 1024 * 1024;//定义GB的计算常量
        const int MB = 1024 * 1024;       //定义MB的计算常量
        const int KB = 1024;              //定义KB的计算常量

        const int SCD=1000;
        const int MNT=1000*60;
        const int HUR=1000*60*60;

        public static string ByteConvertToGBMBKB(long size)
        {
            if (size / GB >= 1)//如果当前Byte的值大于等于1GB
                return (Math.Round(size / (float)GB, 2)) + "GB";//将其转换成GB
            else if (size / MB >= 1)//如果当前Byte的值大于等于1MB
                return (Math.Round(size / (float)MB, 2)) + "MB";//将其转换成MB
            else if (size / KB >= 1)//如果当前Byte的值大于等于1KB
                return (Math.Round(size / (float)KB, 2)) + "KB";//将其转换成KB
            else
                return size + "Byte";//显示Byte值
        }

        public static string MillisecondConvertToSecond(double ms)
        {
            string result = "";
                long r;
                long d;
            long MS = (long)Math.Round(ms);
            if (MS / HUR >= 1)//如果当前的值大于等于1小时
            {
                d = Math.DivRem(MS, HUR, out r);
                string h = d + "小时";
                result += h;
                if (r / MNT >= 1)//如果余数大于等于1分
                {
                    d = Math.DivRem(r, MNT, out r);
                    string m = d + "分";
                    result += m;
                    if (r / SCD >= 1)//如果余数大于等于1秒
                    {
                        string s = Math.Round(r / (float)SCD, 2) + "秒";
                        result += s;
                    }
                }
            }
            else if (MS / MNT >= 1)//如果当前的值大于等于1分
            {
                d = Math.DivRem(MS, MNT, out r);
                string m = d + "分";
                result += m;
                if (r / SCD >= 1)//如果余数大于等于1秒
                {
                    string s = Math.Round(r / (float)SCD, 2) + "秒";
                    result += s;
                }
            }
            else if (MS / SCD >= 1)//如果当前的值大于等于1秒
            {
                result = Math.Round(MS / (float)SCD, 2) + "秒";
            }
            else
            {
                result = MS + "毫秒";//显示毫秒值
            }
            return result;
        }
    }
}