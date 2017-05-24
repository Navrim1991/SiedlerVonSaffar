using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic
{
    public class RecieveMessage
    {
        public IPEndPoint ClientIP { get; set; }
        public byte[] Data { get; set; }


        public RecieveMessage(IPEndPoint clientIP, byte[] data)
        {
            ClientIP = clientIP;
            Data = data;
        }
    }
}
