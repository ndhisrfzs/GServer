using System.Collections.Generic;
using System.Net.Sockets;

namespace GServer.Sockets
{
    public class BufferManager
    {
        private int capacity;
        private byte[] bufferBlock;
        private Stack<int> freeIndexPool;
        private int currentIndex;
        private int splitSize;

        public BufferManager(int capacity, int splitSize)
        {
            this.capacity = capacity;
            this.splitSize = splitSize;
            this.freeIndexPool = new Stack<int>();
        }

        internal void InitBuffer()
        {
            this.bufferBlock = new byte[capacity];
        }

        internal bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (this.freeIndexPool.Count > 0)
            {
                args.SetBuffer(this.bufferBlock, this.freeIndexPool.Pop(), this.splitSize);
            }
            else
            {
                if ((capacity - this.splitSize) < currentIndex)
                {
                    return false;
                }
                args.SetBuffer(this.bufferBlock, this.currentIndex, this.splitSize);
                this.currentIndex += this.splitSize;
            }

            return true;
        }

        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            this.freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
