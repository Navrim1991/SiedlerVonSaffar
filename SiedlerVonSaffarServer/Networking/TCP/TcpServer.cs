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
        private byte connectionCount = 0;
        private readonly object aSyncLock = null;
        private List<Socket> connections;
        private static TcpServer singleton;
        private ARP.ArpRequest arpRequest;
        public bool StopListening { get; set; }
        private GameLogic.GameLogic gameLogic;

        private TcpServer()
        {
            StopListening = false;
            arpRequest = new ARP.ArpRequest();

            connections = new List<Socket>(4);

            gameLogic = new GameLogic.GameLogic();
        }

        public static TcpServer Instance
        {
            get
            {
                if (singleton == null)
                    singleton = new TcpServer();

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

                gameLogic.RxQueue.Enqueue(new NetworkMessageProtocol.SocketStateObject());

                while (connectionCount < ServerConfig.MAX_CONNECTIONS)
                {
                    Socket acceptedConnection = listener.Accept();

                    IPEndPoint remoteEndPoint = (IPEndPoint)acceptedConnection.RemoteEndPoint;
                    IPEndPoint localEndPoint = (IPEndPoint)acceptedConnection.LocalEndPoint;

                    PhysicalAddress remoteMac = arpRequest.Arp(remoteEndPoint);

                    Configuration.DeveloperParameter.PrintDebug("TCP connection accepted\n\r\tip: " + remoteEndPoint.Address.ToString() +
                        "\n\r\tmac: " + remoteMac.ToString());

                    NetworkMessageProtocol.SocketStateObject state = new NetworkMessageProtocol.SocketStateObject();
                    state.WorkSocket = acceptedConnection;

                    acceptedConnection.BeginReceive(state.buffer, 0, NetworkMessageProtocol.SocketStateObject.BufferSize, 0,
                        ReceiveCallback, state);
                           
                    connectionCount++;
                    connections.Add(acceptedConnection);
                }

                arpRequest = null;
                Broadcast.BroadcastServer.Instance.Dispose();
            }
            catch (Exception ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            NetworkMessageProtocol.SocketStateObject state = (NetworkMessageProtocol.SocketStateObject)asyncResult.AsyncState;
            Socket handler = state.WorkSocket;

            int bytesRead = handler.EndReceive(asyncResult);

            if (bytesRead > 0)
            {
                if (protocol.isClientDataPattern(state.buffer))
                {
                    byte[] equalBytes = { state.buffer[0], state.buffer[1], state.buffer[2], state.buffer[3] };

                    if (CLIENT_DATA_TURN.SequenceEqual(equalBytes))
                    {
                        List<Socket> connectionsToSendData = (from p in connections where p.RemoteEndPoint != handler.RemoteEndPoint select p).ToList<Socket>();

                        foreach (Socket element in connectionsToSendData)
                            Send(element, state.buffer);
                    }


                }
                else if (Configuration.DeveloperParameter.TcpEcho)
                {
                    foreach (Socket element in connections)
                        Send(element, state.buffer);
                }
            }        

            handler.BeginReceive(state.buffer, 0, NetworkMessageProtocol.SocketStateObject.BufferSize, 0,
                        ReceiveCallback, state);
        }

        private void Send(Socket handler, byte[] data)
        {
            // Convert the string data to byte data using ASCII encoding.

            // Begin sending the data to the remote device.
            handler.BeginSend(data, 0, data.Length, 0,
                SendCallback, handler);
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)asyncResult.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(asyncResult);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
            }
        }

    }
}
