using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GServer.Sockets
{
    public class SocketSettings
    {
        public SocketSettings()
        {
            MaxConnections = 1024;
            NumOfRecSend = 2048;
            Backlog = 5;
            MaxAcceptOps = 5;
            BufferSize = 8192;
            LocalEndPoint = new IPEndPoint(IPAddress.Any, 11000);
        }
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections { get; private set; }

        /// <summary>
        /// IO数量
        /// </summary>
        public int NumOfRecSend { get; private set; }

        /// <summary>
        /// The maximum length of the pending connections queue.
        /// </summary>
        public int Backlog { get; private set; }

        /// <summary>
        /// max accept ops
        /// </summary>
        public int MaxAcceptOps { get; private set; }

        /// <summary>
        /// IO缓存大小
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// 本地地址
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }
    }
}
