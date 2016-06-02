using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using FileTransmitor;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Plz type in a file path.");
            string file = Console.ReadLine();
            //FileTransmiter.Send(FileTransmitor.TestIP, file);
            FileTransmiter.SendWorker worker = new FileTransmiter.SendWorker(FileTransmiter.TestEndPoint);
            FileTransmiter.SupperSend(FileTransmiter.TestEndPoint, file, worker);
            Console.ReadLine();
        }
    }
}