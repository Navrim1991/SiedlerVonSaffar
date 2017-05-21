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
        private Dictionary<GameObjects.Player.Player, Socket> Players;
        public KeyValuePair<GameObjects.Player.Player, Socket> currentPlayer { get; private set; }
        private DataStruct.Container dataContainer;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;
        private TcpIpProtocol tcpProtocol;

        private bool gameHasStarted = false;

        public ConcurrentQueue<object> RxQueue { get; private set; }
        public ConcurrentQueue<object> TxQueue { get; private set; }

        public GameLogic()
        {
            Players = new Dictionary<GameObjects.Player.Player, Socket>();
            dataContainer = new DataStruct.Container();
            RxQueue = new ConcurrentQueue<object>();
            TxQueue = new ConcurrentQueue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            tcpProtocol = new TcpIpProtocol();
            gameLogicThread.Start();
        }

        //private void AddPlayer

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
                            //container Data anpassen
                        }
                        else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                        {
                            //player hinzufügen
                        }
                        else if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                        {
                            //Spieler möchte handeln
                        }
                        else if (tcpProtocol.PLAYER_ROLL_DICE.SequenceEqual(equalBytes))
                        {
                            //spieler hat gewürfelt
                        }
                        else if (tcpProtocol.PLAYER_PLAY_PROGRESSCARD.SequenceEqual(equalBytes))
                        {
                            //spieler spielt prograss card
                        }
                        else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                        {
                            //spieler ist fertig
                        }
                    }
                }
            }
        }
        private bool CanSetStructHyperloop()
        {
            if (currentPlayer.Key.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.Key.ResourceCardsDeuterium.Count > 0)
                return true;

            return false;
        }

        private bool CanSetStructOutpost()
        {
            if (currentPlayer.Key.ResourceCardsCarbonFibres.Count > 0 && currentPlayer.Key.ResourceCardsDeuterium.Count > 0
                && currentPlayer.Key.ResourceCardsFriendlyAlien.Count > 0 && currentPlayer.Key.ResourceCardsBiomass.Count > 0)
                return true;

            return false;

        }

        private bool CanSetStructCity()
        {
            if (currentPlayer.Key.ResourceCardsTitan.Count >= 3 && currentPlayer.Key.ResourceCardsBiomass.Count > 2)
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
                                    if (hyperloop.PlayerID == currentPlayer.Key.PlayerID)
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
                            if (outpost.PlayerID == currentPlayer.Key.PlayerID)
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

                                        if (hyperloop.PlayerID == currentPlayer.Key.PlayerID)
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

                                        if (hyperloop.PlayerID == currentPlayer.Key.PlayerID)
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
                            if ((buildingUpperAngle != null && buildingUpperAngle.PlayerID == currentPlayer.Key.PlayerID))
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                continue;
                            }

                            //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                            if ((buildingLowerAngle != null && buildingUpperAngle.PlayerID == currentPlayer.Key.PlayerID))
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

        private void ComputeGameRules(short currentPlayerID)
        {
            if (!CanSetStructCity() && !CanSetStructOutpost())
                checkAngles();

            if (CanSetStructHyperloop())
                checkEdges();
        }
    }
}
