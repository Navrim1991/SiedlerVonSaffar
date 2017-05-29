using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    class PlayerRollDice : State
    {
        public PlayerRollDice(GameLogic gameLogic) : base(gameLogic)
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

                if (tcpProtocol.PLAYER_ROLL_DICE.SequenceEqual(equalBytes))
                {
                    gameLogic.roundCounter++;

                    int number = (int)gameLogic.Deserialize(message.Data);

                    //TODO 7 gewürfelt;
                    if (number != 7)
                    {
                        gameLogic.HandleRollDice(number);

                        gameLogic.ComputeGameRules();

                        gameLogic.SerializeContainerData();
                    }
                    else
                        gameLogic.TxQueue.Enqueue(new TransmitMessage(gameLogic.CurrentPlayer.ClientIP, tcpProtocol.SERVER_SET_BANDIT, TransmitMessage.TransmitTyps.TO_OWN));


                    gameLogic.SetState(new PlayerStageDeal(gameLogic));
                }
                else if (tcpProtocol.PLAYER_BUY_PROGRESS_CARD.SequenceEqual(equalBytes))
                {
                    if (gameLogic.PlayerCanBuyProgressCard() && gameLogic.ProgressCards.Count > 0)
                    {
                        gameLogic.CurrentPlayer.ProgressCards.Add(gameLogic.ProgressCards.Last());

                        gameLogic.CurrentPlayer.ProgressCards.Last().Round = gameLogic.roundCounter;

                        gameLogic.ProgressCards.Remove(gameLogic.ProgressCards.Last());

                        gameLogic.SerializePlayerData(gameLogic.CurrentPlayer);
                    }
                    else
                    {
                        byte[] error = gameLogic.Serialize(tcpProtocol.SERVER_ERROR, "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen");

                        gameLogic.TxQueue.Enqueue(new TransmitMessage(gameLogic.CurrentPlayer.ClientIP, error, TransmitMessage.TransmitTyps.TO_OWN));
                    }
                }
                else if (tcpProtocol.PLAYER_PLAY_PROGRESS_CARD.SequenceEqual(equalBytes))
                {
                    gameLogic.HandleProgressCards(message.Data);

                    int points = gameLogic.CheckVictory(gameLogic.CurrentPlayer);

                    if (points > 10)
                    {
                        //TODO VICTORY
                        //ANDERE PLAYER ZÄHLEN
                    }
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
