﻿using robotManager.FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WholesomeDungeonCrawler.Data;
using WholesomeDungeonCrawler.Dungeonlogic;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.Manager;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.States
{
    class DungeonLogic : State, IState
    {
        private readonly ICache _cache;
        private readonly IEntityCache _entityCache;
        private readonly IProfileManager _profileManager;
        public DungeonLogic(ICache iCache, IEntityCache iEntityCache, IProfileManager profilemanager, int priority)
        {
            _cache = iCache;
            _entityCache = iEntityCache;
            _profileManager = profilemanager;
            Priority = priority;
        }
        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnected
                    || !_entityCache.Me.Valid
                    || Fight.InFight)
                {
                    return false;
                }

                return _profileManager.actualDungeonProfile;
            }
        }

        public override void Run()
        {
            var actualDungeon = _profileManager.actualDungeon;
            //actualDungeon.Profile.Load();   <<< this is where the Dungeonprofile will be loaded
            //_logicRunner.Pulse();
        }
    }
}
