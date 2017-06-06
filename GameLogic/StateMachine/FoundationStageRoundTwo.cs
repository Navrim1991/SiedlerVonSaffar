using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    class FoundationStageRoundTwo : State
    {

        public FoundationStageRoundTwo(GameLogic gameLogic) : base(gameLogic)
        {

        }
        public override void BuildingsSet(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }

        public override void Dealed(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }

        public override void DiceRolled(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }

        public override void FoundationRoundAllSet(NetworkMessageClient message)
        {
            if (message.ProtocolType == TcpIpProtocolType.PLAYER_CONTAINER_DATA)
            {
                if (message.Data.Length == 1 && message.Data[0] is DataStruct.Container)
                {
                    gameLogic.SetNewContainerData((DataStruct.Container)message.Data[0]);

                    gameLogic.FoundationStages(ref gameLogic.foundationStageRoundCounter);

                    if (gameLogic.foundationStageRoundCounter > gameLogic.Players.Count * 2 + 2)
                    {
                        List<DataStruct.Hexagon> hexagons = new List<DataStruct.Hexagon>();

                        for (int i = 0; i < gameLogic.containerData.Hexagons.GetLength(0); i++)
                        {
                            for (int j = 0; j < gameLogic.containerData.Hexagons.GetLength(1); j++)
                            {
                                if (gameLogic.containerData.Hexagons[i, j] != null && !gameLogic.containerData.Hexagons[i, j].HasBandit)
                                    hexagons.Add(gameLogic.containerData.Hexagons[i, j]);
                            }
                        }

                        gameLogic.HandleResources(hexagons);

                        gameLogic.SetState(new PlayerRollDice(gameLogic));

                        gameLogic.ComputeGameRules();

                        gameLogic.SendContainerData();
                    }
                }                
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_DATA)
            {
                if (gameLogic.CurrentPlayer.Name  == message.PlayerName)
                {
                    if (message.Data.Length == 1 && message.Data[0] is GameObjects.Player.Player)
                    {
                        gameLogic.CurrentPlayer = (GameObjects.Player.Player)message.Data[0];

                        if (gameLogic.foundationStageRoundCounter % 2 == 0)
                            gameLogic.nextPlayer();
                    }
                }
            }            
        }

        public override void FoundationRoundOne(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }

        public override void GetName(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }
    }
}
