﻿using System;
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

        private GameObjects.Player.Player currentPlayer;

        public GameObjects.Player.Player CurrentPlayer
        {
            get
            {
                return currentPlayer;
            }
        }

        private DataStruct.Container containerData;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;
        private TcpIpProtocol tcpProtocol;
        private GameStage.GameStages gameStage;

        private readonly short COUNT_RESOURCE_CARDS = 24;

        private int ResourceCardsBiomass { get; set; }
        private int ResourceCardsCarbonFibres { get; set; }
        private int ResourceCardsDeuterium { get; set; }
        private int ResourceCardsFriendlyAlien { get; set; }
        private int ResourceCardsTitan { get; set; }

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

            this.ResourceCardsBiomass = COUNT_RESOURCE_CARDS;
            this.ResourceCardsCarbonFibres = COUNT_RESOURCE_CARDS;
            this.ResourceCardsDeuterium = COUNT_RESOURCE_CARDS;
            this.ResourceCardsFriendlyAlien = COUNT_RESOURCE_CARDS;
            this.ResourceCardsTitan = COUNT_RESOURCE_CARDS;

            gameLogicThread.Start();
        }

        #region Threading

        private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public void Signal()
        {
            waitHandle.Set();
        }

        #endregion

        #region Handle Data

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
                if (currentPlayer.ResourceCardsBiomass >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsBiomass--;
                        ResourceCardsBiomass++;
                    }
                }

            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                if (currentPlayer.ResourceCardsCarbonFibres >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsCarbonFibres--;
                        ResourceCardsCarbonFibres++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                if (currentPlayer.ResourceCardsDeuterium >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsDeuterium--;
                        ResourceCardsDeuterium++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                if (currentPlayer.ResourceCardsFriendlyAlien >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsFriendlyAlien--;
                        ResourceCardsFriendlyAlien++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Titan)
            {
                if (currentPlayer.ResourceCardsTitan >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        currentPlayer.ResourceCardsTitan--;
                        ResourceCardsTitan++;
                    }
                }
            }

            if (canDeal)
            {
                SerializePlayerData(currentPlayer);
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

        private void HandelResources(GameObjects.Player.Player player, GameObjects.Buildings.Building building, DataStruct.Angle angle, DataStruct.Hexagon hexagon)
        {
            
                int countResources = 1;

                if (angle.BuildTyp == DataStruct.BuildTypes.CITY)
                    countResources++;

                if (hexagon.HexagonTyp == DataStruct.HexagonTypes.BIOMASS_FACTORY)
                {
                    if (countResources <= ResourceCardsBiomass)
                    {
                        for (int i = 0; i < countResources; i++)
                        {
                            ResourceCardsBiomass--;
                            player.ResourceCardsBiomass++;
                        }
                    }
                }
                else if (hexagon.HexagonTyp == DataStruct.HexagonTypes.COAL_MINE)
                {
                    if (countResources <= ResourceCardsCarbonFibres)
                    {
                        for (int i = 0; i < countResources; i++)
                        {
                            ResourceCardsCarbonFibres--;
                            player.ResourceCardsCarbonFibres++;
                        }
                    }
                }
                else if (hexagon.HexagonTyp == DataStruct.HexagonTypes.DEUTERIUM_GAS_FIELD)
                {
                    if (countResources <= ResourceCardsDeuterium)
                    {
                        for (int i = 0; i < countResources; i++)
                        {
                            ResourceCardsDeuterium--;
                            player.ResourceCardsDeuterium++;
                        }
                    }
                }
                else if (hexagon.HexagonTyp == DataStruct.HexagonTypes.FRIENDLY_ALIEN)
                {
                    if (countResources <= ResourceCardsFriendlyAlien)
                    {
                        for (int i = 0; i < countResources; i++)
                        {
                            ResourceCardsFriendlyAlien--;
                            player.ResourceCardsFriendlyAlien++;
                        }
                    }
                }
                else if (hexagon.HexagonTyp == DataStruct.HexagonTypes.TITAN_MINE)
                {
                    if (countResources <= ResourceCardsFriendlyAlien)
                    {
                        for (int i = 0; i < countResources; i++)
                        {
                            ResourceCardsTitan--;
                            player.ResourceCardsTitan++;
                        }
                    }
                }
            
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

                        if (currentPlayer.PlayerID == building.PlayerID)
                        {
                            HandelResources(currentPlayer, building, innerElement, element);
                            continue;
                        }

                        foreach (GameObjects.Player.Player playerElement in Players)
                        {
                            if (playerElement.PlayerID == building.PlayerID)
                            {
                                HandelResources(playerElement, building, innerElement, element);
                                break;
                            }
                        }
                    }
                }
            }

            SerializePlayerData(currentPlayer);

            foreach (GameObjects.Player.Player playerElement in Players)
            {
                SerializePlayerData(playerElement);
            }
        }

        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private byte[] Serialize(byte[] protocol, object serializeableObject)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, serializeableObject);

            byte[] data = new byte[stream.Length + protocol.Length + 1];

            protocol.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, protocol.Length + 1, (int)stream.Length);

            return data;
        }

 

        private GameObjects.Player.Player HandelPlayerData(byte[] playerData)
        {
            MemoryStream stream = new MemoryStream(playerData);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return (GameObjects.Player.Player)formatter.Deserialize(stream);
        }

        private void SerializeContainerData()
        {
            byte[] data = this.Serialize(tcpProtocol.SERVER_CONTAINER_DATA_OWN, containerData);

            TxQueue.Enqueue(new TransmitMessage(currentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));

            data = this.Serialize(tcpProtocol.SERVER_CONTAINER_DATA_OTHER, containerData);

            TxQueue.Enqueue(new TransmitMessage(currentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OTHER));
        }

        private void SerializePlayerData(GameObjects.Player.Player player)
        {
            byte[] data = this.Serialize(tcpProtocol.SERVER_PLAYER_DATA, player);

            TxQueue.Enqueue(new TransmitMessage(player.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));

            //TODO make the proxy

            TxQueue.Enqueue(new TransmitMessage(player.ClientIP, data, TransmitMessage.TransmitTyps.TO_OTHER));
        }

        private void DeserializeContainerData(byte[] data)
        {
            this.containerData = (DataStruct.Container)Deserialize(data);

            ResetContainerDataBuildStruct();
        }

        #endregion

        #region Workaround Methods

        private void ResetContainerDataBuildStruct()
        {
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

        private void nextPlayer()
        {
            Players.Enqueue(currentPlayer);
            currentPlayer = Players.Dequeue();
        }

        private void FoundationStages(ref int foundationStageRoundCounter)
        {
            


            if (foundationStageRoundCounter % 2 == 0)
            {
                checkAngles();

                foundationStageRoundCounter++;

                if (foundationStageRoundCounter > Players.Count * 2 + 2
                    && gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO)
                    return;

                SerializeContainerData();               

            }
            else
            {
                checkEdges();

                foundationStageRoundCounter++;

                SerializeContainerData();                
            }
        }

        private void SetupNewPlayer(GameObjects.Player.Player newPlayer)
        {
            newPlayer.Cities = 4;

            newPlayer.Outposts = 5;

            newPlayer.Hyperloops = 15;

            Players.Enqueue(newPlayer);

            byte[] data = Serialize(tcpProtocol.SERVER_PLAYER_DATA, newPlayer);

            TxQueue.Enqueue(new TransmitMessage(newPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));
        }

        #endregion

        #region Game Rules

        private bool CanSetStructHyperloop()
        {
            if (currentPlayer.ResourceCardsCarbonFibres > 0 && currentPlayer.ResourceCardsDeuterium > 0)
                return true;

            return false;
        }

        private bool CanSetStructOutpost()
        {
            if (currentPlayer.ResourceCardsCarbonFibres > 0 && currentPlayer.ResourceCardsDeuterium > 0
                && currentPlayer.ResourceCardsFriendlyAlien > 0 && currentPlayer.ResourceCardsBiomass > 0)
                return true;

            return false;

        }

        private bool CanSetStructCity()
        {
            if (currentPlayer.ResourceCardsTitan >= 3 && currentPlayer.ResourceCardsBiomass > 2)
                return true;

            return false;
        }

        private bool IsBuildableAngle(DataStruct.Angle angle)
        {
            if (angle == null)
                return false;

            if (angle.Hexagons.Count == 0)
                return false;

            return true;

        }

        private bool IsSameAngle(DataStruct.Angle first, DataStruct.Angle second)
        {
            if ((first.PositionX == second.PositionX && first.PositionY == second.PositionY))
                return true;

            return false;
        }

        private bool CanAngleSetBuilding(DataStruct.Angle upperAngle, DataStruct.Angle lowerAngle, ref DataStruct.Angle currentAngle)
        {
            if (!IsSameAngle(upperAngle, currentAngle))
            {
                if (upperAngle.Building != null)
                {
                    currentAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                    return false; ;
                }
            }

            if (!IsSameAngle(lowerAngle, currentAngle))
            {
                if (lowerAngle.Building != null)
                {
                    currentAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                    return false; ;
                }
            }

            return true;
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

                        if (!IsBuildableAngle(angles[i, j]))
                            continue;

                        tmpAngle = angles[i, j];

                        if (tmpAngle.BuildTyp != DataStruct.BuildTypes.NONE)
                            continue;

                        tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;

                        foreach (DataStruct.Edge element in tmpAngle.Edges)
                        {
                            DataStruct.Angle upperAngle = element.UpperAngel;
                            DataStruct.Angle lowerAngle = element.LowerAngel;


                            if (!CanAngleSetBuilding(upperAngle, lowerAngle, ref tmpAngle))
                                break;
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
                        if (!IsBuildableAngle(angles[i, j]))
                            continue;

                        tmpAngle = angles[i, j];

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

                                            if (!CanAngleSetBuilding(upperAngle, lowerAngle, ref tmpAngle))
                                                break;
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

        private bool IsBuildingFromPlayer(GameObjects.Buildings.Building building, GameObjects.Player.Player player)
        {
            if ((building != null && building.PlayerID == player.PlayerID))
                return true;

            return false;
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
                                if (IsBuildingFromPlayer(buildingUpperAngle, currentPlayer))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

                                //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                                if (IsBuildingFromPlayer(buildingLowerAngle, currentPlayer))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }
                            }
                            else if (gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_ONE
                                || gameStage == GameStage.GameStages.FOUNDATION_STAGE_ROUND_TWO)
                            {
                                if (IsBuildingFromPlayer(buildingUpperAngle, currentPlayer))
                                {
                                    int counter = 0;
                                    foreach (DataStruct.Edge element in tmpEdge.UpperAngel.Edges)
                                    {
                                        if (element.Building != null)
                                            counter++;
                                    }

                                    if (counter == 0)
                                        tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

                                if (IsBuildingFromPlayer(buildingLowerAngle, currentPlayer))
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

        #endregion



        private void Start()
        {
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
                                        string playerName = (string)this.Deserialize(message.Data);
                                        //TODO FARBE SETZEN
                                        GameObjects.Player.Player newPlayer = new GameObjects.Player.Player(playerName, (short)(Players.Count + 1), message.ClientIP);

                                        SetupNewPlayer(newPlayer);
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
                                        int number = (int)Deserialize(message.Data);

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

                                SerializeContainerData();

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
                                        DeserializeContainerData(message.Data);

                                        FoundationStages(ref foundationStageRoundCounter);

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
                                        DeserializeContainerData(message.Data);

                                        FoundationStages(ref foundationStageRoundCounter);

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

                                            gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;

                                            ComputeGameRules();

                                            SerializeContainerData();

                                            
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
                                        DeserializeContainerData(message.Data);

                                        ComputeGameRules();

                                        SerializeContainerData();
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        nextPlayer();

                                        ComputeGameRules();

                                        SerializeContainerData();

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
                                        DeserializeContainerData(message.Data);

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_BUILD;
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        nextPlayer();

                                        ComputeGameRules();

                                        SerializeContainerData();

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
                                        int number = (int)Deserialize(message.Data);

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





    }
}
