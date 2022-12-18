﻿using robotManager.FiniteStateMachine;
using System.Linq;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.ProductCache;
using WholesomeDungeonCrawler.ProductCache.Entity;
using wManager.Wow.Helpers;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeDungeonCrawler.States
{
    class GroupQueueAccept : State, IState
    {
        public override string DisplayName => "GroupQueue Accept";
        private readonly ICache _cache;
        private readonly IEntityCache _entityCache;
        private Timer timer = new Timer();

        public GroupQueueAccept(ICache iCache, IEntityCache cache)
        {
            _cache = iCache;
            _entityCache = cache;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!timer.IsReady
                    || !Conditions.InGameAndConnected
                    || !_entityCache.Me.Valid
                    || Fight.InFight
                    || _cache.IsInInstance
                    || _entityCache.ListPartyMemberNames.Count() < 4)
                {
                    return false;
                }

                timer = new Timer(1000);

                return _cache.LFGRoleCheckShown;
            }
        }

        public override void Run()
        {
            Logger.Log("Accepting role queue check");
            Lua.LuaDoString("LFDRoleCheckPopupAcceptButton:Click()");
        }
    }
}
