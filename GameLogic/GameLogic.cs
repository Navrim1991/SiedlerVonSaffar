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

namespace SiedlerVonSaffar.GameLogic
{
    public class GameLogic
    {
        private List<GameObjects.Player.Player> Players;
        private GameObjects.Player.Player currentPlayer;
        private DataStruct.Container dataContainer;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;

        private int countResourceCardsBiosmass;
        private int countResourceCardsCarbonFibres;
        private int countResourceCardsDeuterium;
        private int countResourceCardsFriendlyAliens;
        private int countResourceCardsTitan;
        public ConcurrentQueue<object> RxQueue { get; private set; }

        public GameLogic()
        {
            Players = new List<GameObjects.Player.Player>();
            dataContainer = new DataStruct.Container();
            RxQueue = new ConcurrentQueue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            gameLogicThread.Start();
        }

        private void Start()
        {
            object rxObject; 
            while (true)
            {
                while (RxQueue.Count < 1) ;

                RxQueue.TryDequeue(out rxObject);

                Type rxObjectType = rxObject.GetType();
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
                                    if (hyperloop.PlayerID == currentPlayer.PlayerID)
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
                            if (outpost.PlayerID == currentPlayer.PlayerID)
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
                                    if (element.Data != null && element.PositionX != j && element.PositionY != i)
                                    {
                                        GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)element.Data;

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
                                continue;
                            }

                            //Wenn an der unteren Ecke ein eigenes Gebäude existiert
                            if ((buildingLowerAngle != null && buildingUpperAngle.PlayerID == currentPlayer.PlayerID))
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
