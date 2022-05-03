﻿using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WholesomeDungeonCrawler.Data;
using WholesomeDungeonCrawler.Dungeonlogic;
using WholesomeToolbox;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.States.ProfileStates
{
    class SMoveAlongPath : State
    {
        private readonly ICache _cache;
        private readonly IEntityCache _entityCache;
        private readonly IProfile _profile;
        public SMoveAlongPath(ICache iCache, IEntityCache iEntityCache, int priority)
        {
            _cache = iCache;
            _entityCache = iEntityCache;
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

                return _profile.CurrentStepType.Contains("MoveAlongPath");
            }
        }

        public override void Run()
        {

            List<Vector3> Path = _profile.CurrentStep.Path;

            if (_entityCache.Me.PositionWithoutType.DistanceTo(_profile.CurrentStep.Path.Last()) < 5f)
            {
                _profile.CurrentStep.IsCompleted = true;
            }

            MovementManager.Go(WTPathFinder.PathFromClosestPoint(Path));
            /*
            if (!_movehelper.IsMovementThreadRunning || _movehelper.CurrentTarget.DistanceTo(_target) > 2)
            {
                //_movehelper.StartMoveAlongThread(PathFromClosestPoint(_path));
            }
            */
            _profile.CurrentStep.IsCompleted = false;
        }
    }
}