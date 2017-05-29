using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    class PlayerStageDeal : State
    {

        public PlayerStageDeal(GameLogic gameLogic): base(gameLogic)
        {

        }
        public override void BuildingsSet(RecieveMessage message)
        {
            throw new NotImplementedException();
        }

        public override void Dealed(RecieveMessage message)
        {
            if (tcpProtocol.IsClientDataPattern(message.Data))
            {
                byte[] equalBytes = { message.Data[0], message.Data[1], message.Data[2], message.Data[3] };

                if (tcpProtocol.PLAYER_DEAL.SequenceEqual(equalBytes))
                {
                    gameLogic.HandleDeal(message.Data);
                }
                else if (tcpProtocol.PLAYER_DATA.SequenceEqual(equalBytes))
                {
                    GameObjects.Player.Player tmp = gameLogic.HandelPlayerData(message.Data);

                    if (gameLogic.CurrentPlayer.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString())
                        gameLogic.CurrentPlayer = tmp;
                    else
                    {
                        GameObjects.Player.Player tmp2 = (from p in gameLogic.Players where p.ClientIP.Address.ToString() == tmp.ClientIP.Address.ToString() select p).FirstOrDefault();

                        if (tmp2 != null)
                            tmp2 = tmp;
                    }
                }
                else if (tcpProtocol.PLAYER_CONTAINER_DATA.SequenceEqual(equalBytes))
                {
                    gameLogic.DeserializeContainerData(message.Data);

                    if (gameLogic.playerPlayedProgressCardSteet)
                    {
                        gameLogic.CheckEdges();

                        gameLogic.playerPlayedProgressCardSteet = false;

                        gameLogic.SerializeContainerData();
                    }
                    else
                    {
                        int points = gameLogic.CheckVictory(gameLogic.CurrentPlayer);

                        if (points >= 10)
                        {
                            //TODO VICTORY
                            //ANDERE PLAYER ZÄHLEN
                        }
                        else
                        {
                            gameLogic.ComputeGameRules();

                            gameLogic.SerializeContainerData();

                            gameLogic.SetState(new PlayerStageBuild(gameLogic));
                        }
                    }
                }
                else if (tcpProtocol.PLAYER_READY.SequenceEqual(equalBytes))
                {
                    gameLogic.ComputeGameRules();

                    gameLogic.SerializeContainerData();

                    gameLogic.SetState(new PlayerStageBuild(gameLogic));
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
                else if (tcpProtocol.PLAYER_SET_BANDIT.SequenceEqual(equalBytes))
                {
                    gameLogic.DeserializeContainerData(message.Data);

                    gameLogic.HandleBandit();
                }
            }
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
            throw new NotImplementedException();
        }
    }
}
