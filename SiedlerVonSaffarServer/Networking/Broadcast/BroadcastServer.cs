using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace SiedlerVonSaffar.Networking.Broadcast
{
    class BroadcastServer : NetworkMessageProtocol.Broadcast, IDisposable
    {
        private UdpClient listener;                
        public bool StopListening { get; set; }

        private static BroadcastServer singleton;

        private BroadcastServer()
        {
            StopListening = false;           
        }

        public static BroadcastServer Instance
        {
            get
            {
                if (singleton == null)
                    singleton = new BroadcastServer();

                return singleton;
            }
        }


        public void StartReceiving()
        {
            listener = new UdpClient(PORT);

            listener.EnableBroadcast = true;

            Receive();

            Configuration.DeveloperParameter.PrintDebug("Broadcast server started");
        }

        private void Receive()
        {
            listener.BeginReceive(ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                byte[] receivedBytes = listener.EndReceive(asyncResult, ref remoteEndPoint);

                if (protocol.isFindServerPattern(receivedBytes))
                {
                    byte[] equalBytes = { receivedBytes[0], receivedBytes[1], receivedBytes[2], receivedBytes[3] };

                    if (SERVER_REQUEST_ARE_YOU_SERVER.SequenceEqual(equalBytes))
                    {
                        Configuration.DeveloperParameter.PrintDebug(remoteEndPoint.Address.ToString() + ":\r\n\t received message: " +
                            ((NetworkMessageProtocol.BroadcastProtocolType)BitConverter.ToInt32(equalBytes, 0)).ToString());

                        byte[] datagram = BitConverter.GetBytes((int)NetworkMessageProtocol.BroadcastProtocolType.CLIENT_RESPONSE_YES_I_AM);

                        Send(datagram, remoteEndPoint);
                    }
                }

                //TODO STOPPING
                if (!StopListening)
                    Receive(); // <-- this will be our loop
            }
            catch (ObjectDisposedException ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message);
            }
            catch (NullReferenceException ex)
            {
                Configuration.DeveloperParameter.PrintDebug(ex.Message);
            }

        }

        private void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            // Begin sending the data to the remote device.

            listener.BeginSend(data, data.Length, remoteEndPoint, SendCallback, listener);
        }

        public static void SendCallback(IAsyncResult asyncResult)
        {
            UdpClient responseUdpClient = (UdpClient)asyncResult.AsyncState;
            int sendBytes = responseUdpClient.EndSend(asyncResult);
        }

        public void Dispose()
        {
            listener.Close();

            Configuration.DeveloperParameter.PrintDebug("Broadcast-Server stopped and disposed");

            listener = null;

            singleton = null;

            protocol = null;
        }
    }
}
