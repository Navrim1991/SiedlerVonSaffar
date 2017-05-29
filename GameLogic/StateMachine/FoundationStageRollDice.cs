using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
   

    class FoundationStageRollDice : State
    {
        int diceNumber = 0;
        int diceRolled = 0;

        public FoundationStageRollDice(GameLogic gameLogic) : base(gameLogic)
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

            if (tcpProtocol.IsClientDataPattern(message.Data))
            {
                byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                if (tcpProtocol.PLAYER_STAGE_FOUNDATION_ROLL_DICE.SequenceEqual(equalBytes))
                {
                    int number = (int)gameLogic.Deserialize(message.Data);

                    diceRolled++;

                    if (number > diceNumber)
                    {
                        diceNumber = number;

                        for (int i = 0; i < gameLogic.Players.Count; i++)
                        {
                            GameObjects.Player.Player tmp = gameLogic.Players.Dequeue();

                            if (tmp.ClientIP.Address.ToString() == message.ClientIP.Address.ToString())
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
                }

                if (diceRolled == gameLogic.PlayersReady)
                {
                    diceRolled = 0;
                    diceNumber = 0;

                    gameLogic.checkAngles();

                    gameLogic.SerializeContainerData();

                    gameLogic.foundationStageRoundCounter++;

                    gameLogic.SetState(new FoundationStageRoundOne(gameLogic));
                }
            }
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
            throw new NotImplementedException();
        }
    }
}
