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
        PLAYER_ROLL_DICE = 0x00000F03,
        PLAYER_DEAL = 0x00000F04,
        PLAYER_DATA = 0x00000F06,
        PLAYER_READY = 0x00000F07,
        PLAYER_NAME = 0x00000F08,
        PLAYER_PLAY_PROGRESS_CARD = 0x00000F09,
        PLAYER_BUY_PROGRESS_CARD = 0x00000F0A,
        PLAYER_SET_BANDIT = 0x00000F0B,
        SERVER_STAGE_FOUNDATION_ROLL_DICE = 0x00F01000,
        SERVER_CONTAINER_DATA_OWN = 0x00F02000,
        SERVER_CONTAINER_DATA_OTHER = 0x00F03000,
        SERVER_PLAYER_DATA = 0x00F04000,
        SERVER_NEED_PLAYER_NAME = 0x00F05000,
        SERVER_SET_BANDIT = 0x00F06000,
        SERVER_ERROR = 0x00F07000,
    }
}
