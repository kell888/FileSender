using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileTransmitor;
using System.Net.Sockets;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //FileTransmiter.Receive(FileTransmitor.TestIP, @"E:\");//接受放到那个盘里 保存路径=这个参数+发送到文件名。
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(FileTransmiter.TestEndPoint);
            listener.Listen(FileTransmiter.MaxThreadCount);
            FileTransmiter.ReceiveWorker worker = new FileTransmiter.ReceiveWorker(listener.Accept());
            FileTransmiter.SupperReceive(listener, @"F:\Test", worker);
            Console.ReadLine();
        }
    }
}