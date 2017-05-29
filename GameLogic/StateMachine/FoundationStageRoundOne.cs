using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    class FoundationStageRoundOne : State
    {
        public FoundationStageRoundOne(GameLogic gameLogic) : base(gameLogic)
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
                        gameLogic.SetState(new FoundationStageRoundTwo(gameLogic));

                        gameLogic.roundCounter++;

                        gameLogic.foundationStageRoundCounter = 1;
                    }
                }
                else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                {
                    GameObjects.Player.Player tmp = gameLogic.HandelPlayerData(message.Data);

                    if (gameLogic.CurrentPlayer.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString())
                    {
                        gameLogic.CurrentPlayer = tmp;

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
