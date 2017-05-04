using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public abstract class Tcp
    {
        protected TcpIpProtocol protocol;
        protected readonly byte[] CLIENT_DATA_TURN;

        protected readonly int PORT = 15000;

        public Tcp()
        {
            protocol = new TcpIpProtocol();

            CLIENT_DATA_TURN = BitConverter.GetBytes((int)TcpIpProtocolType.DATA_CLIENT_TURN);
        }
    }
}
