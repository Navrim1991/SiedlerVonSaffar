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
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace SiedlerVonSaffar.GameLogic
{
    public class GameLogic
    {
        [NonSerialized]
        internal Queue<GameObjects.Player.Player> Players;

        private StateMachine.State CurrentState;

        internal GameObjects.Player.Player CurrentPlayer
        {
            get; set;
        }

        internal DataStruct.Container containerData;
        internal ThreadStart gameLogicThreadStart;
        internal Thread gameLogicThread;
        //internal GameStage.GameStages gameStage;
        internal bool playerPlayedProgressCardSteet;
        internal int roundCounter;
        internal int foundationStageRoundCounter = 0;
        internal readonly short COUNT_RESOURCE_CARDS = 24;
        internal int ResourceCardsBiomass;
        internal int ResourceCardsCarbonFibres;
        internal int ResourceCardsDeuterium;
        internal int ResourceCardsFriendlyAlien;
        internal int ResourceCardsTitan;

        internal List<GameObjects.Menu.Cards.Progress.ProgressCard> ProgressCards;

        public short PlayersReady;
        public bool GameHasStarted;

        public ConcurrentQueue<object> RxQueue { get; private set; }
        public ConcurrentQueue<object> TxQueue { get; private set; }

        public GameLogic()
        {
            playerPlayedProgressCardSteet = false;
            roundCounter = 1;
            Configuration.DeveloperParameter.init();
            PlayersReady = 0;
            GameHasStarted = false;
            Players = new Queue<GameObjects.Player.Player>();
            containerData = new DataStruct.Container();
            RxQueue = new ConcurrentQueue<object>();
            TxQueue = new ConcurrentQueue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            ProgressCards = new List<GameObjects.Menu.Cards.Progress.ProgressCard>();

            int i;

            for (i = 0; i < 14; i++)
                ProgressCards.Add(new GameObjects.Menu.Cards.Progress.SpaceMarine());

            for (i = 0; i < 2; i++)
            {
                ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Hyperloop());
                ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Monopoly());
                ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Invention());
            }

            ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Library());
            ProgressCards.Add(new GameObjects.Menu.Cards.Progress.SpaceHarbor());
            ProgressCards.Add(new GameObjects.Menu.Cards.Progress.ResearchInstitute());
            ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Senate());
            ProgressCards.Add(new GameObjects.Menu.Cards.Progress.Temple());

            Shuffle(ProgressCards);

            this.ResourceCardsBiomass = COUNT_RESOURCE_CARDS;
            this.ResourceCardsCarbonFibres = COUNT_RESOURCE_CARDS;
            this.ResourceCardsDeuterium = COUNT_RESOURCE_CARDS;
            this.ResourceCardsFriendlyAlien = COUNT_RESOURCE_CARDS;
            this.ResourceCardsTitan = COUNT_RESOURCE_CARDS;

            gameLogicThread.Start();
        }

        internal void SetState(StateMachine.State nextState)
        {
            CurrentState = nextState;
        }

        public void GetName(RecieveMessage rxObject)
        {
            CurrentState.GetName(rxObject);
        }
        public void DiceRolled(RecieveMessage rxObject)
        {
            CurrentState.DiceRolled(rxObject);
        }
        public void FoundationRoundAllSet(RecieveMessage rxObject)
        {
            CurrentState.FoundationRoundAllSet(rxObject);
        }
        public void FoundationRoundOne(RecieveMessage rxObject)
        {
            CurrentState.FoundationRoundOne(rxObject);
        }
        public void BuildingsSet(RecieveMessage rxObject)
        {
            CurrentState.BuildingsSet(rxObject);
        }
        public void Dealed(RecieveMessage rxObject)
        {
            CurrentState.Dealed(rxObject);
        }

        private void Shuffle(List<GameObjects.Menu.Cards.Progress.ProgressCard> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                GameObjects.Menu.Cards.Progress.ProgressCard value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #region Threading
        [NonSerialized]
        private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public void Signal()
        {
            Thread.Sleep(20);
            waitHandle.Set();
        }

        #endregion

        #region Handle Data

        internal void HandleBandit()
        {
            //TODO nur tempoärer nachher müssen die spieler ihre Rostoffkarten selbst auswählen
            Random rnd = new Random();
            double resourceCount = 0;

            foreach (GameObjects.Player.Player element in Players)
            {
                resourceCount = 0;
                resourceCount += element.ResourceCardsBiomass;
                resourceCount += element.ResourceCardsCarbonFibres;
                resourceCount += element.ResourceCardsDeuterium;
                resourceCount += element.ResourceCardsFriendlyAlien;
                resourceCount += element.ResourceCardsTitan;


                if (resourceCount > 7)
                {
                    resourceCount = Math.Ceiling(resourceCount / 2);

                    for (int i = 0; i < resourceCount; i++)
                    {
                        switch (rnd.Next(1, 5))
                        {
                            case 1:
                                if (element.ResourceCardsBiomass > 0)
                                    element.ResourceCardsBiomass--;
                                break;
                            case 2:
                                if (element.ResourceCardsCarbonFibres > 0)
                                    element.ResourceCardsCarbonFibres--;

                                break;
                            case 3:
                                if (element.ResourceCardsFriendlyAlien > 0)
                                    element.ResourceCardsFriendlyAlien--;

                                break;
                            case 4:
                                if (element.ResourceCardsDeuterium > 0)
                                    element.ResourceCardsDeuterium--;
                                break;
                            case 5:
                                if (element.ResourceCardsTitan > 0)
                                    element.ResourceCardsTitan--;
                                break;
                        }
                    }

                    SerializePlayerData(element);
                }
            }

            resourceCount = 0;
            resourceCount += CurrentPlayer.ResourceCardsBiomass;
            resourceCount += CurrentPlayer.ResourceCardsCarbonFibres;
            resourceCount += CurrentPlayer.ResourceCardsDeuterium;
            resourceCount += CurrentPlayer.ResourceCardsFriendlyAlien;
            resourceCount += CurrentPlayer.ResourceCardsTitan;


            if (resourceCount > 7)
            {
                resourceCount = Math.Ceiling(resourceCount / 2);

                for (int i = 0; i < resourceCount; i++)
                {
                    switch (rnd.Next(1, 5))
                    {
                        case 1:
                            if (CurrentPlayer.ResourceCardsBiomass > 0)
                                CurrentPlayer.ResourceCardsBiomass--;
                            break;
                        case 2:
                            if (CurrentPlayer.ResourceCardsCarbonFibres > 0)
                                CurrentPlayer.ResourceCardsCarbonFibres--;

                            break;
                        case 3:
                            if (CurrentPlayer.ResourceCardsFriendlyAlien > 0)
                                CurrentPlayer.ResourceCardsFriendlyAlien--;

                            break;
                        case 4:
                            if (CurrentPlayer.ResourceCardsDeuterium > 0)
                                CurrentPlayer.ResourceCardsDeuterium--;
                            break;
                        case 5:
                            if (CurrentPlayer.ResourceCardsTitan > 0)
                                CurrentPlayer.ResourceCardsTitan--;
                            break;
                    }
                }

                SerializePlayerData(CurrentPlayer);
            }

            DataStruct.Hexagon banditHexagon = null;

            for (int i = 0; i < containerData.Hexagons.GetLength(0); i++)
            {
                for (int j = 0; j < containerData.Hexagons.GetLength(1); j++)
                {
                    if (containerData.Hexagons[i, j] == null)
                        continue;

                    if (containerData.Hexagons[i, j].HasBandit)
                    {
                        banditHexagon = containerData.Hexagons[i, j];
                        break;
                    }
                }

                if (banditHexagon != null)
                    break;
            }

            bool flag = true;
            int counter = 1;


            foreach (DataStruct.Angle element in banditHexagon.Angles)
            {
                if (element == null)
                    continue;

                if (element.Building != null && element.Building.PlayerID != CurrentPlayer.PlayerID)
                {
                    GameObjects.Player.Player stealedPlayer = (from p in Players where p.PlayerID == element.Building.PlayerID select p).First();

                    while (flag && counter < 20)
                    {
                        switch (rnd.Next(1, 5))
                        {
                            case 1:
                                if (stealedPlayer.ResourceCardsBiomass > 0)
                                {
                                    CurrentPlayer.ResourceCardsBiomass++;
                                    stealedPlayer.ResourceCardsBiomass--;
                                    flag = false;
                                }
                                break;
                            case 2:
                                if (stealedPlayer.ResourceCardsCarbonFibres > 0)
                                {
                                    CurrentPlayer.ResourceCardsCarbonFibres++;
                                    stealedPlayer.ResourceCardsCarbonFibres--;
                                    flag = false;
                                }
                                break;
                            case 3:
                                if (stealedPlayer.ResourceCardsFriendlyAlien > 0)
                                {
                                    CurrentPlayer.ResourceCardsFriendlyAlien++;
                                    stealedPlayer.ResourceCardsFriendlyAlien--;
                                    flag = false;
                                }


                                break;
                            case 4:
                                if (stealedPlayer.ResourceCardsDeuterium > 0)
                                {
                                    CurrentPlayer.ResourceCardsDeuterium++;
                                    stealedPlayer.ResourceCardsDeuterium--;
                                    flag = false;
                                }
                                break;
                            case 5:
                                if (stealedPlayer.ResourceCardsTitan > 0)
                                {
                                    CurrentPlayer.ResourceCardsTitan++;
                                    stealedPlayer.ResourceCardsTitan--;
                                    flag = false;
                                }

                                break;
                        }

                        counter++;
                    }

                    if (!flag)
                    {
                        SerializePlayerData(stealedPlayer);
                        SerializePlayerData(CurrentPlayer);

                        break;
                    }
                }
            }
        }

        internal void HandleProgressCardHyperloop()
        {
            CheckEdges();

            SerializeContainerData();

            playerPlayedProgressCardSteet = true;
        }

        internal void HandleProgressCardMonopoly(GameObjects.Menu.Cards.Progress.Monopoly monopoly)
        {
            if (monopoly.ResourceCard is GameObjects.Menu.Cards.Resources.Biomass)
            {
                foreach (GameObjects.Player.Player element in Players)
                {
                    CurrentPlayer.ResourceCardsBiomass += element.ResourceCardsBiomass;
                    element.ResourceCardsBiomass = 0;


                    SerializePlayerData(element);
                }
            }
            else if (monopoly.ResourceCard is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                foreach (GameObjects.Player.Player element in Players)
                {
                    CurrentPlayer.ResourceCardsCarbonFibres += element.ResourceCardsCarbonFibres;
                    element.ResourceCardsCarbonFibres = 0;

                    SerializePlayerData(element);
                }
            }
            else if (monopoly.ResourceCard is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                foreach (GameObjects.Player.Player element in Players)
                {
                    CurrentPlayer.ResourceCardsDeuterium += element.ResourceCardsDeuterium;
                    element.ResourceCardsDeuterium = 0;

                    SerializePlayerData(element);
                }
            }
            else if (monopoly.ResourceCard is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                foreach (GameObjects.Player.Player element in Players)
                {
                    CurrentPlayer.ResourceCardsFriendlyAlien += element.ResourceCardsFriendlyAlien;
                    element.ResourceCardsFriendlyAlien = 0;

                    SerializePlayerData(element);
                }
            }
            else if (monopoly.ResourceCard is GameObjects.Menu.Cards.Resources.Titan)
            {
                foreach (GameObjects.Player.Player element in Players)
                {
                    CurrentPlayer.ResourceCardsTitan += element.ResourceCardsTitan;
                    element.ResourceCardsTitan = 0;

                    SerializePlayerData(element);
                }
            }
        }

        internal void HandleProgressCardInvention(GameObjects.Menu.Cards.Progress.Invention invention)
        {
            if (invention.ResourceCard is GameObjects.Menu.Cards.Resources.Biomass)
            {
                CurrentPlayer.ResourceCardsBiomass += 2;
                ResourceCardsBiomass -= 2;
            }
            else if (invention.ResourceCard is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                CurrentPlayer.ResourceCardsCarbonFibres += 2;
                ResourceCardsCarbonFibres -= 2;
            }
            else if (invention.ResourceCard is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                CurrentPlayer.ResourceCardsDeuterium += 2;
                ResourceCardsDeuterium -= 2;
            }
            else if (invention.ResourceCard is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                CurrentPlayer.ResourceCardsFriendlyAlien += 2;
                ResourceCardsFriendlyAlien -= 2;
            }
            else if (invention.ResourceCard is GameObjects.Menu.Cards.Resources.Titan)
            {
                CurrentPlayer.ResourceCardsTitan += 2;
                ResourceCardsTitan -= 2;
            }
        }

        internal void HandleProgresscardSpaceMarine()
        {
            List<GameObjects.Menu.Cards.Progress.ProgressCard> cards = (from p in CurrentPlayer.PlayedProgressCards where p.GetType() == typeof(GameObjects.Menu.Cards.Progress.SpaceMarine) select p).ToList();

            if (cards.Count > 3)
            {
                foreach (GameObjects.Menu.Cards.Victory.VictoryCard element in CurrentPlayer.VictoryCards)
                {
                    if (element is GameObjects.Menu.Cards.Victory.SpaceMarine)
                        return;
                }

                GameObjects.Menu.Cards.Victory.VictoryCard tmp = null;

                foreach (GameObjects.Player.Player element in Players)
                {
                    foreach (GameObjects.Menu.Cards.Victory.VictoryCard innerElement in element.VictoryCards)
                    {
                        if (innerElement is GameObjects.Menu.Cards.Victory.SpaceMarine)
                            tmp = innerElement;
                    }

                    if (tmp != null)
                    {
                        element.VictoryCards.Remove(tmp);

                        SerializePlayerData(element);

                        break;
                    }
                }

                CurrentPlayer.VictoryCards.Add(new GameObjects.Menu.Cards.Victory.SpaceMarine());

            }

            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, new TcpIpProtocol().SERVER_SET_BANDIT, TransmitMessage.TransmitTyps.TO_OWN));
        }

        internal void HandleProgressCards(byte[] data)
        {
            GameObjects.Menu.Cards.Progress.ProgressCard card = (GameObjects.Menu.Cards.Progress.ProgressCard)Deserialize(data);

            CurrentPlayer.PlayedProgressCards.Add(card);

            card = (from p in CurrentPlayer.ProgressCards where p.GetType() == card.GetType() select p).First();

            CurrentPlayer.ProgressCards.Remove(card);

            if (card is GameObjects.Menu.Cards.Progress.Hyperloop)
            {
                HandleProgressCardHyperloop();
            }
            else if (card is GameObjects.Menu.Cards.Progress.Monopoly)
            {
                HandleProgressCardMonopoly((GameObjects.Menu.Cards.Progress.Monopoly)card);
            }
            else if (card is GameObjects.Menu.Cards.Progress.Invention)
            {
                HandleProgressCardInvention((GameObjects.Menu.Cards.Progress.Invention)card);
            }
            else if (card is GameObjects.Menu.Cards.Progress.SpaceMarine)
            {
                HandleProgresscardSpaceMarine();
            }

            SerializePlayerData(CurrentPlayer);
        }

        internal void HandleDeal(byte[] dealData)
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

                if (upperAngleBuilding != null && upperAngleBuilding.PlayerID == CurrentPlayer.PlayerID)
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
                    if (lowerAngleBuilding != null && lowerAngleBuilding.PlayerID == CurrentPlayer.PlayerID)
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
                if (CurrentPlayer.ResourceCardsBiomass >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        CurrentPlayer.ResourceCardsBiomass--;
                        ResourceCardsBiomass++;
                    }
                }

            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.CarbonFibres)
            {
                if (CurrentPlayer.ResourceCardsCarbonFibres >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        CurrentPlayer.ResourceCardsCarbonFibres--;
                        ResourceCardsCarbonFibres++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Deuterium)
            {
                if (CurrentPlayer.ResourceCardsDeuterium >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        CurrentPlayer.ResourceCardsDeuterium--;
                        ResourceCardsDeuterium++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.FriendlyAlien)
            {
                if (CurrentPlayer.ResourceCardsFriendlyAlien >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        CurrentPlayer.ResourceCardsFriendlyAlien--;
                        ResourceCardsFriendlyAlien++;
                    }
                }
            }
            else if (resourceCardPlayer is GameObjects.Menu.Cards.Resources.Titan)
            {
                if (CurrentPlayer.ResourceCardsTitan >= dealRelation)
                {
                    canDeal = true;

                    for (int i = 0; i < dealRelation; i++)
                    {
                        CurrentPlayer.ResourceCardsTitan--;
                        ResourceCardsTitan++;
                    }
                }
            }

            if (canDeal)
            {
                SerializePlayerData(CurrentPlayer);
            }
            else
            {
                //TODO: data loschicken nur wie
            }

        }


        internal void HandleRollDice(int diceNumber)
        {

            List<DataStruct.Hexagon> hexagons = new List<DataStruct.Hexagon>();

            for (int i = 0; i < containerData.Hexagons.GetLength(0); i++)
            {
                for (int j = 0; j < containerData.Hexagons.GetLength(1); j++)
                {
                    if (containerData.Hexagons[i, j] == null)
                        continue;
                    if (containerData.Hexagons[i, j].HexagonID == (diceNumber) && !containerData.Hexagons[i, j].HasBandit)
                        hexagons.Add(containerData.Hexagons[i, j]);
                }
            }

            HandleResources(hexagons);
        }

        internal void HandelResources(GameObjects.Player.Player player, GameObjects.Buildings.Building building, DataStruct.Angle angle, DataStruct.Hexagon hexagon)
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
        internal void HandleResources(List<DataStruct.Hexagon> hexagons)
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

                        if (CurrentPlayer.PlayerID == building.PlayerID)
                        {
                            HandelResources(CurrentPlayer, building, innerElement, element);
                        }

                        foreach (GameObjects.Player.Player playerElement in Players)
                        {
                            if (playerElement.PlayerID == building.PlayerID)
                            {
                                HandelResources(playerElement, building, innerElement, element);
                            }
                        }
                    }
                }
            }

            SerializePlayerData(CurrentPlayer);

            foreach (GameObjects.Player.Player playerElement in Players)
            {
                SerializePlayerData(playerElement);
            }
        }

        internal object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        internal byte[] Serialize(byte[] protocol, object serializeableObject)
        {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new BinaryFormatter();

            try
            {
                formatter.Serialize(stream, serializeableObject);
            }
            catch (Exception ex)
            {
                string a = ex.Message;
                throw;
            }

            

            byte[] data = new byte[stream.Length + protocol.Length + 1];

            protocol.CopyTo(data, 0);

            stream.Position = 0;

            stream.Read(data, protocol.Length + 1, (int)stream.Length);

            return data;
        }



        internal GameObjects.Player.Player HandelPlayerData(byte[] playerData)
        {
            MemoryStream stream = new MemoryStream(playerData);
            stream.Seek(5, SeekOrigin.Begin);
            IFormatter formatter = new BinaryFormatter();

            return (GameObjects.Player.Player)formatter.Deserialize(stream);
        }

        internal void SerializeContainerData()
        {
            byte[] data = this.Serialize(new TcpIpProtocol().SERVER_CONTAINER_DATA_OWN, this.containerData);

            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));

            data = this.Serialize(new TcpIpProtocol().SERVER_CONTAINER_DATA_OTHER, containerData);

            if (!Configuration.DeveloperParameter.IsPrototyp)
                TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OTHER));
        }

        internal void SerializePlayerData(GameObjects.Player.Player player)
        {
            byte[] data = this.Serialize(new TcpIpProtocol().SERVER_PLAYER_DATA, player);

            TxQueue.Enqueue(new TransmitMessage(player.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));

            //TODO make the proxy

            if(!Configuration.DeveloperParameter.IsPrototyp)
                TxQueue.Enqueue(new TransmitMessage(player.ClientIP, data, TransmitMessage.TransmitTyps.TO_OTHER));
        }

        internal void DeserializeContainerData(byte[] data)
        {
            this.containerData = (DataStruct.Container)Deserialize(data);

            ResetContainerDataBuildStruct();
        }

        #endregion

        #region Workaround Methods

        internal int CheckVictory(GameObjects.Player.Player player)
        {
            int points = (from p in player.PlayedProgressCards where p.GetType() == typeof(SiedlerVonSaffar.GameObjects.Menu.Cards.Progress.VictoryPoint) select p).Count();

            points += player.VictoryCards.Count * 2;

            for(int i = 0; i < containerData.Angles.GetLength(0); i++)
            {
                for (int j = 0; j < containerData.Angles.GetLength(1); j++)
                {
                    if (containerData.Angles[i, j] == null)
                        continue;

                    DataStruct.Angle tmpAngle = containerData.Angles[i, j];

                    if(tmpAngle.Building != null && tmpAngle.Building.PlayerID == player.PlayerID)
                    {
                        if (tmpAngle.BuildTyp == DataStruct.BuildTypes.CITY)
                            points += 2;
                        else
                            points++;
                    }
                }
            }


            return points;
        }

        internal void ResetContainerDataBuildStruct()
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

        internal void nextPlayer()
        {
            Players.Enqueue(CurrentPlayer);
            CurrentPlayer = Players.Dequeue();
        }

        internal void FoundationStages(ref int foundationStageRoundCounter)
        {         
            if (foundationStageRoundCounter % 2 == 0)
            {
                checkAngles();

                foundationStageRoundCounter++;

                if (foundationStageRoundCounter > Players.Count * 2 + 2
                    && CurrentState is StateMachine.FoundationStageRoundTwo)
                    return;

                //containerData.Notify();
                SerializeContainerData();

            }
            else
            {
                CheckEdges();

                foundationStageRoundCounter++;

                //containerData.Notify();
                SerializeContainerData();
            }
        }

        internal void SetupNewPlayer(GameObjects.Player.Player newPlayer)
        { 

            newPlayer.Cities = 4;

            newPlayer.Outposts = 5;

            newPlayer.Hyperloops = 15;

            Players.Enqueue(newPlayer);

            byte[] data = Serialize(new TcpIpProtocol().SERVER_PLAYER_DATA, newPlayer);

            TxQueue.Enqueue(new TransmitMessage(newPlayer.ClientIP, data, TransmitMessage.TransmitTyps.TO_OWN));
        }

        #endregion

        #region Game Rules

        internal bool CanSetStructHyperloop()
        {
            if (CurrentPlayer.ResourceCardsCarbonFibres > 0 && CurrentPlayer.ResourceCardsDeuterium > 0)
                return true;

            return false;
        }

        internal bool CanSetStructOutpost()
        {
            if (CurrentPlayer.ResourceCardsCarbonFibres > 0 && CurrentPlayer.ResourceCardsDeuterium > 0
                && CurrentPlayer.ResourceCardsFriendlyAlien > 0 && CurrentPlayer.ResourceCardsBiomass > 0)
                return true;

            return false;

        }

        internal bool CanSetStructCity()
        {
            if (CurrentPlayer.ResourceCardsTitan >= 3 && CurrentPlayer.ResourceCardsBiomass >= 2)
                return true;

            return false;
        }

        internal bool IsBuildableAngle(DataStruct.Angle angle)
        {
            if (angle == null)
                return false;

            if (angle.Hexagons.Count == 0)
                return false;

            return true;

        }

        internal bool IsSameAngle(DataStruct.Angle first, DataStruct.Angle second)
        {
            if ((first.PositionX == second.PositionX && first.PositionY == second.PositionY))
                return true;

            return false;
        }

        internal bool CanAngleSetBuilding(DataStruct.Angle upperAngle, DataStruct.Angle lowerAngle, ref DataStruct.Angle currentAngle)
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

        internal void checkAngles()
        {
            DataStruct.Angle[,] angles = containerData.Angles;

            DataStruct.Angle tmpAngle;

            
            if (CurrentState is StateMachine.FoundationStageRollDice
                || CurrentState is StateMachine.FoundationStageRoundOne
                || CurrentState is StateMachine.FoundationStageRoundTwo)
            {

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
                                        GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Building;

                                        //Wenn eine Kante eine Eigene Straße beinhaltet darf eine Außenposten gebaut werden
                                        if (hyperloop.PlayerID == CurrentPlayer.PlayerID)
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
                                if (outpost.PlayerID == CurrentPlayer.PlayerID)
                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY;
                                else
                                    tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                                break;
                        }
                    }
                }
            }
        }

        internal bool IsBuildingFromPlayer(GameObjects.Buildings.Building building, GameObjects.Player.Player player)
        {
            if ((building != null && building.PlayerID == player.PlayerID))
                return true;

            return false;
        }

        internal void CheckEdges()
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

                            
                            if (CurrentState is StateMachine.PlayerRollDice
                                || CurrentState is StateMachine.PlayerStageBuild
                                || CurrentState is StateMachine.PlayerStageDeal)
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

                                            if (hyperloop.PlayerID == CurrentPlayer.PlayerID)
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

                                            if (hyperloop.PlayerID == CurrentPlayer.PlayerID)
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
                                if (IsBuildingFromPlayer(buildingUpperAngle, CurrentPlayer))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }

                                //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                                if (IsBuildingFromPlayer(buildingLowerAngle, CurrentPlayer))
                                {
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                }
                            }
                            else if (CurrentState is StateMachine.FoundationStageRoundOne
                                        || CurrentState is StateMachine.FoundationStageRoundTwo)
                            {
                                if (IsBuildingFromPlayer(buildingUpperAngle, CurrentPlayer))
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

                                if (IsBuildingFromPlayer(buildingLowerAngle, CurrentPlayer))
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

        internal bool ComputeGameRules()
        {
            bool canSetStructCity = CanSetStructCity();
            bool canSetStructOutpost = CanSetStructOutpost();
            bool canSetStructHyperloop = CanSetStructHyperloop();


            if (canSetStructCity || canSetStructOutpost)
                checkAngles();

            if (canSetStructHyperloop)
                CheckEdges();

            return (canSetStructCity || canSetStructOutpost) || canSetStructHyperloop;
        }

        #endregion



        private void Start()
        {
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

                    #region deprecated StateMaschine (bessere Lösung?)

                    /*switch (gameStage)
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

                                            for (int i = 0; i < Players.Count; i++)
                                            {
                                                GameObjects.Player.Player tmp = Players.Dequeue();

                                                if (tmp.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
                                                {
                                                    if (CurrentPlayer != null)
                                                        Players.Enqueue(CurrentPlayer);

                                                    CurrentPlayer = tmp;
                                                    break;
                                                }
                                                else
                                                {
                                                    Players.Enqueue(tmp);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (diceRolled == PlayersReady)
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

                                            roundCounter++;

                                            foundationStageRoundCounter = 1;
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        GameObjects.Player.Player tmp = HandelPlayerData(message.Data);

                                        if (CurrentPlayer.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString())
                                        {
                                            CurrentPlayer = tmp;

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
                                                    if (containerData.Hexagons[i, j] != null && !containerData.Hexagons[i, j].HasBandit)
                                                        hexagons.Add(containerData.Hexagons[i, j]);
                                                }
                                            }

                                            HandleResources(hexagons);

                                            gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;

                                            ComputeGameRules();

                                            SerializeContainerData();
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        if (CurrentPlayer.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
                                        {
                                            CurrentPlayer = HandelPlayerData(message.Data);

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

                                    int a = RxQueue.Count;

                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        DeserializeContainerData(message.Data);

                                        if (playerPlayedProgressCardSteet)
                                        {
                                            CheckEdges();

                                            playerPlayedProgressCardSteet = false;

                                            SerializeContainerData();
                                        }
                                        else
                                        {
                                            int points = CheckVictory(CurrentPlayer);

                                            if (points >= 10)
                                            {
                                                //TODO VICTORY
                                                //ANDERE PLAYER ZÄHLEN
                                            }
                                            else
                                            {
                                                ComputeGameRules();

                                                SerializeContainerData();
                                            }
                                        }

                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        GameObjects.Player.Player tmp = HandelPlayerData(message.Data);

                                        if (CurrentPlayer.PlayerID == tmp.PlayerID)
                                            CurrentPlayer = tmp;
                                        else
                                        {
                                            GameObjects.Player.Player tmp2 = (from p in Players where p.PlayerID == tmp.PlayerID select p).FirstOrDefault();

                                            if (tmp2 != null)
                                                tmp2 = tmp;
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        nextPlayer();

                                        ComputeGameRules();

                                        SerializeContainerData();

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_ROLL_DICE;
                                    }
                                    else if (tcpProtocol.PLAYER_BUY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        if (PlayerCanBuyProgressCard() && ProgressCards.Count > 0)
                                        {
                                            CurrentPlayer.ProgressCards.Add(ProgressCards.Last());

                                            CurrentPlayer.ProgressCards.Last().Round = roundCounter;

                                            ProgressCards.Remove(ProgressCards.Last());

                                            SerializePlayerData(CurrentPlayer);
                                        }
                                        else
                                        {
                                            byte[] error = Serialize(tcpProtocol.SERVER_ERROR, "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen");

                                            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, error, TransmitMessage.TransmitTyps.TO_OWN));
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_PLAY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        HandleProgressCards(message.Data);

                                        int points = CheckVictory(CurrentPlayer);

                                        if (points > 10)
                                        {
                                            //TODO VICTORY
                                            //ANDERE PLAYER ZÄHLEN
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_SET_BANDIT.SequenceEqual(equalBytes))
                                    {
                                        DeserializeContainerData(message.Data);

                                        HandleBandit();
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
                                    int a = RxQueue.Count;

                                    byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                                    if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                                    {
                                        HandleDeal(message.Data);
                                    }
                                    else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                                    {
                                        GameObjects.Player.Player tmp = HandelPlayerData(message.Data);

                                        if (CurrentPlayer.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString())
                                            CurrentPlayer = tmp;
                                        else
                                        {
                                            GameObjects.Player.Player tmp2 = (from p in Players where p.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString() select p).FirstOrDefault();

                                            if (tmp2 != null)
                                                tmp2 = tmp;
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                                    {
                                        DeserializeContainerData(message.Data);

                                        if (playerPlayedProgressCardSteet)
                                        {
                                            CheckEdges();

                                            playerPlayedProgressCardSteet = false;

                                            SerializeContainerData();
                                        }
                                        else
                                        {
                                            int points = CheckVictory(CurrentPlayer);

                                            if (points >= 10)
                                            {
                                                //TODO VICTORY
                                                //ANDERE PLAYER ZÄHLEN
                                            }
                                            else
                                            {
                                                ComputeGameRules();

                                                SerializeContainerData();

                                                gameStage = GameStage.GameStages.PLAYER_STAGE_BUILD;
                                            }
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                                    {
                                        ComputeGameRules();

                                        SerializeContainerData();

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_BUILD;
                                    }
                                    else if (tcpProtocol.PLAYER_BUY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        if (PlayerCanBuyProgressCard() && ProgressCards.Count > 0)
                                        {
                                            CurrentPlayer.ProgressCards.Add(ProgressCards.Last());

                                            CurrentPlayer.ProgressCards.Last().Round = roundCounter;

                                            ProgressCards.Remove(ProgressCards.Last());

                                            SerializePlayerData(CurrentPlayer);
                                        }
                                        else
                                        {
                                            byte[] error = Serialize(tcpProtocol.SERVER_ERROR, "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen");

                                            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, error, TransmitMessage.TransmitTyps.TO_OWN));
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_PLAY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        HandleProgressCards(message.Data);

                                        int points = CheckVictory(CurrentPlayer);

                                        if (points > 10)
                                        {
                                            //TODO VICTORY
                                            //ANDERE PLAYER ZÄHLEN
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_SET_BANDIT.SequenceEqual(equalBytes))
                                    {
                                        DeserializeContainerData(message.Data);

                                        HandleBandit();
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
                                        roundCounter++;

                                        int number = (int)Deserialize(message.Data);

                                        //TODO 7 gewürfelt;
                                        if (number != 7)
                                        {
                                            HandleRollDice(number);

                                            ComputeGameRules();

                                            SerializeContainerData();
                                        }
                                        else
                                            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, tcpProtocol.SERVER_SET_BANDIT, TransmitMessage.TransmitTyps.TO_OWN));

                                        gameStage = GameStage.GameStages.PLAYER_STAGE_DEAL;
                                    }
                                    else if (tcpProtocol.PLAYER_BUY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        if (PlayerCanBuyProgressCard() && ProgressCards.Count > 0)
                                        {
                                            CurrentPlayer.ProgressCards.Add(ProgressCards.Last());

                                            CurrentPlayer.ProgressCards.Last().Round = roundCounter;

                                            ProgressCards.Remove(ProgressCards.Last());

                                            SerializePlayerData(CurrentPlayer);
                                        }
                                        else
                                        {
                                            byte[] error = Serialize(tcpProtocol.SERVER_ERROR, "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen");

                                            TxQueue.Enqueue(new TransmitMessage(CurrentPlayer.ClientIP, error, TransmitMessage.TransmitTyps.TO_OWN));
                                        }
                                    }
                                    else if (tcpProtocol.PLAYER_PLAY_PROGRESS_CARD.SequenceEqual(equalBytes))
                                    {
                                        HandleProgressCards(message.Data);

                                        int points = CheckVictory(CurrentPlayer);

                                        if (points > 10)
                                        {
                                            //TODO VICTORY
                                            //ANDERE PLAYER ZÄHLEN
                                        }
                                    }

                                }

                            }

                            break;
                    }*/

                    #endregion

                    if (rxObject is RecieveMessage)
                    {
                        if (CurrentState is StateMachine.FoundationStage)
                        {
                            GetName((RecieveMessage)rxObject);
                        }
                        else if (CurrentState is StateMachine.FoundationStageRollDice)
                        {
                            DiceRolled((RecieveMessage)rxObject);
                        }
                        else if (CurrentState is StateMachine.FoundationStageRoundOne
                            || CurrentState is StateMachine.FoundationStageRoundTwo)
                        {
                            FoundationRoundAllSet((RecieveMessage)rxObject);
                        }
                        else if (CurrentState is StateMachine.PlayerRollDice)
                        {
                            DiceRolled((RecieveMessage)rxObject);
                        }
                        else if (CurrentState is StateMachine.PlayerStageBuild)
                        {
                            BuildingsSet((RecieveMessage)rxObject);
                        }
                        else if (CurrentState is StateMachine.PlayerStageDeal)
                        {
                            Dealed((RecieveMessage)rxObject);
                        }
                    }

                    


                }
                else
                {
                    if (rxObject is RecieveMessage)
                    {
                        RecieveMessage message = (RecieveMessage)rxObject;

                        if (new TcpIpProtocol().IsClientDataPattern(message.Data))
                        {
                            byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                            if (new TcpIpProtocol().PLAYER_READY.SequenceEqual(equalBytes))
                            {
                                if (GameHasStarted == false)
                                {
                                    PlayersReady++;

                                    if (PlayersReady >= 3)
                                    {
                                        GameHasStarted = true;

                                        TxQueue.Enqueue(new TransmitMessage(new TcpIpProtocol().SERVER_NEED_PLAYER_NAME));
                                        SetState(new StateMachine.FoundationStage(this));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal bool PlayerCanBuyProgressCard()
        {
            if (CurrentPlayer.ResourceCardsTitan > 0 && CurrentPlayer.ResourceCardsFriendlyAlien > 0 && CurrentPlayer.ResourceCardsBiomass > 0)
                return true;

            return false;
        }
    }
}
