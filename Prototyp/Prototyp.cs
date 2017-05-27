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
        GameObjects.Player.Player current;
        Random random = new Random();

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

        private byte[] HandleRollDice(int diceNumber, byte[] protocol)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, diceNumber);

            byte[] data = new byte[stream.Length + protocol.Length + 1];

            protocol.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, protocol.Length + 1, (int)stream.Length);

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

        private byte[] HandleDeal(GameObjects.Menu.Cards.Resources.ResourceCard playerCard, GameObjects.Menu.Cards.Resources.ResourceCard serverCard)
        {
            MemoryStream streamPlayerCard = new MemoryStream();

            MemoryStream streamServerCard = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(streamPlayerCard, playerCard);

            byte[] data = new byte[streamPlayerCard.Length + streamServerCard.Length + 1 + tcpProtocol.PLAYER_CONTAINER_DATA.Length + 1];

            tcpProtocol.PLAYER_DEAL.CopyTo(data, 0);

            streamPlayerCard.Position = 0;
            streamServerCard.Position = 0;

            streamPlayerCard.Read(data, tcpProtocol.PLAYER_CONTAINER_DATA.Length + 1, (int)streamPlayerCard.Length);
            streamServerCard.Read(data, (int)streamPlayerCard.Length + tcpProtocol.PLAYER_CONTAINER_DATA.Length + 1, (int)streamServerCard.Length);

            return data;
        }

        private int  buildHyperloop(ref GameObjects.Player.Player current)
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

            return counter;
        }

        private int buildCity(ref GameObjects.Player.Player current)
        {
            int counter = 0;

            for (int i = 0; i < globalContainer.Angles.GetLength(0); i++)
            {
                for (int j = 0; j < globalContainer.Angles.GetLength(1); j++)
                {
                    if (globalContainer.Angles[i, j] == null)
                        continue;

                    if (globalContainer.Angles[i, j].BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY)
                    {
                        Console.WriteLine(globalContainer.Angles[i, j].PositionY + ", " + globalContainer.Angles[i, j].PositionX + ": Stadt");
                        counter++;
                    }
                }
            }
            return counter;            
        }

        private int buildOutpost(ref GameObjects.Player.Player current)
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
                   
                }
            }
            return counter;
        }

        GameObjects.GameStage.GameStages currentStage = GameObjects.GameStage.GameStages.NONE;

        private void HandleDeal()
        {
            Console.WriteLine("Welche karten möchten Sie handeln " + current.Name);
            Console.WriteLine("(1) Biomasse " + current.ResourceCardsBiomass);
            Console.WriteLine("(2) Carbonfaser " + current.ResourceCardsCarbonFibres);
            Console.WriteLine("(3) Deuterium " + current.ResourceCardsDeuterium);
            Console.WriteLine("(4) Freundliche Aliens " + current.ResourceCardsFriendlyAlien);
            Console.WriteLine("(5) Titan " + current.ResourceCardsTitan);
            Console.WriteLine("(6) Abbrechen");

            int value = Convert.ToInt32(Console.ReadLine());
            GameObjects.Menu.Cards.Resources.ResourceCard playerCard = null;

            switch (value)
            {
                case 1:
                    playerCard = new GameObjects.Menu.Cards.Resources.Biomass();
                    break;
                case 2:
                    playerCard = new GameObjects.Menu.Cards.Resources.CarbonFibres();
                    break;
                case 3:
                    playerCard = new GameObjects.Menu.Cards.Resources.Deuterium();
                    break;
                case 4:
                    playerCard = new GameObjects.Menu.Cards.Resources.FriendlyAlien();
                    break;
                case 5:
                    playerCard = new GameObjects.Menu.Cards.Resources.Titan();
                    break;
                case 6:
                    currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_BUILD;
                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                    gameLogic.Signal();
                    return;
            }

            Console.WriteLine("Gegen welche karten möchten Sie handeln " + current.Name);
            Console.WriteLine("(1) Biomasse ");
            Console.WriteLine("(2) Carbonfaser ");
            Console.WriteLine("(3) Deuterium");
            Console.WriteLine("(4) Freundliche Aliens");
            Console.WriteLine("(5) Titan");
            Console.WriteLine("(6) Abbrechen");

            value = Convert.ToInt32(Console.ReadLine());
            GameObjects.Menu.Cards.Resources.ResourceCard serverCard = null;

            switch (value)
            {
                case 1:
                    serverCard = new GameObjects.Menu.Cards.Resources.Biomass();
                    break;
                case 2:
                    serverCard = new GameObjects.Menu.Cards.Resources.CarbonFibres();
                    break;
                case 3:
                    serverCard = new GameObjects.Menu.Cards.Resources.Deuterium();
                    break;
                case 4:
                    serverCard = new GameObjects.Menu.Cards.Resources.FriendlyAlien();
                    break;
                case 5:
                    serverCard = new GameObjects.Menu.Cards.Resources.Titan();
                    break;
                case 6:
                    currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_BUILD;
                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                    gameLogic.Signal();

                    return;
            }

           byte[] data = HandleDeal(playerCard, serverCard);

            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));
        }

        private void handleBuild(GameObjects.GameStage.GameStages nextStage)
        {
            Console.WriteLine("Was möchten Sie Bauen " + current.Name);
            Console.WriteLine("(1) Straße, 1 Carbonfaser, 1 Deuterium");
            Console.WriteLine("(2) Außenposten 1 Carbonfaser, 1 Deuterium, 1 freundliche Alien, 1 Biomasse");
            Console.WriteLine("(3) Stadt, 3 Titan, 3 Biomasse");
            Console.WriteLine("(4) Abbrechen");

            Console.WriteLine("Biomasse " + current.ResourceCardsBiomass);
            Console.WriteLine("Carbonfaser " + current.ResourceCardsCarbonFibres);
            Console.WriteLine("Deuterium " + current.ResourceCardsDeuterium);
            Console.WriteLine("Freundliche Aliens " + current.ResourceCardsFriendlyAlien);
            Console.WriteLine("Titan " + current.ResourceCardsTitan);

            int value = Convert.ToInt32(Console.ReadLine());

            int positionY = 0;
            int positionX = 0;
            byte[] data;
            switch (value)
            {
                case 1:
                    if (buildHyperloop(ref current) > 0)
                    {
                        Console.WriteLine(current.Name + " bitte eine Position für deine Hyperloop wählen (y,x)");
                        positionY = Convert.ToInt32(Console.ReadLine());
                        positionX = Convert.ToInt32(Console.ReadLine());

                        DataStruct.Edge choosedEdge = (DataStruct.Edge)globalContainer.Data[positionY, positionX];

                        if (choosedEdge.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP)
                        {
                            current.Hyperloops--;
                            choosedEdge.BuildTyp = DataStruct.BuildTypes.HYPERLOOP;
                            choosedEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                            choosedEdge.Building = new GameObjects.Buildings.Hyperloop(current.PlayerID);
                        }

                        data = HandlePlayerData(current);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        data = HandleContainerData(globalContainer);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        gameLogic.Signal();

                    }
                    else
                    {
                        Console.WriteLine("Sie können keine Straße Bauen " + current.Name);
                        currentStage = nextStage;

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                        gameLogic.Signal();
                    }

                    break;
                case 2:
                    if (buildOutpost(ref current) > 0)
                    {
                        Console.WriteLine(current.Name + " bitte eine Position für dein Außenposten wählen (y,x)");
                        positionY = Convert.ToInt32(Console.ReadLine());
                        positionX = Convert.ToInt32(Console.ReadLine());

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

                        data = HandlePlayerData(current);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        data = HandleContainerData(globalContainer);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        gameLogic.Signal();

                    }
                    else
                    {
                        Console.WriteLine("Sie können keinen Außenposten Bauen " + current.Name);
                        currentStage = nextStage;

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                        gameLogic.Signal();
                    }


                    break;
                case 3:
                    if (buildCity(ref current) > 0)
                    {
                        Console.WriteLine(current.Name + " bitte eine Position für deine Stadt wählen (y,x)");
                        positionY = Convert.ToInt32(Console.ReadLine());
                        positionX = Convert.ToInt32(Console.ReadLine());

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

                        data = HandlePlayerData(current);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        data = HandleContainerData(globalContainer);

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                        gameLogic.Signal();
                    }
                    else
                    {
                        Console.WriteLine("Sie können keine Stadt Bauen " + current.Name);
                        currentStage = nextStage;

                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                        gameLogic.Signal();
                    }


                    break;
                case 4:
                    currentStage = nextStage;

                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                    gameLogic.Signal();

                    break;
            }
        }
        
        public void Start()
        {
            const string exitString = "exit";
            string input = "";
            int andererCounter = 0;
            int roundCounter = 0;

            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[0], tcpProtocol.PLAYER_READY));
            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[1], tcpProtocol.PLAYER_READY));
            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[2], tcpProtocol.PLAYER_READY));

            gameLogic.Signal();

            object txObject;

            int foundationCounter = 0;

            while (input != exitString)
            {
                while (gameLogic.TxQueue.Count < 1);

                gameLogic.TxQueue.TryDequeue(out txObject);

                if(foundationCounter < (3 * 3 + 3))
                {
                    #region Foundation Stage
                    if (txObject is GameLogic.TransmitMessage)
                    {
                        GameLogic.TransmitMessage message = (GameLogic.TransmitMessage)txObject;

                        if (tcpProtocol.IsServerDataPattern(message.Data))
                        {
                            byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                            if (tcpProtocol.SERVER_NEED_PLAYER_NAME.SequenceEqual(equalBytes))
                            {
                                if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_ALL)
                                {
                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[0], HandlePlayerName("Hans")));
                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[1], HandlePlayerName("Kathi")));
                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(clients[2], HandlePlayerName("Max")));

                                    gameLogic.Signal();
                                }
                            }
                            else if (tcpProtocol.SERVER_PLAYER_DATA.SequenceEqual(equalBytes))
                            {
                                if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                                {
                                    GameObjects.Player.Player tmp = HandlePlayerData(message.Data);

                                    GameObjects.Player.Player tmp2 = (from p in players where p.PlayerID == tmp.PlayerID select p).FirstOrDefault();

                                    if (tmp2 == null)
                                    {
                                        players.Add(tmp);
                                    }
                                    else
                                    {
                                        current = (from p in players where p.PlayerID == tmp.PlayerID select p).First();

                                        players.Remove(current);
                                        players.Add(tmp);
                                    }
                                }
                            }
                            else if (tcpProtocol.SERVER_STAGE_FOUNDATION_ROLL_DICE.SequenceEqual(equalBytes))
                            {
                                if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_ALL)
                                {
                                    foreach (GameObjects.Player.Player element in players)
                                    {
                                        gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(element.ClientIP, HandleRollDice(random.Next(1, 12), tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE)));
                                    }

                                    gameLogic.Signal();
                                }
                            }
                            else if (tcpProtocol.SERVER_CONTAINER_DATA_OWN.SequenceEqual(equalBytes))
                            {
                                if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                                {
                                    current = (from p in players where p.ClientIP.ToString() == message.IPToSend.ToString() select p).First();

                                    globalContainer = HandleContainerDataOwn(message.Data);

                                    if(buildHyperloop(ref current) > 0)
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

                                    if(buildOutpost(ref current) > 0)
                                    {

                                        Console.WriteLine(current.Name + " bitte eine Position für deinen Außenposten wählen (y,x)");
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

                                    byte[] data = HandlePlayerData(current);

                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                                    data = HandleContainerData(globalContainer);

                                    gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, data));

                                    gameLogic.Signal();

                                    foundationCounter++;

                                    if (foundationCounter >= (3 * 3 + 3))
                                        currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;
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

                    #endregion
                }
                else
                {
                    if (txObject is GameLogic.TransmitMessage)
                    {
                        GameLogic.TransmitMessage message = (GameLogic.TransmitMessage)txObject;

                        if (tcpProtocol.IsServerDataPattern(message.Data))
                        {
                            byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                            if (tcpProtocol.SERVER_PLAYER_DATA.SequenceEqual(equalBytes))
                            {
                                if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                                {
                                    andererCounter++;

                                    GameObjects.Player.Player tmp = HandlePlayerData(message.Data);

                                    GameObjects.Player.Player tmp2 = (from p in players where p.PlayerID == tmp.PlayerID select p).FirstOrDefault();

                                    if (tmp2 == null)
                                    {
                                        players.Add(tmp);
                                    }
                                    else
                                    {
                                        GameObjects.Player.Player current = (from p in players where p.PlayerID == tmp.PlayerID select p).First();

                                        players.Remove(current);
                                        players.Add(tmp);
                                    }

                                    if (andererCounter == clients.Count * 2)
                                    {
                                        currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_DEAL;
                                        andererCounter = 0;
                                        roundCounter++;
                                    }
                                    else if (roundCounter > 0 && andererCounter == 3)
                                    {
                                        currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_DEAL;
                                        andererCounter = 0;
                                    }
                                }
                            }
                            int positionY = 0;
                            int positionX = 0;

                            byte[] data = null;

                            switch (currentStage)
                            {


                                case GameObjects.GameStage.GameStages.PLAYER_STAGE_ROLL_DICE:
                                    if (tcpProtocol.SERVER_CONTAINER_DATA_OWN.SequenceEqual(equalBytes))
                                    {
                                        if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                                        {
                                            current = (from p in players where p.ClientIP.ToString() == message.IPToSend.ToString() select p).First();

                                            globalContainer = HandleContainerDataOwn(message.Data);

                                            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, HandleRollDice(random.Next(1,12), tcpProtocol.PLAYER_ROLL_DICE)));

                                            gameLogic.Signal();                                           
                                                
                                        }
                                            
                                    }
                                    break;
                                case GameObjects.GameStage.GameStages.PLAYER_STAGE_DEAL:

                                    if (tcpProtocol.SERVER_CONTAINER_DATA_OWN.SequenceEqual(equalBytes))
                                    {
                                        if (message.TransmitTyp == GameLogic.TransmitMessage.TransmitTyps.TO_OWN)
                                        {
                                            current = (from p in players where p.ClientIP.ToString() == message.IPToSend.ToString() select p).First();

                                            globalContainer = HandleContainerDataOwn(message.Data);

                                            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, HandleRollDice(random.Next(1, 12), tcpProtocol.PLAYER_ROLL_DICE)));

                                            gameLogic.Signal();

                                            currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_DEAL;
                                        }

                                    }

                                    Console.WriteLine("Was wollen sie tun " + current.Name);
                                    Console.WriteLine("(1) handeln");
                                    Console.WriteLine("(2) bauen");
                                    Console.WriteLine("(3) Entwicklungskarte spielen");
                                    Console.WriteLine("(4) Fertig");

                                    int value = Convert.ToInt32(Console.ReadLine());
                                    data = null;

                                    switch (value)
                                    {
                                        case 1:
                                            HandleDeal();
                                            break;
                                        case 2:
                                            handleBuild(GameObjects.GameStage.GameStages.PLAYER_STAGE_BUILD);
                                            break;
                                        case 3:
                                            Console.WriteLine("Noch nicht implementiert");
                                            break;
                                        case 4:
                                            currentStage = GameObjects.GameStage.GameStages.PLAYER_STAGE_BUILD;
                                            gameLogic.RxQueue.Enqueue(new GameLogic.RecieveMessage(current.ClientIP, tcpProtocol.PLAYER_READY));

                                            gameLogic.Signal();
                                            break;
                                    }

                                    break;
                                case GameObjects.GameStage.GameStages.PLAYER_STAGE_BUILD:
                                    handleBuild(GameObjects.GameStage.GameStages.PLAYER_STAGE_ROLL_DICE);
                                    break;

                            }
                        }
                    }
                }                
            }
        }        
    }
}
