using SiedlerVonSaffar.NetworkMessageProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.GameLogic.StateMachine
{
    [Serializable]
    abstract class State
    {
        protected GameLogic gameLogic;
        protected TcpIpProtocol tcpProtocol;
        public State(GameLogic gameLogic)
        {
            this.gameLogic = gameLogic;

            tcpProtocol = new TcpIpProtocol();
        }

        public abstract void GetName(NetworkMessageClient message);
        public abstract void DiceRolled(NetworkMessageClient message);
        public abstract void FoundationRoundAllSet(NetworkMessageClient message);
        public abstract void FoundationRoundOne(NetworkMessageClient message);
        public abstract void BuildingsSet(NetworkMessageClient message);

        public abstract void Dealed(NetworkMessageClient message);
    }
}
