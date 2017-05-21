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
        protected readonly byte[] PLAYER_TURN;
        protected readonly byte[] PLAYER_DATA;
        protected readonly byte[] PLAYER_DEAL;
        protected readonly byte[] PLAYER_ROLL_DICE;
        protected readonly byte[] PLAYER_PLAY_PROGRESSCARD;
        protected readonly byte[] PLAYER_READY;
        protected readonly byte[] SERVER_CREATE_GAME;
        protected readonly byte[] SERVER_DATA;
        protected readonly byte[] SERVER_GIVE_RESOURCES;
        protected readonly byte[] SERVER_CHANGE_BANDIT;

        protected readonly int PORT = 15000;

        public Tcp()
        {
            protocol = new TcpIpProtocol();

            PLAYER_TURN = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_TURN);
            PLAYER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DATA);
            PLAYER_DEAL = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_DEAL);
            PLAYER_ROLL_DICE = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_ROLL_DICE);
            PLAYER_PLAY_PROGRESSCARD = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_PLAY_PROGRESSCARD);
            PLAYER_READY = BitConverter.GetBytes((int)TcpIpProtocolType.PLAYER_READY);
            SERVER_CREATE_GAME = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CREATE_GAME);
            SERVER_DATA = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_DATA);
            SERVER_GIVE_RESOURCES = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_GIVE_RESOURCES);
            SERVER_CHANGE_BANDIT = BitConverter.GetBytes((int)TcpIpProtocolType.SERVER_CHANGE_BANDIT);
        }
    }
}
