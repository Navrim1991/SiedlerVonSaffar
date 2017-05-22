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

        public readonly byte[] PLAYER_TURN;
        public readonly byte[] PLAYER_DATA;
        public readonly byte[] PLAYER_DEAL;
        public readonly byte[] PLAYER_ROLL_DICE;
        public readonly byte[] PLAYER_PLAY_PROGRESSCARD;
        public readonly byte[] PLAYER_READY;
        public readonly byte[] SERVER_CREATE_GAME;
        public readonly byte[] SERVER_DATA;
        public readonly byte[] SERVER_GIVE_RESOURCES;
        public readonly byte[] SERVER_CANT_GIVE_RESOURCES;
        public readonly byte[] SERVER_CHANGE_BANDIT;
        public readonly byte[] SERVER_PLAYER_DATA;

        public TcpIpProtocol(){
            PLAYER_TURN = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_TURN);
            PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DATA);
            PLAYER_DEAL = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DEAL);
            PLAYER_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_ROLL_DICE);
            PLAYER_PLAY_PROGRESSCARD = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_PLAY_PROGRESSCARD);
            PLAYER_READY = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_READY);
            SERVER_CREATE_GAME = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CREATE_GAME);
            SERVER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_DATA);
            SERVER_GIVE_RESOURCES = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_GIVE_RESOURCES);
            SERVER_CANT_GIVE_RESOURCES = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CANT_GIVE_RESOURCES);
            SERVER_CHANGE_BANDIT = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CHANGE_BANDIT);
            SERVER_PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_PLAYER_DATA);
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
