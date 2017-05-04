using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public abstract class Broadcast
    {
        protected readonly byte[] SERVER_REQUEST_ARE_YOU_SERVER;
        protected readonly byte[] CLIENT_RESPONSE_YES_I_AM;
        protected BroadcastProtocol protocol;

        protected readonly int PORT;

        public Broadcast()
        {
            PORT = 11000;

            SERVER_REQUEST_ARE_YOU_SERVER = BitConverter.GetBytes((int)BroadcastProtocolType.SERVER_REQUEST_ARE_YOU_SERVER);
            CLIENT_RESPONSE_YES_I_AM = BitConverter.GetBytes((int)BroadcastProtocolType.CLIENT_RESPONSE_YES_I_AM);

            protocol = new BroadcastProtocol();
        }

    }
}
