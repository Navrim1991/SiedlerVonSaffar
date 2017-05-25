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
        private Queue<GameObjects.Player.Player> Players;
        private short currentPlayerID = 0;
        public GameObjects.Player.Player currentPlayer { get; private set; }
        private DataStruct.Container containerData;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;
        private TcpIpProtocol tcpProtocol;
        private GameObjects.GameStage.GameStages gameStage;

        private readonly short COUNT_RESOURCE_CARDS = 24;

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
            Players = new Queue<GameObjects.Player.Player>();
            containerData = new DataStruct.Container();
            RxQueue = new ConcurrentQueue<object>();
            TxQueue = new ConcurrentQueue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            tcpProtocol = new TcpIpProtocol();

            this.ResourceCardsBiomass = new List<GameObjects.Menu.Cards.Resources.Biomass>(COUNT_RESOURCE_CARDS);
            this.ResourceCardsCarbonFibres = new List<GameObjects.Menu.Cards.Resources.CarbonFibres>(COUNT_RESOURCE_CARDS);
            this.ResourceCardsDeuterium = new List<GameObjects.Menu.Cards.Resources.Deuterium>(COUNT_RESOURCE_CARDS);
            this.ResourceCardsFriendlyAlien = new List<GameObjects.Menu.Cards.Resources.FriendlyAlien>(COUNT_RESOURCE_CARDS);
            this.ResourceCardsTitan = new List<GameObjects.Menu.Cards.Resources.Titan>(COUNT_RESOURCE_CARDS);

            for (int i = 0; i < COUNT_RESOURCE_CARDS; i++)
            {
                this.ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass());
                this.ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres());
                this.ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium());
                this.ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien());
                this.ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan());
            }

            gameLogicThread.Start();
        }

        #region Handle Data

        /*private void HandelContainerData(GameObjects.Player.Player player)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, dataContainer);

            byte[] data = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_DATA.Length];

            data.SetValue(tcpProtocol.SERVER_DATA, 0);
            data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_DATA.Length + 1);

            //TODO: data loschicken nur wie
        }


        private void HandlePlayer(byte[] playerData)
        {
            MemoryStream stream = new MemoryStream(playerData);

            stream.Seek(5, SeekOrigin.End);

            IFormatter formatter = new BinaryFormatter();

            GameObjects.Player.Player player = (GameObjects.Player.Player)formatter.Deserialize(stream);

            //TODO player Data handeln            
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

            for (int i = 0; i < palyerCard.Length; i++)
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
                        upperAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Building;
                    else
                        upperAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Building;
                }

                if (element.LowerAngel.Building != null)
                {
                    if (element.LowerAngel.BuildTyp == DataStruct.BuildTypes.CITY)
                        lowerAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Building;
                    else
                        lowerAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Building;
                }

                if (upperAngleBuilding != null && upperAngleBuilding.PlayerID == currentPlayer.PlayerID)
                {
                    if (element.SpecialHarbor != null && element.SpecialHarbor.GetType() == resourceCardPlayer.GetType())
                    {
                        dealRelation = 2;
                        break;
                    }
                    else
                        dealRelation = 3;
                }
                if (dealRelation > 2)
                {
                    if (lowerAngleBuilding != null && lowerAngleBuilding.PlayerID == currentPlayer.PlayerID)
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

            if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Biomass)
            {
                if (currentPlayer.ResourceCardsBiomass.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsBiomass.Remove(currentPlayer.ResourceCardsBiomass.First());
                        ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass());
                    }
                }

            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                if (currentPlayer.ResourceCardsCarbonFibres.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsCarbonFibres.Remove(currentPlayer.ResourceCardsCarbonFibres.First());
                        ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                if (currentPlayer.ResourceCardsDeuterium.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsDeuterium.Remove(currentPlayer.ResourceCardsDeuterium.First());
                        ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                if (currentPlayer.ResourceCardsFriendlyAlien.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsFriendlyAlien.Remove(currentPlayer.ResourceCardsFriendlyAlien.First());
                        ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Titan)
            {
                if (currentPlayer.ResourceCardsTitan.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsTitan.Remove(currentPlayer.ResourceCardsTitan.First());
                        ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan());
                    }
                }
            }

            if (canDeal)
            {
                stream = new MemoryStream();
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, currentPlayer);

                byte[] data = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                data.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                //TODO: data loschicken nur wie
            }
            else
            {
                //TODO: data loschicken nur wie
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

            for (int i = 0; i < dataContainer.Hexagons.GetLength(0); i++)
            {
                for (int j = 0; j < dataContainer.Hexagons.GetLength(1); j++)
                {
                    if (dataContainer.Hexagons[i, j].HexagonID == (valueDiceOne + valueDiceTwo))
                        hexagons.Add(dataContainer.Hexagons[i, j]);
                }
            }

            foreach (DataStruct.Hexagon element in hexagons)
            {
                foreach (DataStruct.Angle innerElement in element.Angles)
                {
                    if (innerElement.Building != null)
                    {
                        GameObjects.Buildings.Building building = null;

                        if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                            building = (GameObjects.Buildings.City)innerElement.Building;
                        else
                            building = (GameObjects.Buildings.Outpost)innerElement.Building;

                        foreach (GameObjects.Player.Player playerElement in Players)
                        {
                            if (playerElement.PlayerID == building.PlayerID)
                            {
                                int countResources = 1;

                                if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                                    countResources++;

                                if (element.HexagonTyp == DataStruct.HexagonTypes.BIOMASS_FACTORY)
                                {
                                    if (countResources <= ResourceCardsBiomass.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsBiomass.RemoveAt(0);
                                            playerElement.ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.COAL_MINE)
                                {
                                    if (countResources <= ResourceCardsCarbonFibres.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsCarbonFibres.RemoveAt(0);
                                            playerElement.ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.DEUTERIUM_GAS_FIELD)
                                {
                                    if (countResources <= ResourceCardsDeuterium.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsDeuterium.RemoveAt(0);
                                            playerElement.ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.FRIENDLY_ALIEN)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsFriendlyAlien.RemoveAt(0);
                                            playerElement.ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.TITAN_MINE)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsTitan.RemoveAt(0);
                                            playerElement.ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (GameObjects.Player.Player playerElement in Players)
            {
                stream = new MemoryStream();
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, currentPlayer);

                byte[] playerData = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                data.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                data.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                //TODO: data loschicken nur wie
            }
        }*/

        private void HandleDeal(byte[] dealData)
        {
            byte[] palyerCard = new byte[(dealData.Length - 4) / 2];
            byte[] serverCard = new byte[(dealData.Length - 4) / 2];

            for (int i = 0; i < palyerCard.Length; i++)
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

            foreach (DataStruct.Harbor element in containerData.Harbors)
            {
                upperAngleBuilding = null;
                lowerAngleBuilding = null;

                if (element.UpperAngel.BuildTyp != DataStruct.BuildTypes.NONE)
                {
                    if (element.UpperAngel.BuildTyp == DataStruct.BuildTypes.CITY)
                        upperAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Building;
                    else
                        upperAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Building;
                }

                if (element.LowerAngel.Building != null)
                {
                    if (element.LowerAngel.BuildTyp == DataStruct.BuildTypes.CITY)
                        lowerAngleBuilding = (GameObjects.Buildings.City)element.UpperAngel.Building;
                    else
                        lowerAngleBuilding = (GameObjects.Buildings.Outpost)element.UpperAngel.Building;
                }

                if (upperAngleBuilding != null && upperAngleBuilding.PlayerID == currentPlayer.PlayerID)
                {
                    if (element.SpecialHarbor != null && element.SpecialHarbor.GetType() == resourceCardPlayer.GetType())
                    {
                        dealRelation = 2;
                        break;
                    }
                    else
                        dealRelation = 3;
                }
                if (dealRelation > 2)
                {
                    if (lowerAngleBuilding != null && lowerAngleBuilding.PlayerID == currentPlayer.PlayerID)
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

            if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Biomass)
            {
                if (currentPlayer.ResourceCardsBiomass.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsBiomass.Remove(currentPlayer.ResourceCardsBiomass.First());
                        ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass());
                    }
                }

            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                if (currentPlayer.ResourceCardsCarbonFibres.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsCarbonFibres.Remove(currentPlayer.ResourceCardsCarbonFibres.First());
                        ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                if (currentPlayer.ResourceCardsDeuterium.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsDeuterium.Remove(currentPlayer.ResourceCardsDeuterium.First());
                        ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                if (currentPlayer.ResourceCardsFriendlyAlien.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsFriendlyAlien.Remove(currentPlayer.ResourceCardsFriendlyAlien.First());
                        ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien());
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Titan)
            {
                if (currentPlayer.ResourceCardsTitan.Count >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsTitan.Remove(currentPlayer.ResourceCardsTitan.First());
                        ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan());
                    }
                }
            }

            if (canDeal)
            {
                stream = new MemoryStream();
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, currentPlayer);

                byte[] playerData = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                playerData.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                playerData.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                TxQueue.Enqueue(new TransmitMessage(playerData));
            }
            else
            {
                //TODO: data loschicken nur wie
            }

        }


        private void HandleRollDice(int diceNumber)
        {

            List<DataStruct.Hexagon> hexagons = new List<DataStruct.Hexagon>();

            for (int i = 0; i < containerData.Hexagons.GetLength(0); i++)
            {
                for (int j = 0; j < containerData.Hexagons.GetLength(1); j++)
                {
                    if (containerData.Hexagons[i, j].HexagonID == (diceNumber))
                        hexagons.Add(containerData.Hexagons[i, j]);
                }
            }

            HandleResources(hexagons);
        }

        //TODO spielregel noch nicht richtig implementiert
        private void HandleResources(List<DataStruct.Hexagon> hexagons)
        {
            foreach (DataStruct.Hexagon element in hexagons)
            {
                if (element == null)
                    continue;

                foreach (DataStruct.Angle innerElement in element.Angles)
                {
                    if (innerElement.Building != null)
                    {
                        GameObjects.Buildings.Building building = null;

                        if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                            building = (GameObjects.Buildings.City)innerElement.Building;
                        else
                            building = (GameObjects.Buildings.Outpost)innerElement.Building;

                        foreach (GameObjects.Player.Player playerElement in Players)
                        {
                            if (playerElement.PlayerID == building.PlayerID)
                            {
                                int countResources = 1;

                                if (innerElement.BuildTyp == DataStruct.BuildTypes.CITY)
                                    countResources++;

                                if (element.HexagonTyp == DataStruct.HexagonTypes.BIOMASS_FACTORY)
                                {
                                    if (countResources <= ResourceCardsBiomass.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsBiomass.RemoveAt(0);
                                            playerElement.ResourceCardsBiomass.Add(new GameObjects.Menu.Cards.Resources.Biomass());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.COAL_MINE)
                                {
                                    if (countResources <= ResourceCardsCarbonFibres.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsCarbonFibres.RemoveAt(0);
                                            playerElement.ResourceCardsCarbonFibres.Add(new GameObjects.Menu.Cards.Resources.CarbonFibres());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.DEUTERIUM_GAS_FIELD)
                                {
                                    if (countResources <= ResourceCardsDeuterium.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsDeuterium.RemoveAt(0);
                                            playerElement.ResourceCardsDeuterium.Add(new GameObjects.Menu.Cards.Resources.Deuterium());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.FRIENDLY_ALIEN)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsFriendlyAlien.RemoveAt(0);
                                            playerElement.ResourceCardsFriendlyAlien.Add(new GameObjects.Menu.Cards.Resources.FriendlyAlien());
                                        }
                                    }
                                }
                                else if (element.HexagonTyp == DataStruct.HexagonTypes.TITAN_MINE)
                                {
                                    if (countResources <= ResourceCardsFriendlyAlien.Count)
                                    {
                                        for (int i = 0; i < countResources; i++)
                                        {
                                            ResourceCardsTitan.RemoveAt(0);
                                            playerElement.ResourceCardsTitan.Add(new GameObjects.Menu.Cards.Resources.Titan());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (GameObjects.Player.Player playerElement in Players)
            {
                MemoryStream stream = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, playerElement);

                byte[] playerData = new byte[stream.GetBuffer().Length + tcpProtocol.SERVER_PLAYER_DATA.Length];

                playerData.SetValue(tcpProtocol.SERVER_PLAYER_DATA, 0);
                playerData.SetValue(stream.GetBuffer(), tcpProtocol.SERVER_PLAYER_DATA.Length + 1);

                TxQueue.Enqueue(new TransmitMessage(playerData));
            }
        }

        private void HandelContainerData(byte[] containerData)
        {
            MemoryStream stream = new MemoryStream(containerData);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            this.containerData = (DataStruct.Container)formatter.Deserialize(stream);

            DataStruct.Angle[,] angles = this.containerData.Angles;

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

                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                }
            }

            DataStruct.DataStruct[,] edges = this.containerData.Data;

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

                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                }
            }
        }

        private GameObjects.Player.Player HandelPlayerData(byte[] playerData)
        {
            MemoryStream stream = new MemoryStream(playerData);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return (GameObjects.Player.Player)formatter.Deserialize(stream);
        }

        private void HandelContainerData()
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, containerData);

            byte[] data = new byte[stream.Length + tcpProtocol.SERVER_CONTAINER_DATA_OWN.Length + 1];

            tcpProtocol.SERVER_CONTAINER_DATA_OWN.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.SERVER_CONTAINER_DATA_OWN.Length + 1, (int)stream.Length);

            TxQueue.Enqueue(new TransmitMessage(currentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));

            data = new byte[stream.Length + tcpProtocol.SERVER_CONTAINER_DATA_OTHER.Length + 1];

            tcpProtocol.SERVER_CONTAINER_DATA_OTHER.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, tcpProtocol.SERVER_CONTAINER_DATA_OTHER.Length + 1, (int)stream.Length);

            TxQueue.Enqueue(new TransmitMessage(currentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OTHER));
        }

        #endregion

        private int HandleRollDice(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);

            stream.Seek(5, SeekOrigin.Begin);

            IFormatter formatter = new BinaryFormatter();

            int number = (int)formatter.Deserialize(stream);

            return number;
        }

        private string HandlePlayerName(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);

            stream.Seek(5, SeekOrigin.Begin);

            IFormatter formatter = new BinaryFormatter();

            string player = (string)formatter.Deserialize(stream);

            return player;
        }

        private void nextPlayer()
        {
            Players.Enqueue(currentPlayer);
            currentPlayer = Players.Dequeue();
        }

        private EventWaitHandle waitHandle  = new EventWaitHandle(false, EventResetMode.ManualReset);

        public void Signal()
        {
            waitHandle.Set();
        }

        private void Start()
        {
            #region Is Client Data
            //LOGIC DEPRICATED
            /*if (rxObject is byte[])
            {
                byte[] buffer = (byte[])rxObject;

                
                if (tcpProtocol.isClientDataPattern(buffer))
                {
                    byte[] equalBytes = { buffer[0], buffer[1], buffer[2], buffer[3] };

                    if (tcpProtocol.PLAYER_TURN.SequenceEqual(equalBytes))
                    {
                        HandelContainerData(buffer);

                        currentPlayerID++;
                        if (currentPlayerID > Players.Count)
                            currentPlayerID = 0;

                        foreach (GameObjects.Player.Player element in Players)
                        {
                            if (element.PlayerID != currentPlayerID && element.PlayerID != currentPlayer.PlayerID)
                            {
                                HandelContainerData(element);
                            }
                        }

                        foreach (GameObjects.Player.Player element in Players)
                        {
                            if (element.PlayerID == currentPlayerID)
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
                        HandlePlayer(buffer);
                    }
                    else if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                    {
                        HandleDeal(buffer);
                    }
                    else if (tcpProtocol.PLAYER_ROLL_DICE.SequenceEqual(equalBytes))
                    {
                        HandleRollDice(buffer);
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

                


            }*/

            #endregion

            int diceNumber = 0;
            int diceRolled = 0;
            int foundationStageRoundCounter = 0;

            object rxObject; 
            while (true)
            {
                bool dequeue = RxQueue.TryDequeue(out rxObject);

                if (!dequeue)
                {
                    waitHandle.Reset();
                    waitHandle.WaitOne();
                    continue;
                }                    

                if (GameHasStarted)
                {
                    switch (gameStage)
                    {
                        case GameStage.GameStages.FOUNDATION_STAGE:

                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_NAME.SequenceEqual(equalBytes))
                                    {
                                        string playerName = HandlePlayerName(message.Data);
                                        //TODO FARBE SETZEN
                                        GameObjects.Player.Player tmpPlayer = new GameObjects.Player.Player(playerName, (short)(Players.Count + 1), message.ClientIP);

                                        tmpPlayer.Cities = 4;

                                        tmpPlayer.Outposts = 5;

                                        tmpPlayer.Hyperloops = 15;

                                        Players.Enqueue(tmpPlayer);

                                        MemoryStream stream = new MemoryStream();

                                        IFormatter formatter = new BinaryFormatter();

                                        formatter.Serialize(stream, tmpPlayer);

                                        byte[] data = new byte[stream.Length + tcpProtocol.SERVER_PLAYER_DATA.Length + 1];

                                        tcpProtocol.SERVER_PLAYER_DATA.CopyTo(data, 0);

                                        stream.Position = 0;

                                        stream.Read(data, tcpProtocol.SERVER_PLAYER_DATA.Length + 1, (int)stream.Length);

                                        TxQueue.Enqueue(new TransmitMessage(tmpPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));
                                    }
                                }
                            }

                            if (Players.Count == PlayersReady)
                            {
                                TxQueue.Enqueue(new TransmitMessage(tcpProtocol.SERVER_STAGE_FOUNDATION_ROLL_DICE));
                                gameStage = GameStage.GameStages.FOUNDATION_STAGE_ROLLING_DICE;
                            }                                

                            break;
                        case GameStage.GameStages.FOUNDATION_STAGE_ROLLING_DICE:
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE.SequenceEqual(equalBytes))
                                    {
                                        int number = HandleRollDice(message.Data);

                                        diceRolled++;

                                        if (number > diceNumber)
                                        {
                                            diceNumber = number;

                                            for(int i = 0; i < Players.Count; i++)
                                            {
                                                GameObjects.Player.Player tmp = Players.Peek();

                                                if (tmp.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
                                                {
                                                    currentPlayer = Players.Dequeue();
                                                    break;
                                                }                                                    
                                                else
                                                {
                                                    Players.Enqueue(Players.Dequeue());
                                                }
                                                
                                            }
                                        }    
                                    }
                                }
                            }

                            if(diceRolled == PlayersReady)
                            {
                                diceRolled = 0;
                                diceNumber = 0;

                                checkAngles();

                                HandelContainerData();

                                foundationStageRoundCounter++;

                                gameStage = GameStage.GameStages.FOUNDATION_STAGE_ROUND_ONE;
                            }                                
                            
                            break;
                        case GameStage.GameStages.FOUNDATION_STAGE_ROUND_ONE:
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        HandelContainerData(message.Data);

                                        if (foundationStageRoundCounter % 2 == 0)
                                        {
                                            checkAngles();

                                            HandelContainerData();

                                            foundationStageRoundCounter++;
                                        }
                                        else
                                        {
                                            checkEdges();

                                            HandelContainerData();

                                            foundationStageRoundCounter++;
                                        }

                                        if (foundationStageRoundCounter > Players.Count * 2 + 2)
                                        {
                                            gameStage = GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO;

                                            foundationStageRoundCounter = 1;                                            
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        GameObjects.Player.Player tmp = HandelPlayerData(message.Data);

                                        if (currentPlayer.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString())
                                        {
                                            currentPlayer = tmp;

                                            if (foundationStageRoundCounter % 2 == 0)
                                                nextPlayer();
                                        }
                                    }
                                }                     
                            }
                            break;
                        case GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO:
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        HandelContainerData(message.Data);

                                        if (foundationStageRoundCounter % 2 == 0)
                                        {
                                            checkAngles();

                                            HandelContainerData();

                                            foundationStageRoundCounter++;

                                        }
                                        else
                                        {
                                            checkEdges();

                                            HandelContainerData();

                                            foundationStageRoundCounter++;
                                        }

                                        if (foundationStageRoundCounter > Players.Count * 2 + 2)
                                        {
                                            List<DataStruct.Hexagon> hexagons = new List<DataStruct.Hexagon>();

                                            for (int i = 0; i < containerData.Hexagons.GetLength(0); i++)
                                            {
                                                for (int j = 0; j < containerData.Hexagons.GetLength(1); j++)
                                                {
                                                    hexagons.Add(containerData.Hexagons[i, j]);
                                                }
                                            }

                                            HandleResources(hexagons);

                                            nextPlayer();

                                            ComputeGameRules();

                                            HandelContainerData();

                                            gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        if (currentPlayer.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
                                        {
                                            currentPlayer = HandelPlayerData(message.Data);

                                            if (foundationStageRoundCounter % 2 == 0)
                                                nextPlayer();
                                        }
                                    }
                                }
                            }
                            break;
                        case GameStage.GameStages.NONE:
                            break;
                        case GameStage.GameStages.PLAYER_STAGE_BUILD:
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        HandelContainerData(message.Data);

                                        ComputeGameRules();

                                        HandelContainerData();
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        nextPlayer();

                                        ComputeGameRules();

                                        HandelContainerData();

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;
                                    }
                                }
                            }

                            break;
                        case GameStage.GameStages.PLAYER_STAGE_DEAL:
                            
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                                    {
                                        HandleDeal(message.Data);
                                    }
                                    else if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        HandelContainerData(message.Data);

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_BUILD;
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        nextPlayer();

                                        ComputeGameRules();

                                        HandelContainerData();

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;
                                    }
                                }
                            }
                                        break;
                        case GameStage.GameStages.PLAYER_STAGE_ROLL_DICE:
                            if (rxObject is RecieveMessage)
                            {
                                RecieveMessage message = (RecieveMessage)rxObject;

                                if (tcpProtocol.IsClientDataPattern(message.Data))
                                {
                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_ROLL_DICE.SequenceEqual(equalBytes))
                                    {
                                        int number = HandleRollDice(message.Data);

                                        HandleRollDice(number);

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_DEAL;
                                    }
                                }
                            }

                            break;
                    }
                }
                else
                {
                    if (rxObject is RecieveMessage)
                    {
                        RecieveMessage message = (RecieveMessage)rxObject;

                        if (tcpProtocol.IsClientDataPattern(message.Data))
                        {
                            byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                            if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                            {
                                if (GameHasStarted == false)
                                {
                                    PlayersReady++;

                                    if (PlayersReady >= 3)
                                    {
                                        GameHasStarted = true;

                                        TxQueue.Enqueue(new TransmitMessage(tcpProtocol.SERVER_NEED_PLAYER_NAME));
                                        gameStage = GameStage.GameStages.FOUNDATION_STAGE;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private bool CanSetStructHyperloop()
        {
            if (currentPlayer.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.ResourceCardsDeuterium.Count > 0)
                return true;

            return false;
        }

        private bool CanSetStructOutpost()
        {
            if (currentPlayer.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.ResourceCardsDeuterium.Count > 0
                && currentPlayer.ResourceCardsFriendlyAlien.Count > 0 && currentPlayer.ResourceCardsBiomass.Count > 0)
                return true;

            return false;

        }

        private bool CanSetStructCity()
        {
            if (currentPlayer.ResourceCardsTitan.Count >= 3 && currentPlayer.ResourceCardsBiomass.Count > 2)
                return true;

            return false;
        }

        private void checkAngles()
        {
            DataStruct.Angle[,] angles = containerData.Angles;

            DataStruct.Angle tmpAngle;

            if (gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROLLING_DICE
                || gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_ONE
                || gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO)
            {
                int buildingCounter = 0;

                for (int i = 0; i < angles.GetLength(0); i++)
                {
                    for (int j = 0; j < angles.GetLength(1); j++)
                    {
                        if (angles[i, j] == null)
                            continue;

                        tmpAngle = angles[i, j];

                        if (tmpAngle.Hexagons.Count == 0)
                            continue;

                        if (tmpAngle.BuildTyp != DataStruct.BuildTypes.NONE)
                            continue;

                        tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;

                        foreach (DataStruct.Edge element in tmpAngle.Edges)
                        {
                            DataStruct.Angle upperAngle = element.UpperAngel;
                            DataStruct.Angle lowerAngle = element.LowerAngel;

                            

                            if (!(upperAngle.PositionX == tmpAngle.PositionX && upperAngle.PositionY == tmpAngle.PositionY))
                            {
                                if (upperAngle.Building != null)
                                {
                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                    break;
                                }
                            }

                            if (!(lowerAngle.PositionX == tmpAngle.PositionX && lowerAngle.PositionY == tmpAngle.PositionY))
                            {
                                if (lowerAngle.Building != null)
                                {
                                    tmpAngle.BuildStruct = tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                    break;
                                }
                            }               
                        }
                    }
                }
            }
            else
            {
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

                                tmpAngle.BuildStruct = tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;

                                //Gehe alle kanten an den Ecken durch
                                foreach (DataStruct.Edge element in tmpAngle.Edges)
                                {
                                    if (element.Building != null)
                                    {
                                        GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)tmpAngle.Building;

                                        //Wenn eine Kante eine Eigene Straße beinhaltet darf eine Außenposten gebaut werden
                                        if (hyperloop.PlayerID == currentPlayer.PlayerID)
                                        {
                                            DataStruct.Angle upperAngle = element.UpperAngel;
                                            DataStruct.Angle lowerAngle = element.LowerAngel;

                                            tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;

                                            if (!(upperAngle.PositionX == tmpAngle.PositionX && upperAngle.PositionY == tmpAngle.PositionY))
                                            {
                                                if (upperAngle.Building != null)
                                                {
                                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                                    break;
                                                }
                                            }

                                            if (!(lowerAngle.PositionX == tmpAngle.PositionX && lowerAngle.PositionY == tmpAngle.PositionY))
                                            {
                                                if (lowerAngle.Building != null)
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
                                GameObjects.Buildings.Outpost outpost = (GameObjects.Buildings.Outpost)tmpAngle.Building;

                                //Wenn der Außenposten dem spieler gehört, darf er eine Stadt Bauen
                                if (outpost.PlayerID == currentPlayer.PlayerID)
                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY;
                                else
                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                                break;
                        }
                    }
                }
            }

            
        }

        private void checkEdges()
        {
            DataStruct.DataStruct[,] edges = containerData.Data;

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
                            buildingUpperAngle = (GameObjects.Buildings.Outpost)tmpEdge.UpperAngel.Building;
                        else
                            buildingUpperAngle = (GameObjects.Buildings.City)tmpEdge.UpperAngel.Building;
                    }

                    //Wenn an der unteren Ecke ein Gebäude existiert
                    if (tmpEdge.LowerAngel.BuildTyp != DataStruct.BuildTypes.NONE)
                    {
                        if (tmpEdge.LowerAngel.BuildTyp == DataStruct.BuildTypes.OUTPOST)
                            buildingLowerAngle = (GameObjects.Buildings.Outpost)tmpEdge.LowerAngel.Building;
                        else
                            buildingLowerAngle = (GameObjects.Buildings.City)tmpEdge.LowerAngel.Building;
                    }

                    switch (tmpEdge.BuildTyp)
                    {
                        case DataStruct.BuildTypes.NONE:
                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                            //Wenn keine Gebäude oder nur ein Gebäude an dieser Kante existierem
                            if (gameStage == GameStage.GameStages.PLAYER_STAGE_BUILD
                                || gameStage == GameStage.GameStages.PLAYER_STAGE_DEAL)
                            {
                                if (buildingUpperAngle == null || buildingLowerAngle == null)
                                {
                                    //wenn eine eigene Hyperloop an der Kante angrenzt
                                    //Obere Kanten prüfen
                                    foreach (DataStruct.Edge element in tmpEdge.UpperAngel.Edges)
                                    {
                                        if (element.Building != null && element.PositionX != j && element.PositionY != i)
                                        {
                                            GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Building;

                                            if (hyperloop.PlayerID == currentPlayer.PlayerID)
                                            {
                                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                                break;
                                            }

                                        }
                                    }

                                    //Untere Kanten prüfen
                                    foreach (DataStruct.Edge element in tmpEdge.LowerAngel.Edges)
                                    {
                                        if (element.Building != null && element.PositionX != j && element.PositionY != i)
                                        {
                                            GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Building;

                                            if (hyperloop.PlayerID == currentPlayer.PlayerID)
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
                                if ((buildingUpperAngle != null && buildingUpperAngle.PlayerID == currentPlayer.PlayerID))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

                                //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                                if ((buildingLowerAngle != null && buildingLowerAngle.PlayerID == currentPlayer.PlayerID))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }
                            }
                            else if (gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_ONE
                                || gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO)
                            {
                                if ((buildingUpperAngle != null && buildingUpperAngle.PlayerID == currentPlayer.PlayerID))
                                {
                                    int counter = 0;
                                    foreach(DataStruct.Edge element in tmpEdge.UpperAngel.Edges)
                                    {
                                        if (element.Building != null)
                                            counter++;
                                    }

                                    if(counter == 0)
                                        tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

                                if ((buildingLowerAngle != null && buildingLowerAngle.PlayerID == currentPlayer.PlayerID))
                                {
                                    int counter = 0;
                                    foreach (DataStruct.Edge element in tmpEdge.LowerAngel.Edges)
                                    {
                                        if (element.Building != null)
                                            counter++;
                                    }

                                    if (counter == 0)
                                        tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

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
            if (CanSetStructCity() && CanSetStructOutpost())
                checkAngles();

            if (CanSetStructHyperloop())
                checkEdges();
        }
    }
}
