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

namespace SiedlerVonSaffar.GameLogic
{
    public class GameLogic
    {
        private List<GameObjects.Player.Player> Players;
        private short currentPlayerID;
        private DataStruct.Container dataContainer;
        private ThreadStart gameLogicThreadStart;
        private Thread gameLogicThread;
        public Queue<object> RxQueue { get; private set; }

        public GameLogic()
        {
            Players = new List<GameObjects.Player.Player>();
            dataContainer = new DataStruct.Container();
            RxQueue = new Queue<object>();
            gameLogicThreadStart = new ThreadStart(Start);
            gameLogicThread = new Thread(gameLogicThreadStart);
            gameLogicThread.Name = "GameLogic";
            gameLogicThread.Start();

        }

        private void Start()
        {
            SocketStateObject socketStateObject;

            while (true)
            {
                while (RxQueue.Count < 1) ;

                object a = RxQueue.Dequeue();

                /*using (MemoryStream ms = new MemoryStream(socketStateObject.buffer))
                {
                    IFormatter br = new BinaryFormatter();
                    dataContainer = (br.Deserialize(ms) as DataStruct.Container);
                }*/
            }
        }

        private void createGameRules(short currentPlayerID)
        {
            DataStruct.Angle[,] angles = dataContainer.Angles;
            DataStruct.DataStruct[,] edges = dataContainer.Data;

            int arrayDimensionZero = angles.GetLength(0);
            int arrayDimensionOne = angles.GetLength(1);

            int i;
            int j;

            DataStruct.Angle tmpAngle;

            for (i = 0; i < arrayDimensionZero; i++)
            {
                for (j = 0; j < arrayDimensionOne; j++)
                {
                    if (angles[i, j] == null)
                        continue;

                    tmpAngle = angles[i, j];

                    if (tmpAngle.Hexagons.Count == 0)
                        continue;

                    switch (tmpAngle.BuildTyp)
                    {
                        case DataStruct.BuildTypes.NONE:

                            //Gehe alle kanten an den Ecken durch
                            foreach (DataStruct.Edge element in tmpAngle.Edges)
                            {
                                if (element.BuildTyp == DataStruct.BuildTypes.HYPERLOOP)
                                {
                                    GameObjects.Buildings.Hyperloop hyperloop = (GameObjects.Buildings.Hyperloop)tmpAngle.Data;

                                    //Wenn eine Kante eine Eigene Straße beinhaltet darf eine Außenposten gebaut werden
                                    if (hyperloop.PlayerID == currentPlayerID)
                                    {
                                        tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_OUTPOST;
                                        break;
                                    }
                                    else
                                        tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                }
                            }
                            break;
                        case DataStruct.BuildTypes.OUTPOST:
                            GameObjects.Buildings.Outpost outpost = (GameObjects.Buildings.Outpost)tmpAngle.Data;

                            //Wenn der Außenposten dem spieler gehört, darf er eine Stadt Bauen
                            if (outpost.PlayerID == currentPlayerID)
                                tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_CITY;
                            else
                                tmpAngle.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;

                            break;
                    }
                }
            }

            arrayDimensionZero = edges.GetLength(0);
            arrayDimensionOne = edges.GetLength(1);

            DataStruct.Edge tmpEdge;

            for (i = 0; i < arrayDimensionZero; i++)
            {
                for (j = 0; j < arrayDimensionOne; j++)
                {
                    if (edges[i, j] == null)
                        continue;
                    else if (edges[i, j] is DataStruct.Hexagon)
                        continue;

                    tmpEdge = (DataStruct.Edge)edges[i, j];

                    switch (tmpEdge.BuildTyp)
                    {
                        case DataStruct.BuildTypes.NONE:
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

                            //Wenn keine Gebäude an dieser Kante existierem
                            if (buildingUpperAngle == null && buildingLowerAngle == null)
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                continue;
                            }

                            //Wenn an der oberen Ecke ein eigenes Gebäude existiert und an der unteren Kante nicht
                            if ((buildingUpperAngle != null && buildingUpperAngle.PlayerID == currentPlayerID) && buildingLowerAngle == null)
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                continue;
                            }

                            //Wenn an der unteren Ecke ein eigenes Gebäude existiert und an der oberen Kante nicht
                            if (buildingUpperAngle == null && (buildingLowerAngle != null && buildingUpperAngle.PlayerID == currentPlayerID))
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                continue;
                            }

                            //Wenn 2 Gebäude an der Kante existieren aber von unterschiedlichen Spielern
                            if (buildingUpperAngle.PlayerID != buildingLowerAngle.PlayerID)
                            {
                                tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                                continue;
                            }

                            //Wenn 2 Gebäude existieren und beide die eigenen sind
                            if (buildingUpperAngle.PlayerID == buildingLowerAngle.PlayerID)
                            {
                                if (buildingUpperAngle.PlayerID == currentPlayerID)
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CAN_SET_BUILDING_HYPERLOOP;
                                else
                                    tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                            }

                            break;
                        case DataStruct.BuildTypes.HYPERLOOP:
                            tmpEdge.BuildStruct = DataStruct.BuildStructTypes.CANT_SET_BUILDING;
                            break;
                    }
                }
            }
        }
    }
}
