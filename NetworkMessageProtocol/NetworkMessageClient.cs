using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public class NetworkMessageClient
    {
        public string PlayerName { get; private set; }
        public TcpIpProtocolType ProtocolType { get; private set; }

        public object[] Data{ get; private set; }

        public NetworkMessageClient(string playerName, TcpIpProtocolType protocolType, object[] data)
        {
            PlayerName = playerName;
            ProtocolType = protocolType;
            Data = data;
        }

    }
}
