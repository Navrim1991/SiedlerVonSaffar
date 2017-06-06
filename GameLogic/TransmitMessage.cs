using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic
{
    public class TransmitMessage
    {
        public enum TransmitTyps
        {
            TO_ALL,
            TO_OWN,
            TO_OTHER
        }
        public byte[] Data { get; set; }

        public TransmitTyps TransmitTyp { get; set; }

        public TransmitMessage(byte[] data, TransmitTyps transmitTyp)
        {
            Data = data;
            TransmitTyp = transmitTyp;
        }

        public TransmitMessage(byte[] data)
        {
            Data = data;
            TransmitTyp = TransmitTyps.TO_ALL;
        }

    }
}
