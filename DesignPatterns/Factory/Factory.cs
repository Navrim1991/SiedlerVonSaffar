﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiedlerVonSaffar.DesignPatterns.Factory
{
    public abstract class Factory
    {
        public abstract Product FactoryMethod { get; }

    }
}