using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    abstract class State
    {
        protected GameLogic gameLogic;
        protected TcpIpProtocol tcpProtocol;
        public State(GameLogic gameLogic)
        {
            this.gameLogic = gameLogic;

            tcpProtocol = new TcpIpProtocol();
        }

        public abstract void GetName(RecieveMessage message);
        public abstract void DiceRolled(RecieveMessage message);
        public abstract void FoundationRoundAllSet(RecieveMessage message);
        public abstract void FoundationRoundOne(RecieveMessage message);
        public abstract void BuildingsSet(RecieveMessage message);

        public abstract void Dealed(RecieveMessage message);
    }
}
