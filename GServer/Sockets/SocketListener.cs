using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GServer.Sockets
{
    public class SocketListener
    {
        private BufferManager bufferManager;                                    //缓存管理器
        private Socket listenSocket;                                            //监听Socket
        private Semaphore maxConnectionsEnforcer;                               //最大连接数量控制
        private SocketSettings socketSettings;                                  //Socket设置项

        private ThreadSafeStack<SocketAsyncEventArgs> acceptEventArgsPool;      //Accept池
        private ThreadSafeStack<SocketAsyncEventArgs> ioEventArgsPool;          //IO事件池

        public SocketListener(SocketSettings socketSettings)
        {
            this.socketSettings = socketSettings;
            this.bufferManager = new BufferManager(socketSettings.BufferSize * socketSettings.NumOfRecSend, socketSettings.BufferSize);
            this.ioEventArgsPool = new ThreadSafeStack<SocketAsyncEventArgs>(socketSettings.NumOfRecSend);
            this.acceptEventArgsPool = new ThreadSafeStack<SocketAsyncEventArgs>(socketSettings.MaxAcceptOps);
            this.maxConnectionsEnforcer = new Semaphore(socketSettings.MaxConnections, socketSettings.MaxConnections);
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            //初始化Buffer(申请Buffer内存)
            bufferManager.InitBuffer();     

            //填充Accept池
            for (int i = 0; i < socketSettings.MaxAcceptOps; i++)
            {
                acceptEventArgsPool.Push(CreateAcceptEventArgs());
            }

            //填充IO池
            SocketAsyncEventArgs ioEventArgs;
            for (int i = 0; i < socketSettings.NumOfRecSend; i++)
            {
                ioEventArgs = new SocketAsyncEventArgs();
                bufferManager.SetBuffer(ioEventArgs);       //设置IO缓冲区
                ioEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                DataToken dataToken = new DataToken();
                dataToken.bufferOffset = ioEventArgs.Offset;
                ioEventArgs.UserToken = dataToken;
                ioEventArgsPool.Push(ioEventArgs);
            }
        }

        /// <summary>
        /// 创建accept异步事件参数
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateAcceptEventArgs()
        {
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
            return acceptEventArg;
        }

        /// <summary>
        /// 开始监听本地端口
        /// </summary>
        public void StartListen()
        {
            listenSocket = new Socket(socketSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenSocket.Bind(socketSettings.LocalEndPoint);
            listenSocket.Listen(socketSettings.Backlog);
            PostAccept();
        }

        /// <summary>
        /// 投递Accept
        /// </summary>
        private void PostAccept()
        {
            try
            {
                SocketAsyncEventArgs acceptEventArgs = acceptEventArgsPool.Pop() ?? CreateAcceptEventArgs();
                bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArgs);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 处理Accept
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                maxConnectionsEnforcer.WaitOne(); 
                if (acceptEventArgs.SocketError != SocketError.Success)
                {
                    HandleBadAccept(acceptEventArgs);
                }
                else
                {
                    //投递接收事件
                    SocketAsyncEventArgs ioEventArgs = ioEventArgsPool.Pop(); 
                    ioEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;
                    DataToken dataToken = (DataToken)ioEventArgs.UserToken;
                    ioEventArgs.SetBuffer(dataToken.bufferOffset, socketSettings.BufferSize);
                    GSSocket gsSocket = new GSSocket(ioEventArgs.AcceptSocket);
                    dataToken.Socket = gsSocket;
                    acceptEventArgs.AcceptSocket = null;
                    PostReceive(ioEventArgs);

                    //释放acceptArgs
                    ReleaseAccept(acceptEventArgs, false);
                }
            }
            finally
            {
                //投递下一个Accept事件
                PostAccept();
            }
        }
        
        /// <summary>
        /// 操作错误Accept
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        private void HandleBadAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            CloseSocket(acceptEventArgs);
            ReleaseAccept(acceptEventArgs);
        }

        /// <summary>
        /// 关闭Socket
        /// </summary>
        /// <param name="eventArgs"></param>
        private void CloseSocket(SocketAsyncEventArgs eventArgs)
        {
            try
            {
                if (eventArgs.AcceptSocket != null)
                {
                    eventArgs.AcceptSocket.Close();
                }
            }
            finally
            {
                eventArgs.AcceptSocket = null;
            }
        }

        /// <summary>
        /// 释放Accept(放入池中)
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        /// <param name="isRelease"></param>
        private void ReleaseAccept(SocketAsyncEventArgs acceptEventArgs, bool isRelease = true)
        {
            acceptEventArgsPool.Push(acceptEventArgs);
            if (isRelease)
            {
                maxConnectionsEnforcer.Release();
            }
        }

        /// <summary>
        /// Accept完成调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="acceptEventArgs"></param>
        private void Accept_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                HandleBadAccept(acceptEventArgs);
            }
        }

        /// <summary>
        /// 投递接收
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void PostReceive(SocketAsyncEventArgs ioEventArgs)
        {
            if (ioEventArgs.AcceptSocket == null) { return; }

            //异步接收数据
            if (!ioEventArgs.AcceptSocket.ReceiveAsync(ioEventArgs))
            {
                //同步接收数据完成,立即处理接收
                ProcessReceive(ioEventArgs);
            }
        }

        /// <summary>
        /// 处理接收
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs ioEventArgs)
        {
            DataToken dataToken = (DataToken)ioEventArgs.UserToken;
            if (ioEventArgs.BytesTransferred == 0)
            {
                //客户端主动关闭连接
                Closing(ioEventArgs);
                return;
            }

            if (ioEventArgs.SocketError != SocketError.Success)
            {
                //Socket错误
                Closing(ioEventArgs);
                return;
            }

            GSSocket gsSocket = dataToken == null ? null : dataToken.Socket;
            byte[] buffer = new byte[ioEventArgs.BytesTransferred];
            Buffer.BlockCopy(ioEventArgs.Buffer, dataToken.bufferOffset, buffer, 0, buffer.Length);

            PostReceive(ioEventArgs);
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(GSSocket socket, byte[] data)
        {
            socket.Enqueue(data);
            return await Task.Run(() =>
                {
                    if (socket.TrySetSendFlag())
                    {
                        DequeueAndPostSend(socket, null);
                        return true;
                    }
                    return false;
                });
        }

        /// <summary>
        /// 尝试发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ioEventArgs"></param>
        private void DequeueAndPostSend(GSSocket socket, SocketAsyncEventArgs ioEventArgs)
        {
            try
            {
                byte[] data;
                if (socket.Dequeue(out data))           //获取发送队列中的数据
                {
                    if (ioEventArgs == null)
                    {
                        ioEventArgs = ioEventArgsPool.Pop();
                        ioEventArgs.AcceptSocket = socket.WorkSocket;
                    }

                    DataToken dataToken = (DataToken)ioEventArgs.UserToken;
                    dataToken.Socket = socket;
                    dataToken.data = data;
                    dataToken.dataBytesDone = 0;
                    //投递发送
                    PostSend(ioEventArgs);
                }
                else
                {
                    //发送结束
                    ReleaseIOEventArgs(ioEventArgs);
                    socket.ResetSendFlag();
                }
            }
            catch (Exception ex)
            {
                socket.ResetSendFlag();
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 投递发送
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void PostSend(SocketAsyncEventArgs ioEventArgs)
        {
            DataToken dataToken = (DataToken)ioEventArgs.UserToken;
            if (dataToken.data.Length - dataToken.dataBytesDone <= socketSettings.BufferSize)
            {
                ioEventArgs.SetBuffer(dataToken.bufferOffset, dataToken.data.Length - dataToken.dataBytesDone);
                Buffer.BlockCopy(dataToken.data, dataToken.dataBytesDone, ioEventArgs.Buffer, dataToken.bufferOffset, dataToken.data.Length - dataToken.dataBytesDone);
            }
            else
            {
                ioEventArgs.SetBuffer(dataToken.bufferOffset, socketSettings.BufferSize);
                Buffer.BlockCopy(dataToken.data, dataToken.dataBytesDone, ioEventArgs.Buffer, dataToken.bufferOffset, socketSettings.BufferSize);
            }
            
            if (!ioEventArgs.AcceptSocket.SendAsync(ioEventArgs))
            {
                ProcessSend(ioEventArgs);
            }
        }

        /// <summary>
        /// 处理发送完成
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void ProcessSend(SocketAsyncEventArgs ioEventArgs)
        {
            DataToken dataToken = (DataToken)ioEventArgs.UserToken;
            if (ioEventArgs.SocketError == SocketError.Success)
            {
                dataToken.dataBytesDone += ioEventArgs.BytesTransferred;
                if (dataToken.dataBytesDone < dataToken.data.Length)
                {
                    //数据很大，没有一次发送完成
                    PostSend(ioEventArgs);
                }
                else
                {
                    //当前队列已经发送完成，发送队列中下一个数据
                    DequeueAndPostSend(dataToken.Socket, ioEventArgs);
                }
            }
            else
            {
                //发送出错,关闭socket
                dataToken.Socket.ResetSendFlag();
                Closing(ioEventArgs);
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void Closing(SocketAsyncEventArgs ioEventArgs)
        {
            DataToken dataToken = (DataToken)ioEventArgs.UserToken;

            try
            {
                if (ioEventArgs.AcceptSocket != null)
                {
                    ioEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                CloseSocket(ioEventArgs);
                ReleaseIOEventArgs(ioEventArgs);
                maxConnectionsEnforcer.Release();
            }
        }

        /// <summary>
        /// 释放IOEventArgs
        /// </summary>
        /// <param name="ioEventArgs"></param>
        private void ReleaseIOEventArgs(SocketAsyncEventArgs ioEventArgs)
        {
            if (ioEventArgs == null) { return; }

            DataToken dataToken = (DataToken)ioEventArgs.UserToken;
            if (dataToken != null)
            {
                dataToken.Socket = null;
            }
            ioEventArgs.AcceptSocket = null;
            ioEventArgsPool.Push(ioEventArgs);
        }

        /// <summary>
        /// IO完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ioEventArgs"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs ioEventArgs)
        {
            DataToken dataToken = (DataToken)ioEventArgs.UserToken;
            try
            {
                switch (ioEventArgs.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(ioEventArgs);
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(ioEventArgs);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (ObjectDisposedException)
            {
                ReleaseIOEventArgs(ioEventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
