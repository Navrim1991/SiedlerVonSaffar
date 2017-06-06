using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SiedlerVonSaffar.NetworkMessageProtocol;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SiedlerVonSaffar.RabbitMQBroker
{
    public class Server
    {
        private ConnectionFactory factory;
        private IConnection connection;
        private IModel channelToServer;
        private IModel channelDirectRouting;
        private IModel channelFanout;
        private EventingBasicConsumer serverConsumer;
        public GameLogic.GameLogic GameLogic { get; private set; }
        private TcpIpProtocol tcpProtocol;
        public short TcpConnectionCount
        {
            get; set;
        }
        private short playersReady;

        public Server()
        {
            tcpProtocol = new TcpIpProtocol();
            //TODO setHOstname
            factory = new ConnectionFactory();
            factory.HostName = "localhost";
            connection = factory.CreateConnection();

            channelToServer = connection.CreateModel();
            channelDirectRouting = connection.CreateModel();
            channelFanout = connection.CreateModel();

            channelToServer.QueueDeclare(RabbitMQConfig.SERVER_QUEUE_NAME, false, false, false, null);
            channelFanout.ExchangeDeclare(RabbitMQConfig.PROXY_QUEUE_NAME, "fanout");

            serverConsumer = new EventingBasicConsumer(channelToServer);
            serverConsumer.Received += ChannelToServerReceivedEvent;
            channelToServer.BasicConsume(RabbitMQConfig.SERVER_QUEUE_NAME, true, serverConsumer);

            GameLogic = new GameLogic.GameLogic();
        }

        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(0, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }



        private void ChannelToServerReceivedEvent(object sender, BasicDeliverEventArgs args)
        {
            object data = Deserialize(args.Body);

            if (data is NetworkMessageClient)
            {
                NetworkMessageClient message = (NetworkMessageClient)data;
                if (!GameLogic.GameHasStarted)
                {
                    if (message.ProtocolType == TcpIpProtocolType.PLAYER_READY)
                    {
                        playersReady++;

                        if (TcpConnectionCount >= 3 && TcpConnectionCount == playersReady)
                        {
                            GameLogic.RxQueue.Enqueue(playersReady);

                            GameLogic.Signal();

                            channelFanout.BasicPublish(RabbitMQConfig.PROXY_QUEUE_NAME, "", null, tcpProtocol.SERVER_NEED_PLAYER_NAME);
                        }
                    }
                }
                else
                {
                    if (data is NetworkMessageClient)
                    {
                        GameLogic.RxQueue.Enqueue(data);

                        GameLogic.Signal();
                    }
                }
            }                  
        }
    }
}
