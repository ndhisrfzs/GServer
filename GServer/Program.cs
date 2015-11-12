using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GServer.Sockets;

namespace GServer
{
    class Program
    {
        static Dictionary<int, Coroutine> waits = new Dictionary<int, Coroutine>();
        static void Main(string[] args)
        {
            Coroutine co = new Coroutine(test());
            waits[co.session] = co;
            while (true)
            {
                string cmds = Console.ReadLine();
                string[] messages = cmds.Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries);
                int session = int.Parse(messages[0]);
                Coroutine c = null;
                if (waits.TryGetValue(session, out c))
                {
                    c.Resume(messages[1]);
                }
            }
        }

        static IEnumerator test()
        {
            SocketListener sl = new SocketListener(new SocketSettings());
            sl.StartListen();
            //ServerSocket socket = new ServerSocket("127.0.0.1", 11000);
            //socket.Start();
            int i = 1;
            Console.WriteLine("step "+ i++);
            yield return Call("call 1");
            Console.WriteLine("Data:" + Thread.GetData(Thread.GetNamedDataSlot("slot")));           
            Console.WriteLine("step " + i++);
        }

        static int session = 0;
        static int Call(string message)
        {
            Console.WriteLine(message);
            return Interlocked.Increment(ref session);
        }
    }
    public class MyData
    {
        public int index { get; set; }
        public string str { get; set; }
    }
}
