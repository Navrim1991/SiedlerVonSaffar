using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    class FoundationStage : State
    {

        public FoundationStage(GameLogic gameLogic) :base(gameLogic)
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
            throw new NotImplementedException();
        }

        public override void FoundationRoundOne(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void GetName(RecieveMessage message)
        {
            if (tcpProtocol.IsClientDataPattern(message.Data))
            {
                byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                if (tcpProtocol.PLAYER_NAME.SequenceEqual(equalBytes))
                {
                    string playerName = (string)gameLogic.Deserialize(message.Data);
                    //TODO FARBE SETZEN
                    GameObjects.Player.Player newPlayer = new GameObjects.Player.Player(playerName, (short)(gameLogic.Players.Count + 1), message.ClientIP);

                    gameLogic.SetupNewPlayer(newPlayer);

                    if (gameLogic.Players.Count == gameLogic.PlayersReady)
                    {
                        gameLogic.TxQueue.Enqueue(new TransmitMessage(tcpProtocol.SERVER_STAGE_FOUNDATION_ROLL_DICE));
                        gameLogic.SetState(new FoundationStageRollDice(gameLogic));
                    }
                }
            }
        }
    }
}
