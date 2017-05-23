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
        PLAYER_STAGE_FOUNDATION_ROLL_DICE = 0x00000F01,
        PLAYER_CONTAINER_DATA = 0x00000F02,
        PLAYER_ROLL_DICE = 0x00000F04,
        PLAYER_READY = 0x00000F20,
        PLAYER_NAME = 0x00000F40,
        SERVER_STAGE_FOUNDATION_ROLL_DICE = 0x00F01000,
        SERVER_CONTAINER_DATA = 0x00F02000,
        SERVER_PLAYER_DATA = 0x00F20000,
        SERVER_NEED_PLAYER_NAME = 0x00F40000,
        PLAYER_LOGIN_TO_SERVER = 0x0F000000,
    }
}
