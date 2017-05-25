using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace SiedlerVonSaffar.Prototyp
{

    class Prototyp
    {
        List<IPEndPoint> clients;
        GameLogic.GameLogic gameLogic;
        NetworkMessageProtocol.TcpIpProtocol tcpProtocol;
        DataStruct.Container globalContainer = null;

        List<GameObjects.Player.Player> players;

        public Prototyp()
        {
            
            clients = new List<IPEndPoint>(4);
            players = new List<GameObjects.Player.Player>(4);

            clients.Add(new IPEndPoint(IPAddress.Parse("192.168.1.100"), 15000));
            clients.Add(new IPEndPoint(IPAddress.Parse("192.168.1.101"), 15000));
            clients.Add(new IPEndPoint(IPAddress.Parse("192.168.1.102"), 15000));

            gameLogic = new GameLogic.GameLogic();

            tcpProtocol = new NetworkMessageProtocol.TcpIpProtocol();
        }

        private GameObjects.Player.Player HandlePlayerData(byte[] playerData)
        {
            MemoryStream stream = new MemoryStream(playerData);

            IFormatter formatter = new BinaryFormatter();

            stream.Seek(5, SeekOrigin.Begin);

            return (GameObjects.Player.Player)formatter.Deserialize(stream); 
        }

        private byte[] HandlePlayerData(GameObjects.Player.Player player)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, player);

            byte[] data = new byte[stream.Length + tcpProtocol.PLAYER_DATA.Length + 1];

            tcpProtocol.PLAYER_DATA.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.PLAYER_DATA.Length + 1, (int)stream.Length);

            return data;
        }

        private byte[] HandlePlayerName(string playerName)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, playerName);

            byte[] data = new byte[stream.Length + tcpProtocol.PLAYER_NAME.Length + 1];

            tcpProtocol.PLAYER_NAME.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.PLAYER_NAME.Length + 1, (int)stream.Length);

            return data; 
        }

        private byte[] HandleRollDice(int diceNumber)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, diceNumber);

            byte[] data = new byte[stream.Length + tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE.Length + 1];

            tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE.Length + 1, (int)stream.Length);

            return data;
        }

        private DataStruct.Container HandleContainerDataOwn(byte[] containerData)
        {
            MemoryStream stream = new MemoryStream(containerData);

            IFormatter formatter = new BinaryFormatter();

            stream.Seek(5, SeekOrigin.Begin);

            return (DataStruct.Container)formatter.Deserialize(stream);
        }

        private void HandleContainerDataOther(byte[] containerData)
        {
            DataStruct.Container container = HandleContainerDataOwn(containerData);
        }

        private byte[] HandleContainerData(DataStruct.Container container)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, container);

            byte[] data = new byte[stream.Length + tcpProtocol.PLAYER_CONTAINER_DATA.Length + 1];

            tcpProtocol.PLAYER_CONTAINER_DATA.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.PLAYER_CONTAINER_DATA.Length + 1, (int)stream.Length);

            return data;
        }

        private void buildHyperloop(ref GameObjects.Player.Player current)
        {
            int counter = 0;

            for (int i = 0; i < globalContainer.Data.GetLength(0); i++)
            {
                for (int j = 0; j < globalContainer.Data.GetLength(0); j++)
                {
                    if (globalContainer.Data[i, j] == null)
                        continue;

                    if (globalContainer.Data[i, j] is DataStruct.Edge)
                    {
                        DataStruct.Edge edge = (DataStruct.Edge)globalContainer.Data[i, j];

                        if (edge.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP)
                        {
                            counter++;
                            Console.WriteLine(edge.PositionY + ", " + edge.PositionX + ": Hyperloop");
                        }
                    }
                }
            }

            if (counter > 0)
            {
                Console.WriteLine(current.Name + " bitte eine Position für deine Hyperloop wählen (y,x)");
                int positionY = Convert.ToInt32(Console.ReadLine());
                int positionX = Convert.ToInt32(Console.ReadLine());

                DataStruct.Edge choosedEdge = (DataStruct.Edge)globalContainer.Data[positionY, positionX];

                if (choosedEdge.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP)
                {
                    current.Hyperloops--;
                    choosedEdge.BuildTyp = DataStruct.BuildTypes.HYPERLOOP;
                    choosedEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                    choosedEdge.Building = new GameObjects.Buildings.Hyperloop(current.PlayerID);
                }
            }
        }

        private void buildCityOrOutpost(ref GameObjects.Player.Player current)
        {
            int counter = 0;

            for (int i = 0; i < globalContainer.Angles.GetLength(0); i++)
            {
                for (int j = 0; j < globalContainer.Angles.GetLength(1); j++)
                {
                    if (globalContainer.Angles[i, j] == null)
                        continue;

                    if (globalContainer.Angles[i, j].BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST)
                    {
                        Console.WriteLine(globalContainer.Angles[i, j].PositionY + ", " + globalContainer.Angles[i, j].PositionX + ": Außenposten");
                        counter++;
                    }
                    else if (globalContainer.Angles[i, j].BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY)
                    {
                        Console.WriteLine(globalContainer.Angles[i, j].PositionY + ", " + globalContainer.Angles[i, j].PositionX + ": Stadt");
                        counter++;
                    }
                }
            }

            if (counter > 0)
            {
                Console.WriteLine(current.Name + " bitte eine Position für dein/e Außenposten/Stadt wählen (y,x)");
                int positionY = Convert.ToInt32(Console.ReadLine());
                int positionX = Convert.ToInt32(Console.ReadLine());

                DataStruct.Angle choosedAngle = globalContainer.Angles[positionY, positionX];

                if (choosedAngle.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY)
                {
                    current.Cities--;
                    current.Outposts++;
                    choosedAngle.BuildTyp = DataStruct.BuildTypes.CITY;
                    choosedAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                    choosedAngle.Building = new GameObjects.Buildings.City(current.PlayerID);
                }
                else if (choosedAngle.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST)
                {
                    current.Outposts--;
                    choosedAngle.BuildTyp = DataStruct.BuildTypes.OUTPOST;
                    choosedAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                    choosedAngle.Building = new GameObjects.Buildings.Outpost(current.PlayerID);
                }
            }
        }

        public void Start()
        {
            const string exitString = "exit";
            string input = "";

            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[0], tcpProtocol.PLAYER_READY));
            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[1], tcpProtocol.PLAYER_READY));
            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[2], tcpProtocol.PLAYER_READY));

            gameLogic.Signal();

            object txObject;

            while (input != exitString)
            {
                while (gameLogic.TxQueue.Count < 1);

                gameLogic.TxQueue.TryDequeue(out txObject);

                if (txObject is GameLogic.TransmitMessage)
                {
                    GameLogic.TransmitMessage message = (GameLogic.TransmitMessage)txObject;

                    if (tcpProtocol.IsServerDataPattern(message.Data))
                    {
                        byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                        if (tcpProtocol.SERVER_NEED_PLAYER_NAME.SequenceEqual(equalBytes))
                        {
                            if(message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_ALL)
                            {
                                gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[0], HandlePlayerName("Hans")));
                                gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[1], HandlePlayerName("Kathi")));
                                gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[2], HandlePlayerName("Max")));

                                gameLogic.Signal();
                            }
                        }
                        else if(tcpProtocol.SERVER_PLAYER_DATA.SequenceEqual(equalBytes))
                        {
                            if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                            {
                                GameObjects.Player.Player tmp = HandlePlayerData(message.Data);

                                if(!players.Contains(tmp))
                                {
                                    players.Add(tmp);
                                }
                                else
                                {
                                    GameObjects.Player.Player current = (from p in players where p.PlayerID == tmp.PlayerID select p).First();

                                    players.Remove(current);
                                    players.Add(tmp);
                                }
                            }
                        }
                        else if (tcpProtocol.SERVER_STAGE_FOUNDATION_ROLL_DICE.SequenceEqual(equalBytes))
                        {
                            if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_ALL)
                            {
                                foreach(GameObjects.Player.Player element in players)
                                {
                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(element.ClientIP, HandleRollDice(new Random().Next(1,12))));
                                }

                                gameLogic.Signal();
                            }
                        }
                        else if (tcpProtocol.SERVER_CONTAINER_DATA_OWN.SequenceEqual(equalBytes))
                        {
                            if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                            {
                                GameObjects.Player.Player current = (from p in players where p.ClientIP.ToString() == message.IPToSend.ToString() select p).First();

                                globalContainer = HandleContainerDataOwn(message.Data);

                                buildHyperloop(ref current);

                                buildCityOrOutpost(ref current);

                                byte[] data = HandlePlayerData(current);

                                gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                                data = HandleContainerData(globalContainer);

                                gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));                                

                                gameLogic.Signal();

                            }
                        }
                        else if (tcpProtocol.SERVER_CONTAINER_DATA_OTHER.SequenceEqual(equalBytes))
                        {
                            if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OTHER)
                            {
                                /*foreach (GameObjects.Player.Player element in players)
                                {
                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(element.ClientIP, HandleRollDice(new Random().Next(1, 12))));
                                }

                                gameLogic.Signal();*/
                            }
                        }

                    }
                }
            }
        }

        
    }
}
