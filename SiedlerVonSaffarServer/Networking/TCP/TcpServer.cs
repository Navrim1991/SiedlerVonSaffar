using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using SiedlerVonSaffar.GameLogic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.Networking.TCP
{
    class TcpServer : NetworkMessageProtocol.Tcp
    {
        //private readonly object aSyncLock = null;
        private static TcpServer singleton;
        List<Socket> connections;
        //private ARP.ArpRequest arpRequest;
        public bool StopListening { get; set; }

        private RabbitMQBroker.Server rabbitMQServer;
        private TcpServer()
        {
            StopListening = false;
            rabbitMQServer = new RabbitMQBroker.Server();
            connections = new List<Socket>();
        }

        public static TcpServer Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new TcpServer();
                }
                    

                return singleton;
            }
        }

        public void StartListening()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);


            try
            {
                listener.Bind(new IPEndPoint(ServerConfig.HostIpAddress, PORT));
                listener.Listen(ServerConfig.MAX_CONNECTIONS);

                Configuration.DeveloperParameter.PrintDebug("TCP server started\n\r\tWaiting for TCP Connections");

                while (rabbitMQServer.TcpConnectionCount < ServerConfig.MAX_CONNECTIONS && !rabbitMQServer.GameLogic.GameHasStarted)
                {
                    connections.Add(listener.Accept());

                    rabbitMQServer.TcpConnectionCount++;
                }

                foreach(Socket element in connections)
                {
                    element.Disconnect(false);
                }

                connections = null;

                Broadcast.BroadcastServer.Instance.Dispose();
            }
            catch (Exception ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
            }
        }
    }
}
