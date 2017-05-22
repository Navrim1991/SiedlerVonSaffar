using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiedlerVonSaffar.GameObjects;
using SiedlerVonSaffar.NetworkMessageProtocol;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace SiedlerVonSaffar.GameLogic
{
    public class GameLogic
    {
        private Dictionary<Socket, GameObjects.Player.Player> Players;
        private  short currentPlayerID = 0;
        public KeyValuePair<Socket, GameObjects.Player.Player> currentPlayer { get; private set; }
        private DataStruct.Container dataContainer;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;
        private TcpIpProtocol tcpProtocol;

        private List<GameObjects.Menu.Cards.Resources.Biomass> ResourceCardsBiomass { get; set; }
        private List<GameObjects.Menu.Cards.Resources.CarbonFibres> ResourceCardsCarbonFibres { get; set; }
        private List<GameObjects.Menu.Cards.Resources.Deuterium> ResourceCardsDeuterium { get; set; }
        private List<GameObjects.Menu.Cards.Resources.FriendlyAlien> ResourceCardsFriendlyAlien { get; set; }
        private List<GameObjects.Menu.Cards.Resources.Titan> ResourceCardsTitan { get; set; }

        public short PlayersReady { get; set; }
        public bool GameHasStarted { get; set; }

        public ConcurrentQueue<object> RxQueue { get; private set; }
        public ConcurrentQueue<object> TxQueue { get; private set; }

        public GameLogic()
        {
            PlayersReady = 0;
            GameHasStarted = false;
            Players = new Dictionary<Socket, GameObjects.Player.Player>();
            dataContainer = new DataStruct.Container();
            RxQueue = new ConcurrentQueue<object>();
            TxQueue = new ConcurrentQueue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            tcpProtocol = new TcpIpProtocol();

            this.ResourceCardsBiomass = new List<GameObjects.Menu.Cards.Resources.Biomass>();
            this.ResourceCardsCarbonFibres = new List<GameObjects.Menu.Cards.Resources.CarbonFibres>();
            this.ResourceCardsDeuterium = new List<GameObjects.Menu.Cards.Resources.Deuterium>();
            this.ResourceCardsFriendlyAlien = new List<GameObjects.Menu.Cards.Resources.FriendlyAlien>();
            this.ResourceCardsTitan = new List<GameObjects.Menu.Cards.Resources.Titan>();

            gameLogicThread.Start();
        }



        private void HandelContainerData(KeyValuePair<Socket, GameObjects.Player.Player> player)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, dataContainer);

            byte[] data = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_DATA.Length];

            data.SetValue(tcpProtocol.SERVER_DATA, 0);
            data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_DATA.Length + 1);

            NetworkMessageProtocol.SocketStateObject state = new SocketStateObject();
            state.buffer = data;
            state.WorkSocket = player.Key;

            TxQueue.Enqueue(state);
        }


        private void HandlePlayer(byte[] playerData, Socket socket)
        {
            MemoryStream stream = new MemoryStream(playerData);

            stream.Seek(5, SeekOrigin.End);

            IFormatter formatter = new BinaryFormatter();

            GameObjects.Player.Player player = (GameObjects.Player.Player)formatter.Deserialize(stream);

            if (!Players.ContainsValue(player))
            {                
                Players.Add(socket, player);
            }
            else
            {
                Players[socket] = player;
                //TODO: send playerSecureProxy to other players;
            }
        }

        private void HandelContainerData(byte[] containerData)
        {
            MemoryStream stream = new MemoryStream(containerData);

            stream.Seek(5, SeekOrigin.End);

            IFormatter formatter = new BinaryFormatter();

            DataStruct.Container dataContainer = (DataStruct.Container)formatter.Deserialize(stream);

            this.dataContainer = dataContainer;
        }

        private void HandleDeal(byte[] dealData)
        {
            byte[] palyerCard = new byte[(dealData.Length - 4) / 2];
            byte[] serverCard = new byte[(dealData.Length - 4) / 2];

            for(int i = 0; i < palyerCard.Length; i++)
            {
                palyerCard[i] = dealData[i + 4];
            }

            for (int i = 0; i < serverCard.Length; i++)
            {
                serverCard[i] = dealData[i + ((dealData.Length - 4) / 2)];
            }

            MemoryStream stream = new MemoryStream(palyerCard);

            IFormatter formatter = new BinaryFormatter();

            GameObjects.Menu.Cards.Resources.ResourceCard resourceCardPlayer = (GameObjects.Menu.Cards.Resources.ResourceCard)formatter.Deserialize(stream);

            stream = new MemoryStream(serverCard);

            GameObjects.Menu.Cards.Resources.ResourceCard resourceCardServer = (GameObjects.Menu.Cards.Resources.ResourceCard)formatter.Deserialize(stream);

            short dealRelation = 4;

            GameObjects.Buildings.Building upperAngleBuilding;
            GameObjects.Buildings.Building lowerAngleBuilding;

            foreach (DataStruct.Harbor element in dataContainer.Harbors)
            {
                upperAngleBuilding = null;
                lowerAngleBuilding = null;

                if (element.UpperAngel.BuildTyp != DataStruct.BuildTypes.NONE)
                {
                    if (element.UpperAngel.BuildTyp == DataStruct.BuildTypes.CITY)
                        upperAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Data;
                    else
                        upperAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Data;
                }

                if (element.LowerAngel.Data != null)
                {
                    if (element.LowerAngel.BuildTyp == DataStruct.BuildTypes.CITY)
                        lowerAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Data;
                    else
                        lowerAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Data;
                }

                if(upperAngleBuilding != null && upperAngleBuilding.PlayerID == currentPlayer.Value.PlayerID)
                {
                    if(element.SpecialHarbor != null && element.SpecialHarbor.GetType() == resourceCardPlayer.GetType())
                    {
                        dealRelation = 2;
                        break;
                    }
                    else
                        dealRelation = 3;
                }
                if(dealRelation > 2)
                {
                    if (lowerAngleBuilding != null && lowerAngleBuilding.PlayerID == currentPlayer.Value.PlayerID)
                    {
                        if (element.SpecialHarbor != null && element.SpecialHarbor.GetType() == resourceCardPlayer.Resource.GetType())
                        {
                            dealRelation = 2;
                            break;
                        }
                        else
                            dealRelation = 3;
                    }
                }              
            }

            bool canDeal = false;

            if(resourceCardPlayer is GameObjects.Menu.Cards.Resources.Biomass)
            {
                if (currentPlayer.Value.ResourceCardsBiomass.Count >= dealRelation)
                {
                    canDeal = true;

                    for(int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.Value.ResourceCardsBiomass.Remove(currentPlayer.Value.ResourceCardsBiomass.First());
                        ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass(null));
                    }
                }
                    
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                if (currentPlayer.Value.ResourceCardsCarbonFibres.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.Value.ResourceCardsCarbonFibres.Remove(currentPlayer.Value.ResourceCardsCarbonFibres.First());
                        ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres(null));
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                if (currentPlayer.Value.ResourceCardsDeuterium.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.Value.ResourceCardsDeuterium.Remove(currentPlayer.Value.ResourceCardsDeuterium.First());
                        ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium(null));
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                if (currentPlayer.Value.ResourceCardsFriendlyAlien.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.Value.ResourceCardsFriendlyAlien.Remove(currentPlayer.Value.ResourceCardsFriendlyAlien.First());
                        ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien(null));
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Titan)
            {
                if (currentPlayer.Value.ResourceCardsTitan.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.Value.ResourceCardsTitan.Remove(currentPlayer.Value.ResourceCardsTitan.First());
                        ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan(null));
                    }
                }
            }

            if(canDeal)
            { 
                stream = new MemoryStream();
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, currentPlayer.Value);

                byte[] data = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                data.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                SocketStateObject state = new SocketStateObject();
                state.buffer = data;
                state.WorkSocket = currentPlayer.Key;

                TxQueue.Enqueue(state);

                //TODO: send playerProxy to other players

                /*
                Glaube ich ist nicht notwendig
                data = new byte[serverCard.Length + tcpProtocol.SERVER_GIVE_RESOURCES.Length];

                data.SetValue(tcpProtocol.SERVER_GIVE_RESOURCES, 0);
                data.SetValue(serverCard, tcpProtocol.SERVER_GIVE_RESOURCES.Length + 1);

                state = new SocketStateObject();
                state.buffer = data;
                state.WorkSocket = currentPlayer.Key;

                TxQueue.Enqueue(state);*/
            }
            else
            {
                SocketStateObject state = new SocketStateObject();
                state.buffer = tcpProtocol.SERVER_CANT_GIVE_RESOURCES;
                state.WorkSocket = currentPlayer.Key;

                TxQueue.Enqueue(state);
            }

        }

        private void HandleRollDice(byte[] data)
        {
            byte[] diceOne = new byte[(data.Length - 4) / 2];
            byte[] diceTwo = new byte[(data.Length - 4) / 2];

            for (int i = 0; i < diceOne.Length; i++)
            {
                diceOne[i] = data[i + 4];
            }

            for (int i = 0; i < diceTwo.Length; i++)
            {
                diceTwo[i] = data[i + ((data.Length - 4) / 2)];
            }

            MemoryStream stream = new MemoryStream(diceOne);

            IFormatter formatter = new BinaryFormatter();

            int valueDiceOne = (int)formatter.Deserialize(stream);

            stream = new MemoryStream(diceTwo);

            int valueDiceTwo = (int)formatter.Deserialize(stream);

            List<DataStruct.Hexagon> hexagons = new List<DataStruct.Hexagon>();

            for(int i = 0; i < dataContainer.Hexagons.GetLength(0); i++)
            {
                for (int j = 0; j < dataContainer.Hexagons.GetLength(1); j++)
                {
                    if (dataContainer.Hexagons[i, j].HexagonID == (valueDiceOne + valueDiceTwo))
                        hexagons.Add(dataContainer.Hexagons[i, j]);
                }
            }

            foreach(DataStruct.Hexagon element in hexagons)
            {
                foreach(DataStruct.Angle innerElement in element.Angles)
                {
                    if (innerElement.Data != null)
                    {
                        GameObjects.Buildings.Building building = null;

                        if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                            building = (GameObjects.Buildings.City)innerElement.Data;
                        else
                            building = (GameObjects.Buildings.Outpost)innerElement.Data;

                        foreach (KeyValuePair<Socket, GameObjects.Player.Player> playerElement in Players)
                        {
                            if(playerElement.Value.PlayerID == building.PlayerID)
                            {
                                int countResources = 1;

                                if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                                    countResources++;
                               
                                if(element.Data is GameObjects.GameField.BiomassFactory)
                                {
                                    if(countResources <= ResourceCardsBiomass.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsBiomass.RemoveAt(0);
                                            playerElement.Value.ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass(null));
                                        }
                                    }                              
                                }
                                else if (element.Data is GameObjects.GameField.CoalMine)
                                {
                                    if (countResources <= ResourceCardsCarbonFibres.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsCarbonFibres.RemoveAt(0);
                                            playerElement.Value.ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres(null));
                                        }
                                    }
                                }
                                else if (element.Data is GameObjects.GameField.DeuteriumGasField)
                                {
                                    if (countResources <= ResourceCardsDeuterium.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsDeuterium.RemoveAt(0);
                                            playerElement.Value.ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium(null));
                                        }
                                    }   
                                }
                                else if (element.Data is GameObjects.GameField.FriendlyAlien)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsFriendlyAlien.RemoveAt(0);
                                            playerElement.Value.ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien(null));
                                        }
                                    }
                                }
                                else if (element.Data is GameObjects.GameField.TitanMine)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsTitan.RemoveAt(0);
                                            playerElement.Value.ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan(null));
                                        }
                                    }                                   
                                }
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<Socket, GameObjects.Player.Player> playerElement in Players)
            {
                stream = new MemoryStream();
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, currentPlayer.Value);

                byte[] playerData = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                data.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                SocketStateObject state = new SocketStateObject();
                state.buffer = playerData;
                state.WorkSocket = currentPlayer.Key;

                TxQueue.Enqueue(state);
            }
        }

        private void Start()
        {
            object rxObject; 
            while (true)
            {
                while (RxQueue.Count < 1) ;

                RxQueue.TryDequeue(out rxObject);

                if(rxObject is SocketStateObject)
                {
                    SocketStateObject state = (SocketStateObject)rxObject;
                    if (tcpProtocol.isClientDataPattern(state.buffer))
                    {
                        byte[] equalBytes = { state.buffer[0], state.buffer[1], state.buffer[2], state.buffer[3] };

                        if (tcpProtocol.PLAYER_TURN.SequenceEqual(equalBytes))
                        {
                            HandelContainerData(state.buffer);                            

                            currentPlayerID++;
                            if (currentPlayerID > Players.Count)
                                currentPlayerID = 0;

                            foreach (KeyValuePair<Socket, GameObjects.Player.Player> element in Players)
                            {
                                if(element.Value.PlayerID != currentPlayerID && element.Value.PlayerID != currentPlayer.Value.PlayerID)
                                {
                                    HandelContainerData(element);
                                }
                            }

                            foreach (KeyValuePair<Socket, GameObjects.Player.Player> element in Players)
                            {
                                if (element.Value.PlayerID == currentPlayerID)
                                {
                                    currentPlayer = element;
                                    break;
                                }
                            }

                            ComputeGameRules();

                            HandelContainerData(currentPlayer);
                        }
                        else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                        {
                            HandlePlayer(state.buffer, state.WorkSocket);                            
                        }
                        else if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                        {
                            HandleDeal(state.buffer);
                        }
                        else if (tcpProtocol.PLAYER_ROLL_DICE.SequenceEqual(equalBytes))
                        {
                            HandleRollDice(state.buffer);
                        }
                        else if (tcpProtocol.PLAYER_PLAY_PROGRESSCARD.SequenceEqual(equalBytes))
                        {
                            //spieler spielt prograss card
                        }
                        else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                        {
                            if (GameHasStarted == false)
                            {
                                PlayersReady++;

                                if (PlayersReady >= 3)
                                {
                                    GameHasStarted = true;
                                }                                    
                            }
                        }
                    }
                }
            }
        }
        private bool CanSetStructHyperloop()
        {
            if (currentPlayer.Value.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.Value.ResourceCardsDeuterium.Count > 0)
                return true;

            return false;
        }

        private bool CanSetStructOutpost()
        {
            if (currentPlayer.Value.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.Value.ResourceCardsDeuterium.Count > 0
                && currentPlayer.Value.ResourceCardsFriendlyAlien.Count > 0 && currentPlayer.Value.ResourceCardsBiomass.Count > 0)
                return true;

            return false;

        }

        private bool CanSetStructCity()
        {
            if (currentPlayer.Value.ResourceCardsTitan.Count >= 3 && currentPlayer.Value.ResourceCardsBiomass.Count > 2)
                return true;

            return false;
        }

        private void checkAngles()
        {
            DataStruct.Angle[,] angles = dataContainer.Angles;

            DataStruct.Angle tmpAngle;

            for (int i = 0; i < angles.GetLength(0); i++)
            {
                for (int j = 0; j < angles.GetLength(1); j++)
                {
                    if (angles[i, j] == null)
                        continue;

                    tmpAngle = angles[i, j];

                    if (tmpAngle.Hexagons.Count == 0)
                        continue;

                    switch (tmpAngle.BuildTyp)
                    {
                        case DataStruct.BuildTypes.NONE:

                            tmpAngle.BuildStruct = tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                            //Gehe alle kanten an den Ecken durch
                            foreach (DataStruct.Edge element in tmpAngle.Edges)
                            {
                                if (element.Data != null)
                                {
                                    GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)tmpAngle.Data;

                                    //Wenn eine Kante eine Eigene Straße beinhaltet darf eine Außenposten gebaut werden
                                    if (hyperloop.PlayerID == currentPlayer.Value.PlayerID)
                                    {
                                        DataStruct.Angle upperAngle = element.UpperAngel;
                                        DataStruct.Angle lowerAngle = element.LowerAngel;

                                        tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;

                                        GameObjects.Buildings.Building building = null;

                                        if (upperAngle.PositionX != tmpAngle.PositionX && upperAngle.PositionY != tmpAngle.PositionY)
                                        {
                                            if (upperAngle.Data != null)
                                            {
                                                tmpAngle.BuildStruct = tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                                break;
                                            }
                                        }

                                        if (lowerAngle.PositionX != tmpAngle.PositionX && lowerAngle.PositionY != tmpAngle.PositionY)
                                        {
                                            if (lowerAngle.Data != null)
                                            {
                                                tmpAngle.BuildStruct = tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case DataStruct.BuildTypes.OUTPOST:
                            GameObjects.Buildings.Outpost outpost = (GameObjects.Buildings.Outpost)tmpAngle.Data;

                            //Wenn der Außenposten dem spieler gehört, darf er eine Stadt Bauen
                            if (outpost.PlayerID == currentPlayer.Value.PlayerID)
                                tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY;
                            else
                                tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                            break;
                    }
                }
            }
        }

        private void checkEdges()
        {
            DataStruct.DataStruct[,] edges = dataContainer.Data;

            DataStruct.Edge tmpEdge;

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                for (int j = 0; j < edges.GetLength(1); j++)
                {
                    if (edges[i, j] == null)
                        continue;
                    else if (edges[i, j] is DataStruct.Hexagon)
                        continue;

                    tmpEdge = (DataStruct.Edge)edges[i, j];

                    GameObjects.Buildings.Outpost buildingUpperAngle = null;
                    GameObjects.Buildings.Outpost buildingLowerAngle = null;

                    //Wenn an der oberen Ecke ein Gebäude existiert 
                    if (tmpEdge.UpperAngel.BuildTyp != DataStruct.BuildTypes.NONE)
                    {
                        if (tmpEdge.UpperAngel.BuildTyp == DataStruct.BuildTypes.OUTPOST)
                            buildingUpperAngle = (GameObjects.Buildings.Outpost)tmpEdge.UpperAngel.Data;
                        else
                            buildingUpperAngle = (GameObjects.Buildings.City)tmpEdge.UpperAngel.Data;
                    }

                    //Wenn an der unteren Ecke ein Gebäude existiert
                    if (tmpEdge.LowerAngel.BuildTyp != DataStruct.BuildTypes.NONE)
                    {
                        if (tmpEdge.UpperAngel.BuildTyp == DataStruct.BuildTypes.OUTPOST)
                            buildingLowerAngle = (GameObjects.Buildings.Outpost)tmpEdge.UpperAngel.Data;
                        else
                            buildingLowerAngle = (GameObjects.Buildings.City)tmpEdge.UpperAngel.Data;
                    }

                    switch (tmpEdge.BuildTyp)
                    {
                        case DataStruct.BuildTypes.NONE:
                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                            //Wenn keine Gebäude oder nur ein Gebäude an dieser Kante existierem
                            if (buildingUpperAngle == null || buildingLowerAngle == null)
                            {
                                //wenn eine eigene Hyperloop an der Kante angrenzt
                                //Obere Kanten prüfen
                                foreach (DataStruct.Edge element in tmpEdge.UpperAngel.Edges)
                                {
                                    if (element.Data != null && element.PositionX != j && element.PositionY != i)
                                    {
                                        GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Data;

                                        if (hyperloop.PlayerID == currentPlayer.Value.PlayerID)
                                        {
                                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                            break;
                                        }

                                    }
                                }

                                //Untere Kanten prüfen
                                foreach (DataStruct.Edge element in tmpEdge.LowerAngel.Edges)
                                {
                                    if (element.Data != null && element.PositionX != j && element.PositionY != i)
                                    {
                                        GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Data;

                                        if (hyperloop.PlayerID == currentPlayer.Value.PlayerID)
                                        {
                                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                            break;
                                        }
                                    }
                                }

                                if (tmpEdge.BuildStruct == DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP)
                                    continue;
                            }

                            //Wenn an der oberen Ecke ein eigenes Gebäude existiert
                            if ((buildingUpperAngle != null && buildingUpperAngle.PlayerID == currentPlayer.Value.PlayerID))
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                continue;
                            }

                            //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                            if ((buildingLowerAngle != null && buildingUpperAngle.PlayerID == currentPlayer.Value.PlayerID))
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                continue;
                            }

                            break;
                        case DataStruct.BuildTypes.HYPERLOOP:
                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                            break;
                    }
                }
            }
        }

        private void ComputeGameRules()
        {
            if (!CanSetStructCity() && !CanSetStructOutpost())
                checkAngles();

            if (CanSetStructHyperloop())
                checkEdges();
        }
    }
}
