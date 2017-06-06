using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{

    [Serializable]
    class FoundationStageRollDice : State
    {
        int diceNumber = 0;
        int diceRolled = 0;

        public FoundationStageRollDice(GameLogic gameLogic) : base(gameLogic)
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
            if (message.ProtocolType == TcpIpProtocolType.PLAYER_STAGE_FOUNDATION_ROLL_DICE)
            {
                if (message.Data.Length == 1 && message.Data[0] is int)
                {
                    int number = (int)message.Data[0];

                    diceRolled++;

                    if (number > diceNumber)
                    {
                        diceNumber = number;

                        for (int i = 0; i < gameLogic.Players.Count; i++)
                        {
                            GameObjects.Player.Player tmp = gameLogic.Players.Dequeue();

                            if(tmp.Name == message.PlayerName)
                            {
                                if (gameLogic.CurrentPlayer != null)
                                    gameLogic.Players.Enqueue(gameLogic.CurrentPlayer);

                                gameLogic.CurrentPlayer = tmp;

                                break;
                            }
                            else
                            {
                                gameLogic.Players.Enqueue(tmp);
                            }
                        }
                    }

                    if (diceRolled == gameLogic.PlayersReady)
                    {
                        diceRolled = 0;
                        diceNumber = 0;

                        gameLogic.checkAngles();

                        gameLogic.SendContainerData();

                        gameLogic.foundationStageRoundCounter++;

                        gameLogic.SetState(new FoundationStageRoundOne(gameLogic));
                    }
                }
            }
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
            throw new NotImplementedException();
        }
    }
}
