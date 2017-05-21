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
        PLAYER_TURN = 0x00000F01,
        PLAYER_DATA = 0x00000F02,
        PLAYER_DEAL = 0x00000F04,
        PLAYER_ROLL_DICE = 0x00000F08,
        PLAYER_PLAY_PROGRESSCARD = 0x00000F10,
        PLAYER_READY = 0x00000F20,
        SERVER_CREATE_GAME = 0x00F01000,
        SERVER_DATA = 0x00F02000,
        SERVER_GIVE_RESOURCES = 0x00F04000,
        SERVER_CHANGE_BANDIT = 0x00F04000,
    }
}
