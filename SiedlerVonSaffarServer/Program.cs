using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Net.NetworkInformation;
using SiedlerVonSaffar.Networking;

namespace SiedlerVonSaffar
{
    class Program
    {

        static void Main(string[] args)
        {
            Configuration.DeveloperParameter.init();
            Networking.ServerConfig.init();
            Networking.Broadcast.BroadcastServer.Instance.StartReceiving();
            Networking.TCP.TcpServer.Instance.StartListening();

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
    }
}
