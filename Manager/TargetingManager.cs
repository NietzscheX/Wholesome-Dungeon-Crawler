﻿using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WholesomeDungeonCrawler.CrawlerSettings;
using WholesomeDungeonCrawler.Data;
using WholesomeDungeonCrawler.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.Manager
{
    class TargetingManager : ITargetingManager
    {

        private readonly ICache _cache;
        private readonly IEntityCache _entityCache;
        private IWoWUnit Target;

        public TargetingManager(IEntityCache entityCache, ICache cache)
        {
            _entityCache = entityCache;
            _cache = cache;
        }

        public void Targetswitcher(WoWUnit target, CancelEventArgs cancable)
        {
            if (_entityCache.Target.Dead)
            {
                Interact.ClearTarget();
            }

            //Tank Section Start
            if(_cache.IAmTank)
            {
                IWoWUnit attackerGroupMember = AttackingGroupMember();
                if (attackerGroupMember != null 
                    && attackerGroupMember.TargetGuid != _entityCache.Me.Guid
                    && attackerGroupMember.TargetGuid != _entityCache.Target.Guid)
                {
                    Target = attackerGroupMember;
                    Logger.Log($"Attacking: {Target.Name} is attacking Groupmember, switching");
                    SwitchedTargetFight(Target);
                }
            }

            //Tank Section End
            //Slave Section Start
            if (!_cache.IAmTank)
            {
                IWoWUnit fleeUnit = FleeingUnit(_entityCache.TankUnit);
                if (fleeUnit != null && _entityCache.Me.TargetGuid != fleeUnit.Guid)
                {
                    Target = FleeingUnit(_entityCache.TankUnit);
                    Logger.Log($"Attacking: {Target.Name} is attacking Fleeing, switching");
                    SwitchedTargetFight(Target);
                }
                //Check to AssistTank
                if (AssistTank(_entityCache.TankUnit) != null 
                    && _entityCache.Me.TargetGuid == 0 
                    && AssistTank(_entityCache.TankUnit).Guid != _entityCache.Target.Guid)
                {
                    Target = AssistTank(_entityCache.TankUnit);
                    Logger.Log($"Attacking: {Target.Name} is attacking Tank, switching");
                    SwitchedTargetFight(Target);
                }

                //check to Assist any  Groupmember if Tank don´t get the aggro
                if (AssistGroup(_entityCache.TankUnit) != null 
                    && _entityCache.Me.TargetGuid == 0
                    && AssistGroup(_entityCache.TankUnit).Guid != _entityCache.Target.Guid)
                {
                    Target = AssistTank(_entityCache.TankUnit);
                    Logger.Log($"Attacking: {Target.Name} is attacking Groupmember, switching");
                    SwitchedTargetFight(Target);
                }
            }
            //Slave Section End
        }

        private void SwitchedTargetFight(IWoWUnit target)
        {
            MovementManager.StopMove();
            Fight.StopFight();
            Fight.StartFight(target.Guid, false);
        }

        private IWoWUnit AttackingGroupMember()
        {
            IWoWUnit Unit = FindClosestUnit(unit =>
            unit.IsAttackingGroup
            && !unit.IsAttackingMe
            && _entityCache.Me.PositionWithoutType.DistanceTo(unit.PositionWithoutType) <= 60
            && !unit.Dead, PointInMidOfGroup());
            return Unit;
        }

        private IWoWUnit FleeingUnit(IWoWUnit Tank)
        {
            IWoWUnit Unit = FindClosestUnit(unit =>
            unit.IsAttackingGroup
            && unit.Fleeing
            && _entityCache.Me.PositionWithoutType.DistanceTo(unit.PositionWithoutType) <= 60
            && !unit.Dead, Tank.PositionWithoutType);
            return Unit;
        }

        private IWoWUnit AssistTank(IWoWUnit Tank)
        {
            IWoWUnit Unit = FindClosestUnit(unit =>
            unit.IsAttackingGroup
            && unit.TargetGuid == Tank.Guid
            && _entityCache.Me.PositionWithoutType.DistanceTo(unit.PositionWithoutType) <= 60
            && !unit.Dead, Tank.PositionWithoutType);
            return Unit;
        }

        private IWoWUnit AssistGroup(IWoWUnit Tank)
        {
            IWoWUnit Unit = FindClosestUnit(unit =>
            unit.IsAttackingGroup
            && unit.TargetGuid != Tank.Guid
            && _entityCache.Me.PositionWithoutType.DistanceTo(unit.PositionWithoutType) <= 60
            && !unit.Dead, Tank.PositionWithoutType);
            return Unit;
        }

        private IWoWUnit FindClosestUnit(Func<IWoWUnit, bool> predicate, Vector3 referencePosition = null)
        {
            IWoWUnit foundUnit = null;
            var distanceToUnit = float.MaxValue;

            Vector3 position = referencePosition != null ? referencePosition : _entityCache.Me.PositionWithoutType;

            foreach (IWoWUnit unit in _entityCache.EnemyUnitsList)
            {
                if (!predicate(unit)) continue;

                if (foundUnit == null)
                {
                    distanceToUnit = position.DistanceTo(unit.PositionWithoutType);
                    foundUnit = unit;
                }
                else
                {
                    float currentDistanceToUnit = WTPathFinder.CalculatePathTotalDistance(position, unit.PositionWithoutType);
                    if (currentDistanceToUnit < distanceToUnit)
                    {
                        foundUnit = unit;
                        distanceToUnit = currentDistanceToUnit;
                    }
                }
            }
            return foundUnit;
        }

        private Vector3 PointInMidOfGroup()
        {
            float xvec = 0;
            float yvec = 0;
            float zvec = 0;

            int counter = 0;
            foreach (IWoWUnit player in _entityCache.ListGroupMember)
            {
                xvec = xvec + player.PositionWithoutType.X;
                yvec = yvec + player.PositionWithoutType.Y;
                zvec = zvec + player.PositionWithoutType.Z;

                counter++;
            }

            return new Vector3(xvec / counter, yvec / counter, zvec / counter);
        }

        public void Initialize()
        {
            wManager.Events.FightEvents.OnFightLoop += Targetswitcher;
        }

        public void Dispose()
        {
            wManager.Events.FightEvents.OnFightLoop -= Targetswitcher;
        }
    }
}
