﻿using System.ComponentModel;
using WholesomeDungeonCrawler.ProductCache.Entity;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.Helpers
{
    class TargetingHelper
    {
        public static void SwitchTargetAndFight(IWoWUnit unit, CancelEventArgs canceable)
        {
            canceable.Cancel = true;
            ObjectManager.Me.Target = unit.Guid;
            Fight.StartFight(unit.Guid, false);
        }

        public enum TargetPriority
        {
            High,
            Low
        }

        public class SpecialPrio
        {
            public bool WhenAttackingGroup { get; private set; } // Prioritized only if the unit is targeting a party member
            public int WhenInFightWith { get; private set; }
            public TargetPriority TargetPriority { get; private set; }
            public SpecialPrio(int whenInFightWith, TargetPriority targetPriority, bool ifAttackingGroup = false)
            {
                WhenInFightWith = whenInFightWith;
                TargetPriority = targetPriority;
                WhenAttackingGroup = ifAttackingGroup;
            }
        }
    }
}
