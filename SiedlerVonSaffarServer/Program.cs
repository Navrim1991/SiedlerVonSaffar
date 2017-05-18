using System;
using System.Threading;
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
using SiedlerVonSaffar.GameLogic;

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

            string exitString = "";

            do
            {
                Console.WriteLine("\ntip exit for closing the server...");
                exitString = Console.ReadLine();
            } while (exitString != "exit");
            
        }
    }
}
