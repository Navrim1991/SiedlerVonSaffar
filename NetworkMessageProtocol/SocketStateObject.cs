using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public class SocketStateObject
    {
        public Socket WorkSocket { get; set; }
        // Size of receive buffer.
        public const int BufferSize = 4096;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
    }
}
