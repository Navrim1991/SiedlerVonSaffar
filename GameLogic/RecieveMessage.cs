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
        public byte[] Data { get; set; }

        public string PlayerName { get; private set; }


        public RecieveMessage(byte[] data)
        {
            Data = data;
        }
    }
}
