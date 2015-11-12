
namespace GServer.Sockets
{
    public class DataToken
    {
        /// <summary>
        /// 当前使用的缓冲区在BufferBlock中的位置
        /// </summary>
        public int bufferOffset;
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] data;
        /// <summary>
        /// 已处理数据数量
        /// </summary>
        public int dataBytesDone;
        /// <summary>
        /// socket
        /// </summary>
        public GSSocket Socket;
    }
}
