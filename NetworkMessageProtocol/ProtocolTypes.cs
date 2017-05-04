using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiedlerVonSaffar.Configuration;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    [Flags]
    public enum BroadcastProtocolType
    {
        SERVER_REQUEST_ARE_YOU_SERVER = 0x000000F1,
        CLIENT_RESPONSE_YES_I_AM = 0x000000F2
    }

    [Flags]
    public enum TcpIpProtocolType
    {
        DATA_CLIENT_TURN = 0x00000F01
    }
}
