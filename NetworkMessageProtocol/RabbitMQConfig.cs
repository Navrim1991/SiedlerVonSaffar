using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.NetworkMessageProtocol
{
    public class RabbitMQConfig
    {
        public static string SERVER_QUEUE_NAME
        {
            get
            {
                return "toServer";
            }            
        }

        public static string PROXY_QUEUE_NAME
        {
            get
            {
                return "proxy";
            }
        }


    }
}
