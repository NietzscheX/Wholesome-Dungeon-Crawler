﻿using System.Collections.Generic;

namespace WholesomeDungeonCrawler.ProductCache.Entity
{
    public interface IEntityCache : ICycleable
    {
        IWoWUnit Target { get; }
        IWoWUnit Pet { get; }
        IWoWLocalPlayer Me { get; }
        IWoWPlayer TankUnit { get; }
        IWoWUnit[] EnemyUnitsTargetingGroup { get; }
        IWoWUnit[] EnemyUnitsLootable { get; }
        IWoWUnit[] EnemyAttackingGroup { get; }
        IWoWUnit[] EnemyUnitsList { get; }
        IWoWPlayer[] ListGroupMember { get; }
        List<string> ListPartyMemberNames { get; }

        bool IAmTank { get; }
    }
}