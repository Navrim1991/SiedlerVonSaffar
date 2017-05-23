using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public class TcpIpProtocol
    {
        private const int TCP_IP_PROTOCOL_DATA_PATTERN = 0x00000F00;

        public readonly byte[] PLAYER_NAME;
        public readonly byte[] PLAYER_READY;
        public readonly byte[] PLAYER_STAGE_FOUNDATION_ROLL_DICE;
        public readonly byte[] PLAYER_CONTAINER_DATA;
        public readonly byte[] PLAYER_ROLL_DICE;

        public readonly byte[] SERVER_PLAYER_DATA;
        public readonly byte[] SERVER_NEED_PLAYER_NAME;
        public readonly byte[] SERVER_STAGE_FOUNDATION_ROLL_DICE;
        public readonly byte[] SERVER_CONTAINER_DATA;

        public readonly byte[] PLAYER_LOGIN_TO_SERVER;

        public TcpIpProtocol()
        {
            SERVER_PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_PLAYER_DATA);
            SERVER_NEED_PLAYER_NAME = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_NEED_PLAYER_NAME);
            PLAYER_NAME = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_NAME);
            PLAYER_LOGIN_TO_SERVER = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_LOGIN_TO_SERVER);
            SERVER_STAGE_FOUNDATION_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_STAGE_FOUNDATION_ROLL_DICE);
            PLAYER_STAGE_FOUNDATION_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_STAGE_FOUNDATION_ROLL_DICE);
            PLAYER_CONTAINER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_CONTAINER_DATA);
            SERVER_CONTAINER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CONTAINER_DATA);
            PLAYER_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_ROLL_DICE);
        }

        public bool isClientDataPattern(byte[] data)
        {
            byte[] clientDataPatternBytes = BitConverter.GetBytes(TCP_IP_PROTOCOL_DATA_PATTERN);
            int clientDataPatternBytesLength = clientDataPatternBytes.Length;

            if (data.Length < clientDataPatternBytesLength)
            {
                Configuration.DeveloperParameter.PrintDebug("ClientDataPattern-Data is to small");

                return false;
            }

            byte[] clientDataProtocol = { 0, data[1], 0 , 0 };

            return (BitConverter.ToInt32(clientDataProtocol, 0) == TCP_IP_PROTOCOL_DATA_PATTERN) ? true : false;            
        }
    }
}
