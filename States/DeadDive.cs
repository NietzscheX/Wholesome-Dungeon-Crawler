﻿using robotManager.FiniteStateMachine;
using System.Threading;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.ProductCache.Entity;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.States
{
    class DeadDive : State, IState
    {
        public override string DisplayName => "DeadDive";

        private readonly IEntityCache _entityCache;

        public DeadDive(IEntityCache iEntityCache)
        {
            _entityCache = iEntityCache;
        }

        public override bool NeedToRun
        {
            get
            {
                if (_entityCache.Me.Swimming
                    || !_entityCache.Me.IsDead
                    || !MovementManager.InMovement)
                {
                    return false;
                }

                return _entityCache.Me.Auras.ContainsKey(8326) 
                    && MovementManager.CurrentPath.Find(x => x == MovementManager.CurrentMoveTo) != null 
                    && MovementManager.CurrentPath.Find(x => x == MovementManager.CurrentMoveTo).Type == "Swimming";
            }
        }

        public override void Run()
        {
            Logger.Log("Diving!");
            MovementManager.StopMove();
            MovementManager.GoUnderWater();
            Thread.Sleep(1000);
        }
    }
}
