using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GServer.Sockets
{
    public class GSSocket
    {
        private Socket socket;
        private IPEndPoint remoteEndPoint;
        private ConcurrentQueue<byte[]> sendQueue;
        private int isInSending;

        public GSSocket(Socket socket)
        {
            this.socket = socket;
            this.remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.sendQueue = new ConcurrentQueue<byte[]>();
            this.isInSending = 0;
        }

        internal void Enqueue(byte[] data)
        {
            sendQueue.Enqueue(data);
        }

        internal bool Dequeue(out byte[] data)
        {
            return sendQueue.TryDequeue(out data);
        }

        /// <summary>
        /// 发送中标记,当前正在发送时数据先存入队列中按顺序进行发送(保证数据按顺序发送给客户端）
        /// </summary>
        /// <returns></returns>
        internal bool TrySetSendFlag()
        {
            return Interlocked.CompareExchange(ref isInSending, 1, 0) == 0;
        }

        /// <summary>
        /// 发送结束标记
        /// </summary>
        internal void ResetSendFlag()
        {
            Interlocked.Exchange(ref isInSending, 0);
        }

        public Socket WorkSocket { get { return socket; } }
    }
}
