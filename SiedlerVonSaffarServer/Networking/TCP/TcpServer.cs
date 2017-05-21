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
        private static TcpServer singleton;
        private ARP.ArpRequest arpRequest;
        public bool StopListening { get; set; }
        private GameLogic.GameLogic gameLogic;

        private TcpServer()
        {
            StopListening = false;

            gameLogic = new GameLogic.GameLogic();

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
            gameLogic.RxQueue.Enqueue(this);

            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(new IPEndPoint(ServerConfig.HostIpAddress, PORT));
                listener.Listen(ServerConfig.MAX_CONNECTIONS);

                Configuration.DeveloperParameter.PrintDebug("TCP server started\n\r\tWaiting for TCP Connections");

                bool gameHasStarted = false;

                while (connectionCount < ServerConfig.MAX_CONNECTIONS && !gameHasStarted)
                {
                    if (gameLogic.TxQueue.Count > 0)
                    {
                        object rxObject;
                        gameLogic.TxQueue.TryDequeue(out rxObject);

                        if (rxObject != null)
                        {
                            if (rxObject is bool)
                                gameHasStarted = (bool)rxObject;

                            if (gameHasStarted)
                                break;
                        }
                    }

                    Socket acceptedConnection = listener.Accept();

                    NetworkMessageProtocol.SocketStateObject state = new NetworkMessageProtocol.SocketStateObject();
                    state.WorkSocket = acceptedConnection;                    

                    acceptedConnection.BeginReceive(state.buffer, 0, NetworkMessageProtocol.SocketStateObject.BufferSize, 0,
                        ReceiveCallback, state);
                           
                    connectionCount++;
                }

                Broadcast.BroadcastServer.Instance.Dispose();
            }
            catch (Exception ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
            }

            object txObject;

            while (true)
            {
                while (gameLogic.TxQueue.Count < 1) ;

                gameLogic.TxQueue.TryDequeue(out txObject);

                if (txObject is NetworkMessageProtocol.SocketStateObject)
                {
                    NetworkMessageProtocol.SocketStateObject state = (NetworkMessageProtocol.SocketStateObject)txObject;
                    Send(state.WorkSocket, state.buffer);
                }
           }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            NetworkMessageProtocol.SocketStateObject state = (NetworkMessageProtocol.SocketStateObject)asyncResult.AsyncState;

            int bytesRead = state.WorkSocket.EndReceive(asyncResult);

            if (bytesRead > 0)
            {
                gameLogic.RxQueue.Enqueue(state);
            }
                

            state.WorkSocket.BeginReceive(state.buffer, 0, NetworkMessageProtocol.SocketStateObject.BufferSize, 0,
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
            }
            catch (Exception ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
            }
        }

    }
}
