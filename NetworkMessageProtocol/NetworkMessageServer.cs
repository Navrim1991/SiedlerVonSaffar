using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public enum MessageTyps
    {
        TO_ALL,
        TO_OWN,
        TO_OTHER
    }
    public class NetworkMessageServer
    {
        public string PlayerName { get; private set; }
        public TcpIpProtocolType ProtocolType { get; private set; }

        public object[] Data { get; private set; }

        public MessageTyps Typ { get; private set; }

        public NetworkMessageServer(string playerName, TcpIpProtocolType protocolType, object[] data, MessageTyps typ)
        {
            PlayerName = playerName;
            ProtocolType = protocolType;
            Data = data;
            Typ = typ;
        }

        public NetworkMessageServer(string playerName, TcpIpProtocolType protocolType, object[] data)
        {
            PlayerName = playerName;
            ProtocolType = protocolType;
            Data = data;
            Typ = MessageTyps.TO_ALL;
        }

    }
}
