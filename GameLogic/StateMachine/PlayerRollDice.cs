using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    class PlayerRollDice : State
    {
        public PlayerRollDice(GameLogic gameLogic) : base(gameLogic)
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
            if (message.ProtocolType == TcpIpProtocolType.PLAYER_ROLL_DICE)
            {
                gameLogic.roundCounter++;

                if (message.Data.Length == 1 && message.Data[0] is int)
                {
                    int number = (int)message.Data[0];

                    //TODO 7 gewürfelt;
                    if (number != 7)
                    {
                        gameLogic.HandleRollDice(number);

                        gameLogic.ComputeGameRules();

                        gameLogic.SendContainerData();

                        gameLogic.SetState(new PlayerStageDeal(gameLogic));
                    }
                    else
                    {
                        gameLogic.TxQueue.Enqueue(new NetworkMessageServer(gameLogic.CurrentPlayer.Name, TcpIpProtocolType.SERVER_SET_BANDIT, new object[] { "Du musst den Banditen versetzen" }, MessageTyps.TO_OWN));
                    }                      
                }
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_SET_BANDIT)
            {
                if (message.Data.Length == 1 && message.Data[0] is DataStruct.Container)
                {
                    gameLogic.SetNewContainerData((DataStruct.Container)message.Data[0]);

                    gameLogic.HandleBandit();
                }
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_BUY_PROGRESS_CARD)
            {
                if (gameLogic.PlayerCanBuyProgressCard() && gameLogic.ProgressCards.Count > 0)
                {
                    gameLogic.CurrentPlayer.ProgressCards.Add(gameLogic.ProgressCards.Last());

                    gameLogic.CurrentPlayer.ProgressCards.Last().Round = gameLogic.roundCounter;

                    gameLogic.ProgressCards.Remove(gameLogic.ProgressCards.Last());

                    gameLogic.SendPlayerData(gameLogic.CurrentPlayer);
                }
                else
                {
                    gameLogic.TxQueue.Enqueue(new NetworkMessageServer(gameLogic.CurrentPlayer.Name, TcpIpProtocolType.SERVER_ERROR, new object[] { "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen" }, MessageTyps.TO_OWN));
                }
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_PLAY_PROGRESS_CARD)
            {
                if (message.Data.Length == 1 && message.Data[0] is GameObjects.Menu.Cards.Progress.ProgressCard)
                {
                    gameLogic.HandleProgressCards((GameObjects.Menu.Cards.Progress.ProgressCard)message.Data[0]);

                    int points = gameLogic.CheckVictory(gameLogic.CurrentPlayer);

                    if (points > 10)
                    {
                        //TODO VICTORY
                        //ANDERE PLAYER ZÄHLEN
                    }
                }
            }

            /*if (tcpProtocol.IsClientDataPattern(message.Data))
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

                        gameLogic.SendContainerData();
                    }
                    else
                        gameLogic.TxQueue.Enqueue(new TransmitMessage(tcpProtocol.SERVER_SET_BANDIT, TransmitMessage.TransmitTyps.TO_OWN));


                    gameLogic.SetState(new PlayerStageDeal(gameLogic));
                }
                else if (tcpProtocol.PLAYER_BUY_PROGRESS_CARD.SequenceEqual(equalBytes))
                {
                    if (gameLogic.PlayerCanBuyProgressCard() && gameLogic.ProgressCards.Count > 0)
                    {
                        gameLogic.CurrentPlayer.ProgressCards.Add(gameLogic.ProgressCards.Last());

                        gameLogic.CurrentPlayer.ProgressCards.Last().Round = gameLogic.roundCounter;

                        gameLogic.ProgressCards.Remove(gameLogic.ProgressCards.Last());

                        gameLogic.SendPlayerData(gameLogic.CurrentPlayer);
                    }
                    else
                    {
                        byte[] error = gameLogic.Serialize(tcpProtocol.SERVER_ERROR, "Du hast zu weniger Ressourcen um eine Entwicklungskarte zu kaufen");

                        gameLogic.TxQueue.Enqueue(new TransmitMessage(error, TransmitMessage.TransmitTyps.TO_OWN));
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

            }*/
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
