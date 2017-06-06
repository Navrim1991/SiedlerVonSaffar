using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    class PlayerStageBuild : State
    {

        public PlayerStageBuild(GameLogic gameLogic): base(gameLogic)
        {

        }
        public override void BuildingsSet(NetworkMessageClient message)
        {
            if (message.ProtocolType == TcpIpProtocolType.PLAYER_CONTAINER_DATA)
            {
                if (message.Data.Length == 1 && message.Data[0] is DataStruct.Container)
                {
                    gameLogic.SetNewContainerData((DataStruct.Container)message.Data[0]);

                    if (gameLogic.playerPlayedProgressCardSteet)
                    {
                        gameLogic.CheckEdges();

                        gameLogic.playerPlayedProgressCardSteet = false;

                        gameLogic.SendContainerData();
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

                            gameLogic.SendContainerData();

                            gameLogic.SetState(new PlayerStageBuild(gameLogic));
                        }
                    }
                }
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_DEAL)
            {
                if (message.Data.Length == 1 && message.Data[0] is GameObjects.Player.Player)
                {
                    GameObjects.Player.Player tmp = (GameObjects.Player.Player)message.Data[0];

                    if (gameLogic.CurrentPlayer.Name == tmp.Name)
                    {
                        gameLogic.CurrentPlayer = tmp;
                    }
                    else
                    {
                        GameObjects.Player.Player tmp2 = (from p in gameLogic.Players where p.Name == tmp.Name select p).FirstOrDefault();

                        if (tmp2 != null)
                            tmp2 = tmp;
                    }
                }
            }
            else if (message.ProtocolType == TcpIpProtocolType.PLAYER_READY)
            {
                gameLogic.nextPlayer();

                gameLogic.ComputeGameRules();

                gameLogic.SendContainerData();

                gameLogic.SetState(new PlayerRollDice(gameLogic));
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
            throw new NotImplementedException();
        }
    }
}
