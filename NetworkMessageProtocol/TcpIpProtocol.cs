using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    [Serializable]
    public class TcpIpProtocol
    {
        private const int TCP_IP_PROTOCOL_CLIENT_PATTERN = 0x00000F00;
        private const int TCP_IP_PROTOCOL_SERVER_PATTERN = 0x00F00000;

        public readonly byte[] PLAYER_NAME;
        public readonly byte[] PLAYER_READY;
        public readonly byte[] PLAYER_STAGE_FOUNDATION_ROLL_DICE;
        public readonly byte[] PLAYER_CONTAINER_DATA;
        public readonly byte[] PLAYER_ROLL_DICE;
        public readonly byte[] PLAYER_DEAL;
        public readonly byte[] PLAYER_DATA;
        public readonly byte[] PLAYER_PLAY_PROGRESS_CARD;
        public readonly byte[] PLAYER_BUY_PROGRESS_CARD;
        public readonly byte[] PLAYER_SET_BANDIT;

        public readonly byte[] SERVER_PLAYER_DATA;
        public readonly byte[] SERVER_NEED_PLAYER_NAME;
        public readonly byte[] SERVER_STAGE_FOUNDATION_ROLL_DICE;
        public readonly byte[] SERVER_CONTAINER_DATA_OWN;
        public readonly byte[] SERVER_CONTAINER_DATA_OTHER;
        public readonly byte[] SERVER_ERROR;
        public readonly byte[] SERVER_SET_BANDIT;

        public TcpIpProtocol()
        {
            PLAYER_NAME = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_NAME);
            PLAYER_READY = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_READY);
            PLAYER_STAGE_FOUNDATION_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_STAGE_FOUNDATION_ROLL_DICE);
            PLAYER_CONTAINER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_CONTAINER_DATA);
            PLAYER_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_ROLL_DICE);
            PLAYER_DEAL = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DEAL);
            PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DATA);
            PLAYER_PLAY_PROGRESS_CARD = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_PLAY_PROGRESS_CARD);
            PLAYER_BUY_PROGRESS_CARD = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_BUY_PROGRESS_CARD);
            PLAYER_SET_BANDIT = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_SET_BANDIT);

            SERVER_PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_PLAYER_DATA);
            SERVER_NEED_PLAYER_NAME = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_NEED_PLAYER_NAME);
            SERVER_STAGE_FOUNDATION_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_STAGE_FOUNDATION_ROLL_DICE);
            SERVER_CONTAINER_DATA_OWN = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CONTAINER_DATA_OWN);
            SERVER_CONTAINER_DATA_OTHER = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CONTAINER_DATA_OTHER);
            SERVER_ERROR = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_ERROR);
            SERVER_SET_BANDIT = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_SET_BANDIT);
        }

        public bool IsClientDataPattern(byte[] data)
        {
            byte[] clientPatternBytes = BitConverter.GetBytes(TCP_IP_PROTOCOL_CLIENT_PATTERN);
            int clientDataPatternBytesLength = clientPatternBytes.Length;

            if (data.Length < clientDataPatternBytesLength)
            {
                Configuration.DeveloperParameter.PrintDebug("ClientDataPattern-Data is to small");

                return false;
            }

            byte[] clientDataProtocol = { 0, data[1], 0 , 0 };

            return (BitConverter.ToInt32(clientDataProtocol, 0) == TCP_IP_PROTOCOL_CLIENT_PATTERN) ? true : false;            
        }

        public bool IsServerDataPattern(byte[] data)
        {
            byte[] serverPatternBytes = BitConverter.GetBytes(TCP_IP_PROTOCOL_SERVER_PATTERN);
            int serverPatternBytesLength = serverPatternBytes.Length;

            if (data.Length < serverPatternBytesLength)
            {
                Configuration.DeveloperParameter.PrintDebug("ServertDataPattern-Data is to small");

                return false;
            }

            byte[] serverDataProtocol = { 0, 0, data[2], 0 };

            return (5>>BitConverter.ToInt32(serverDataProtocol, 0) == 5>>TCP_IP_PROTOCOL_SERVER_PATTERN) ? true : false;
        }
    }
}
