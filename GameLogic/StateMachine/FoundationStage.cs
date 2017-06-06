using SiedlerVonSaffar.NetworkMessageProtocol;
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
            throw new NotImplementedException();
        }

        public override void FoundationRoundOne(NetworkMessageClient message)
        {
            throw new NotImplementedException();
        }

        public override void GetName(NetworkMessageClient message)
        {
            if(message.ProtocolType == TcpIpProtocolType.PLAYER_NAME)
            {
                if(message.Data.Length == 1  && message.Data[0] is string)
                {
                    string playerName = (string)message.Data[0];
                    //TODO FARBE SETZEN
                    GameObjects.Player.Player newPlayer = new GameObjects.Player.Player(playerName, (short)(gameLogic.Players.Count + 1));

                    gameLogic.SetupNewPlayer(newPlayer);

                    if (gameLogic.Players.Count == gameLogic.PlayersReady)
                    {
                        gameLogic.TxQueue.Enqueue(new NetworkMessageServer("", TcpIpProtocolType.SERVER_STAGE_FOUNDATION_ROLL_DICE, null));
                        gameLogic.SetState(new FoundationStageRollDice(gameLogic));
                    }
                }
            }
        }
    }
}
