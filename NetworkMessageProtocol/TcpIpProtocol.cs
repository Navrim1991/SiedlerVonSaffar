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

        public bool isClientDataPattern(byte[] data)
        {
            byte[] clientDataPatternBytes = BitConverter.GetBytes(TCP_IP_PROTOCOL_DATA_PATTERN);
            int clientDataPatternBytesLength = clientDataPatternBytes.Length;

            if (data.Length < clientDataPatternBytesLength)
            {
                Configuration.DeveloperParameter.PrintDebug("ClientDataPattern-Data is to small");

                return false;
            }

            byte[] clientDataProtocol = { data[0], data[1], 0 , 0 };

            return (BitConverter.ToInt32(clientDataProtocol, 0) == TCP_IP_PROTOCOL_DATA_PATTERN) ? true : false;            
        }
    }
}
