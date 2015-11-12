using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GClient
{
    class Program
    {
        private static byte[] result = new byte[1024]; 
        static void Main(string[] args)
        {
            //设定服务器IP地址  
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(ip, 11000)); //配置服务器IP与端口  
            clientSocket.Send(Encoding.ASCII.GetBytes("client send Message Hellp" + DateTime.Now));
            byte[] buff = new byte[8192];
            int len = clientSocket.Receive(buff);
            Console.WriteLine("Len:"+len);
            Console.WriteLine("Send step 1");
            Console.ReadLine();
            clientSocket.Send(Encoding.ASCII.GetBytes("client send Message" + DateTime.Now));
            Console.WriteLine("Send step 2");
            Console.ReadLine();

           
            //try
            //{
            //    ArrayList clients = new ArrayList();
            //    for (int i = 0; i < 5000; i++)
            //    {
            //        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //        clientSocket.Connect(new IPEndPoint(ip, 5150)); //配置服务器IP与端口  
            //        clients.Add(clientSocket);
            //        Console.Clear();
            //        Console.WriteLine(i);
            //        Thread.Sleep(1);
            //        //Console.WriteLine("连接服务器成功");
            //    }
            //    Random rnd = new Random();
            //    while(true)
            //    {
            //        string sendMessage = "client send Message Hellp" + DateTime.Now;
            //        (clients[rnd.Next(clients.Count)] as Socket).Send(Encoding.ASCII.GetBytes(sendMessage));
            //        Thread.Sleep(1);
            //    }
            //}
            //catch 
            //{
            //    Console.WriteLine("连接服务器失败，请按回车键退出！");
            //    return;
            //}





            ////通过clientSocket接收数据  
            ////int receiveLength = clientSocket.Receive(result);
            ////Console.WriteLine("接收服务器消息：{0}", Encoding.ASCII.GetString(result, 0, receiveLength));
            ////通过 clientSocket 发送数据  
            //for (int i = 0; i < 10; i++)
            //{
            //    try
            //    {
            //        Thread.Sleep(500);    //等待1秒钟  
            //        string sendMessage = "client send Message Hellp" + DateTime.Now;
            //        clientSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
            //        Console.WriteLine("向服务器发送消息：{0}" + sendMessage);
            //    }
            //    catch
            //    {
            //        clientSocket.Shutdown(SocketShutdown.Both);
            //        clientSocket.Close();
            //        break;
            //    }
            //}
            //Console.WriteLine("发送完毕，按回车键退出");
            //Console.ReadLine();  
        }
    }
}
