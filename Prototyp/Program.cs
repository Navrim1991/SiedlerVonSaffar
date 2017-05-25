using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SiedlerVonSaffar.Prototyp
{
    class Program
    {
        static void Main(string[] args)
        {
            Prototyp prototyp = new Prototyp();

            Thread.Sleep(50);

            prototyp.Start();

        }
    }
}
