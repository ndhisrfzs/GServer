using System;

namespace GServer
{
    public class SocketMessage
    {
        public int id { get; set; }
        public MessageType type { get; set; }
        public IntPtr handle { get; set; }
        public byte[] data;
    }

    public enum MessageType
    {
        SOCKET_EXIT,
        SOCKET_DATA,
        SOCKET_CLOSE,
        SOCKET_OPEN,
        SOCKET_ERROR,
        SOCKET_ACCEPT,
    }
}
