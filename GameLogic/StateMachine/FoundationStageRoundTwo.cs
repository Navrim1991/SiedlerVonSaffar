using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    class FoundationStageRoundTwo : State
    {

        public FoundationStageRoundTwo(GameLogic gameLogic) : base(gameLogic)
        {

        }
        public override void BuildingsSet(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void Dealed(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void DiceRolled(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void FoundationRoundAllSet(RecieveMessage message)
        {
            if (tcpProtocol.IsClientDataPattern(message.Data))
            {
                byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                {
                    gameLogic.DeserializeContainerData(message.Data);

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

                        gameLogic.SerializeContainerData();
                    }
                }
                else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                {
                    if (gameLogic.CurrentPlayer.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
                    {
                        gameLogic.CurrentPlayer = gameLogic.HandelPlayerData(message.Data);

                        if (gameLogic.foundationStageRoundCounter % 2 == 0)
                            gameLogic.nextPlayer();
                    }
                }
            }
        }

        public override void FoundationRoundOne(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void GetName(RecieveMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
