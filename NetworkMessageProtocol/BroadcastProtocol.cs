using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    [Serializable]
    public class BroadcastProtocol
    {
        private const int BROADCAST_PROTOCOL_FIND_SERVER_PATTERN = 0x000000F0;

        public bool isFindServerPattern(byte[] data)
        {
            byte[] findServerPatternBytes = BitConverter.GetBytes(BROADCAST_PROTOCOL_FIND_SERVER_PATTERN);
            int findServerPatternBytesLength = findServerPatternBytes.Length;

            if (data.Length < findServerPatternBytesLength)
            {
                Configuration.DeveloperParameter.PrintDebug("FindServerPattern-Data is to small");

                return false;
            }

            return ((data[0] & findServerPatternBytes[0])
                == BROADCAST_PROTOCOL_FIND_SERVER_PATTERN) ? true : false;
        }


    }
}
