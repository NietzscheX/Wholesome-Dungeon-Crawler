﻿using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.Managers.AvoidAOEHelpers;
using WholesomeDungeonCrawler.Managers.ManagedEvents;
using WholesomeDungeonCrawler.ProductCache;
using WholesomeDungeonCrawler.ProductCache.Entity;
using WholesomeToolbox;
using wManager.Events;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.Managers
{
    internal class AvoidAOEManager : IAvoidAOEManager
    {
        private readonly IEntityCache _entityCache;
        private readonly ICache _cache;
        private List<DangerZone> _dangerZones = new List<DangerZone>();
        private LFGRoles _myRole = CrawlerSettings.WholesomeDungeonCrawlerSettings.CurrentSetting.LFGRole;

        private Dictionary<int, KnownAOE> _knowAOEsDic = new Dictionary<int, KnownAOE>();

        private readonly SortedSet<int> _relevantBuffEnemyIds = new SortedSet<int>();
        private readonly Dictionary<int, DangerSpell> _enemiesSpellsById = new Dictionary<int, DangerSpell>();
        private readonly Lookup<int, DangerBuff> _enemiesBuffsByUnit;
        private Dictionary<int, ForcedSafeZone> _forcedSafeZonesDic = new Dictionary<int, ForcedSafeZone>();

        private bool IAmInDangerZone => _dangerZones.Any(dangerZone => dangerZone.PositionInDangerZone(_entityCache.Me.PositionWT));
        public RepositionInfo RepositionInfo { get; private set; }

        public AvoidAOEManager(IEntityCache entityCache, ICache cache)
        {
            _entityCache = entityCache;
            _cache = cache;

            // TODO: playerDebuffs

            // We load the buffs in a Lookup to speed up lookup, multiple keys can be identical
            _enemiesBuffsByUnit = (Lookup<int, DangerBuff>)DangerList.GetEnemyBuffs
                .Where(eb => eb.AffectedRoles.Contains(_myRole))
                .ToLookup(eb => eb.UnitId, eb => eb);

            // We load the AOEs in a dictionary to speed up lookup
            foreach (KnownAOE knownAOE in DangerList.GetKnownAOEs)
            {
                if (!knownAOE.AffectedRoles.Contains(_myRole)) continue;

                if (!_knowAOEsDic.ContainsKey(knownAOE.Id))
                {
                    _knowAOEsDic.Add(knownAOE.Id, knownAOE);
                }
                else
                {
                    Logger.LogError($"Multiple entries for AOE : {knownAOE.Id}");
                }
            }

            // We load the Spells in a dictionary to speed up lookup
            foreach (DangerSpell dangerSpell in DangerList.GetEnemySpells)
            {
                if (!dangerSpell.AffectedRoles.Contains(_myRole)) continue;

                if (!_enemiesSpellsById.ContainsKey(dangerSpell.SpellId))
                {
                    _enemiesSpellsById.Add(dangerSpell.SpellId, dangerSpell);
                }
                else
                {
                    Logger.LogError($"Multiple entries for Spell : {dangerSpell.SpellId}");
                }
            }

            // We load the Forced Safe Zones in a dictionary to speed up lookup
            foreach (ForcedSafeZone forcedSafeZone in DangerList.GetForcedSafeZones)
            {
                if (!_forcedSafeZonesDic.ContainsKey(forcedSafeZone.BossId))
                {
                    _forcedSafeZonesDic.Add(forcedSafeZone.BossId, forcedSafeZone);
                }
                else
                {
                    Logger.LogError($"Multiple entries for Forced Safe Zone : {forcedSafeZone.BossId}");
                }
            }

            foreach (DangerBuff eb in DangerList.GetEnemyBuffs)
            {
                _relevantBuffEnemyIds.Add(eb.UnitId);
            }
        }

        public void Initialize()
        {
            ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
            FightEvents.OnFightStart += FightEventHandler;
            FightEvents.OnFightLoop += FightEventHandler;
            MovementEvents.OnMovementPulse += MovementEventsOnMovementPulse;
            MovementEvents.OnMoveToPulse += MovementsEventsOnMoveToPulse;
            if (!Radar3D.IsLaunched) Radar3D.Pulse();
            Radar3D.OnDrawEvent += DrawEventAOE;
        }

        public void Dispose()
        {
            ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
            FightEvents.OnFightStart -= FightEventHandler;
            FightEvents.OnFightLoop -= FightEventHandler;
            MovementEvents.OnMovementPulse -= MovementEventsOnMovementPulse;
            MovementEvents.OnMoveToPulse -= MovementsEventsOnMoveToPulse;
            Radar3D.OnDrawEvent -= DrawEventAOE;
            Radar3D.Stop();
        }

        public bool CheckSpells(List<string> args)
        {
            if (int.TryParse(args[8], out int spellId)
                && _enemiesSpellsById.ContainsKey(spellId))
            {
                ulong unitGuid = (ulong)Convert.ToInt64(args[2], 16);
                DangerSpell enemySpell = _enemiesSpellsById[spellId];
                IWoWUnit enemy = _entityCache.EnemyUnitsList.FirstOrDefault(e => e.Guid == unitGuid);
                if (enemy != null)
                {
                    AddSpellDangerZone(enemy, enemySpell);
                    CalculateReposition();
                    return true;
                }
            }
            return false;
        }

        private void AddObjectDangerZone(WoWObject wowObject, float radius)
        {
            if (_dangerZones.Any(dangerZone => dangerZone.Guid == wowObject.Guid && dangerZone.Type == DangerType.GameObject && dangerZone.Position.DistanceTo(wowObject.Position) < 1f))
            {
                return;
            }
            RemoveAllObjectDangerZones(wowObject.Guid);
            DangerObject dangerObject = new DangerObject(wowObject, radius);
            _dangerZones.Add(new DangerZone(dangerObject));
        }

        private void AddSpellDangerZone(IWoWUnit unit, DangerSpell spell)
        {
            if (_dangerZones.Any(dangerZone => dangerZone.Guid == unit.Guid && dangerZone.Danger.Equals(spell)))
            {
                return;
            }
            _dangerZones.Add(new DangerZone(unit, spell));
        }

        private void AddBuffDangerZone(IWoWUnit unit, DangerBuff buff, float duration)
        {
            if (_dangerZones.Any(dangerZone => dangerZone.Guid == unit.Guid && dangerZone.Danger.Equals(buff)))
            {
                return;
            }
            _dangerZones.Add(new DangerZone(unit, buff, duration));
        }

        private void RemoveAllObjectDangerZones(ulong objectGuid)
        {
            _dangerZones.RemoveAll(dz => dz.Guid == objectGuid && dz.Type == DangerType.GameObject);
        }

        private void RemoveAllSpellDangerZones(ulong objectGuid)
        {
            _dangerZones.RemoveAll(dz => dz.Guid == objectGuid && dz.Type == DangerType.Spell);
        }

        private void RemoveAllBuffDangerZones(ulong objectGuid)
        {
            _dangerZones.RemoveAll(dz => dz.Guid == objectGuid && dz.Type == DangerType.Buff);
        }

        private void OnObjectManagerPulse()
        {
            Stopwatch watch = Stopwatch.StartNew();

            List<WoWObject> objectList = ObjectManager.ObjectList;

            //Logger.Log($"OM has pulsed! Currently : {_dangerZones.Count} Zones under management.");

            // Clear danger zone if its corresponding object doesn't exist anymore in the OM
            List<ulong> dangerZonesToRemove = _dangerZones
                .Where(dangerZone => dangerZone.Type == DangerType.GameObject && !objectList.Exists(wObject => wObject.Guid == dangerZone.Guid))
                .Select(dangerZone => dangerZone.Guid)
                .ToList();
            foreach (ulong dzToRemoveGuid in dangerZonesToRemove)
            {
                RemoveAllObjectDangerZones(dzToRemoveGuid);
            }

            // Clear buff zones if their corresponding timers have expired
            List<ulong> expiredDangerZones = _dangerZones
                .Where(dangerZone => dangerZone.Type == DangerType.Buff && dangerZone.Timer != null && dangerZone.Timer.IsReady)
                .Select(dangerZone => dangerZone.Guid)
                .ToList();
            foreach (ulong dzToRemoveGuid in expiredDangerZones)
            {
                RemoveAllBuffDangerZones(dzToRemoveGuid);
            }

            // Clear Spell zones if their corresponding timers have expired
            List<ulong> expiredDangerZoneSpells = _dangerZones
                .Where(dangerZone => dangerZone.Type == DangerType.Spell && dangerZone.Timer != null && dangerZone.Timer.IsReady)
                .Select(dangerZone => dangerZone.Guid)
                .ToList();
            foreach (ulong dzToRemoveGuid in expiredDangerZoneSpells)
            {
                RemoveAllSpellDangerZones(dzToRemoveGuid);
            }

            foreach (IWoWUnit unit in _entityCache.EnemyUnitsList)
            {
                if (_relevantBuffEnemyIds.Contains(unit.Entry))
                {
                    foreach (DangerBuff spell in _enemiesBuffsByUnit[unit.Entry])
                    {
                        if (unit.WowUnit.HaveBuff(spell.Name))
                        {
                            Logger.Log($"Creating buff danger: {spell.Name} for {unit.WowUnit.BuffTimeLeft(spell.Name)}s.");
                            AddBuffDangerZone(unit, spell, unit.WowUnit.BuffTimeLeft(spell.Name));
                        }
                    }
                }
            }

            // Record danger zones
            foreach (WoWObject wowObject in objectList)
            {
                if (_knowAOEsDic.TryGetValue(wowObject.Entry, out KnownAOE knownAOE))
                {
                    switch (wowObject.Type)
                    {
                        case WoWObjectType.Unit:
                            WoWUnit unit = wowObject as WoWUnit;
                            if (unit.IsAlive)
                            {
                                AddObjectDangerZone(wowObject, knownAOE.Radius);
                            }
                            else
                            {
                                RemoveAllObjectDangerZones(unit.Guid);
                            }
                            break;
                        case WoWObjectType.DynamicObject:
                            DynamicObject dObject = new DynamicObject(wowObject.GetBaseAddress);
                            AddObjectDangerZone(dObject, knownAOE.Radius);
                            break;
                        case WoWObjectType.GameObject:
                            AddObjectDangerZone(wowObject, knownAOE.Radius);
                            break;
                        default:
                            break;
                    }
                }
            }

            CalculateReposition();
        }

        private void CalculateReposition()
        {
            Vector3 myPos = _entityCache.Me.PositionWT;

            // Is current fight a Forced Safe Zone fight?
            ForcedSafeZone forcedSafeZone = null;
            foreach (IWoWUnit enemy in _entityCache.EnemiesAttackingGroup)
            {
                if (_forcedSafeZonesDic.ContainsKey(enemy.Entry))
                {
                    forcedSafeZone = _forcedSafeZonesDic[enemy.Entry];
                    break;
                }
            }

            DangerZone currentDangerZone = _dangerZones.Find(dangerZone => dangerZone.PositionInDangerZone(myPos));
            bool inSafeZone = forcedSafeZone == null || forcedSafeZone.PositionInSafeZone(myPos);
            if (currentDangerZone != null || !inSafeZone)
            {
                Logger.Log($"Standing in danger zone!: {currentDangerZone.Name} - {currentDangerZone.Timer?.TimeLeft()}s.");
                RepositionInfo = new RepositionInfo(_dangerZones, forcedSafeZone, currentDangerZone, inSafeZone);
            }
            else
            {
                RepositionInfo = null;
            }
        }

        private void MovementEventsOnMovementPulse(List<Vector3> path, CancelEventArgs cancelable)
        {
            if (path == null || path.Count <= 0) return;

            // Don't cancel during pull
            if (Fight.InFight
                && _entityCache.Target != null
                && !_entityCache.Target.WowUnit.InCombat) return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                DangerZone dangerZoneOnTheWay = _dangerZones
                    .FirstOrDefault(dz =>
                        dz.Position.DistanceTo(_entityCache.Me.PositionWT) < dz.Radius + 5
                        && WTPathFinder.PointDistanceToLine(path[i], path[i + 1], dz.Position) < dz.Radius);
                if (dangerZoneOnTheWay != null && !IAmInDangerZone)
                {
                    Logger.LogOnce($"Can't move, {dangerZoneOnTheWay.Name} is on the path. Waiting despawn.");
                    cancelable.Cancel = true;
                    return;
                }
            }
        }

        private void MovementsEventsOnMoveToPulse(Vector3 node, CancelEventArgs cancelable)
        {
            // Don't cancel during pull
            if (Fight.InFight
                && _entityCache.Target != null
                && !_entityCache.Target.WowUnit.InCombat) return;

            // Cancel moves into danger zones
            DangerZone dangerZoneOnTheWay = _dangerZones.FirstOrDefault(dz =>
                dz.Position.DistanceTo(_entityCache.Me.PositionWT) < dz.Radius + 5
                && WTPathFinder.PointDistanceToLine(_entityCache.Me.PositionWT, node, dz.Position) < dz.Radius);
            if (dangerZoneOnTheWay != null && !IAmInDangerZone)
            {
                Logger.LogOnce($"Can't move, {dangerZoneOnTheWay.Name} is on the path to node. Waiting despawn.");
                cancelable.Cancel = true;
                return;
            }
        }

        private void FightEventHandler(WoWUnit unit, CancelEventArgs cancelable)
        {
            if (RepositionInfo != null)
            {
                Logger.LogOnce($"Canceled fight because we need to reposition");
                Lua.LuaDoString("SpellStopCasting();");
                cancelable.Cancel = true;
            }
        }

        private void DrawEventAOE()
        {
            try
            {
                if (RepositionInfo != null)
                {
                    DangerZone currentDangerZone = RepositionInfo.CurrentDangerZone;
                    ForcedSafeZone currentforcedSafeZone = RepositionInfo.ForcedSafeZone;
                    if (currentDangerZone != null)
                    {
                        Radar3D.DrawCircle(currentDangerZone.Position, currentDangerZone.Radius, Color.Orange, true, 30);
                    }

                    if (currentforcedSafeZone != null)
                    {
                        Radar3D.DrawCircle(currentforcedSafeZone.ZoneCenter, currentforcedSafeZone.Radius, Color.Blue, false, 30);
                    }
                }

                List<DangerZone> dangerZones = new List<DangerZone>(_dangerZones);
                foreach (DangerZone dangerZone in dangerZones)
                {
                    dangerZone.Draw();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }
    }
}
