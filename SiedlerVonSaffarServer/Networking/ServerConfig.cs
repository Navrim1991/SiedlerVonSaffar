using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SiedlerVonSaffar.Configuration;

namespace SiedlerVonSaffar.Networking
{
    class ServerConfig
    {
        public static IPHostEntry HostIpInfo { get; set; }
        public static IPHostEntry LocalEndPoint { get; set; }
        public static IPAddress HostIpAddress { get; set; }
        public static byte MAX_CONNECTIONS
        {
            get
            {
                return 4;
            }
        }
        public static void init()
        {
#pragma warning disable CS0618 // Typ oder Element ist veraltet
            HostIpInfo = Dns.Resolve(Dns.GetHostName());
#pragma warning restore CS0618 // Typ oder Element ist veraltet
            HostIpAddress = HostIpInfo.AddressList[1];

#pragma warning disable CS0618 // Typ oder Element ist veraltet
            DeveloperParameter.PrintDebug("Server configurated with host-IP: \n\r\t" + HostIpAddress.Address.ToString());
#pragma warning restore CS0618 // Typ oder Element ist veraltet
        }
    }
}
