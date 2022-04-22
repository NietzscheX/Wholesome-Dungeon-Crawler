﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.Class;

namespace WholesomeDungeonCrawler.Data
{
    internal sealed class CachedAura : IAura
    {
        public int Stacks { get; }
        public int TimeLeft { get; }

        public CachedAura(Aura aura)
        {
            Stacks = aura.Stack;
            TimeLeft = aura.TimeLeft;
        }
    }

}
