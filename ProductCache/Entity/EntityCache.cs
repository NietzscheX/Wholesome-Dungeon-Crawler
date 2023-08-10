﻿using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WholesomeDungeonCrawler.CrawlerSettings;
using WholesomeDungeonCrawler.Helpers;
using wManager.Events;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.ProductCache.Entity
{
    internal class EntityCache : IEntityCache
    {
        private object cacheLock = new object();
        private bool IAmShaman;

        public EntityCache()
        {
        }

        public void Dispose()
        {
            ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
        }
        public void Initialize()
        {
            IAmShaman = ObjectManager.Me.WowClass == WoWClass.Shaman;
            CachePartyMembersInfo();
            OnObjectManagerPulse();
            ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
            IAmTank = WholesomeDungeonCrawlerSettings.CurrentSetting.LFGRole == LFGRoles.Tank;
        }

        public ICachedWoWUnit Target { get; private set; } = Cache(new WoWUnit(0));
        public ICachedWoWUnit Pet { get; private set; } = Cache(new WoWUnit(0));
        public ICachedWoWUnit[] GroupPets { get; private set; } = new ICachedWoWUnit[0];
        public ICachedWoWLocalPlayer Me { get; private set; } = Cache(new WoWLocalPlayer(0));
        public ICachedWoWUnit[] EnemyUnitsList { get; private set; } = new ICachedWoWUnit[0];
        public ICachedWoWPlayer[] ListGroupMember { get; private set; } = new ICachedWoWPlayer[0];
        public List<string> ListPartyMemberNames { get; private set; } = new List<string>();
        public ICachedWoWUnit[] EnemiesAttackingGroup { get; private set; } = new ICachedWoWUnit[0];
        public ICachedWoWPlayer TankUnit { get; private set; }
        private List<ulong> _listPartyMemberGuid { get; set; } = new List<ulong>();
        private ulong _tankGuid { get; set; }
        public bool IAmTank { get; private set; }

        private List<int> _npcToDefendEntries = new List<int>();
        public List<ICachedWoWUnit> NpcsToDefend { get; private set; } = new List<ICachedWoWUnit>();
        public List<ICachedWoWUnit> LootableUnits { get; private set; } = new List<ICachedWoWUnit>();
        public void AddNpcIdToDefend(int npcId) => _npcToDefendEntries.Add(npcId);
        public void ClearNpcListIdToDefend() => _npcToDefendEntries.Clear();

        private static ICachedWoWLocalPlayer Cache(WoWLocalPlayer player) => new CachedWoWLocalPlayer(player);
        private static ICachedWoWUnit Cache(WoWUnit unit) => new CachedWoWUnit(unit);
        private static ICachedWoWPlayer Cache(WoWPlayer player) => new CachedWoWPlayer(player);

        private void OnObjectManagerPulse()
        {
            try
            {
                Stopwatch watchTotal = Stopwatch.StartNew();
                Stopwatch watchInit = Stopwatch.StartNew();
                ICachedWoWLocalPlayer cachedMe;
                ICachedWoWUnit cachedTarget, cachedPet;
                List<WoWUnit> units;
                List<WoWPlayer> playerUnits;

                if (!Conditions.InGameAndConnected)
                {
                    return;
                }

                lock (cacheLock)
                {
                    cachedMe = Cache(ObjectManager.Me);
                    //Stopwatch watchTarget = Stopwatch.StartNew();
                    cachedTarget = ObjectManager.Target.Guid != 0 ? Cache(ObjectManager.Target) : Cache(new WoWUnit(0)); // Is occasionally slow with shaman for some reason
                    //if (watchTarget.ElapsedMilliseconds > 50) Logger.LogError($"Target took {watchTarget.ElapsedMilliseconds}");
                    cachedPet = IAmShaman ? Cache(new WoWUnit(0)) : Cache(ObjectManager.Pet);
                    units = ObjectManager.GetObjectWoWUnit();
                    playerUnits = ObjectManager.GetObjectWoWPlayer();
                }

                long initTime = watchInit.ElapsedMilliseconds;
                Stopwatch playersWatch = Stopwatch.StartNew();

                var enemyAttackingGroup = new List<ICachedWoWUnit>();
                var enemyUnits = new List<ICachedWoWUnit>();
                var listGroupMember = new List<ICachedWoWPlayer>();
                var groupPets = new List<ICachedWoWUnit>();

                var targetGuid = cachedTarget.Guid;
                var playerPosition = cachedMe.PositionWT;

                ICachedWoWPlayer tankUnit = null;

                foreach (WoWPlayer play in playerUnits)
                {
                    ICachedWoWPlayer cachedplayer = Cache(play);
                    if (_listPartyMemberGuid.Contains(cachedplayer.Guid))
                    {
                        listGroupMember.Add(cachedplayer);
                        if (cachedplayer.Guid == _tankGuid)
                        {
                            tankUnit = cachedplayer;
                        }
                    }
                }

                ListGroupMember = listGroupMember.ToArray();

                long playersTime = playersWatch.ElapsedMilliseconds;
                Stopwatch enemiesWatch = Stopwatch.StartNew();

                NpcsToDefend.Clear();
                LootableUnits.Clear();
                TankUnit = tankUnit;

                List<ulong> allTeamGuids = new List<ulong>();
                allTeamGuids.Add(cachedMe.Guid);
                allTeamGuids.AddRange(GroupPets.Select(gp => gp.Guid));
                allTeamGuids.AddRange(ListGroupMember.Select(lgm => lgm.Guid));

                foreach (WoWUnit unit in units)
                {
                    // Ignored mobs from list
                    if (Lists.IgnoredMobs.Contains(unit.Entry))
                    {
                        continue;
                    }

                    ulong unitGuid = unit.Guid;
                    ICachedWoWUnit cachedUnit = unitGuid == targetGuid ? cachedTarget : Cache(unit);
                    bool? cachedReachable = unitGuid == targetGuid ? true : (bool?)null;
                    Vector3 unitPosition = unit.PositionWithoutType;

                    if (_listPartyMemberGuid.Contains(unit.PetOwnerGuid) || unit.IsMyPet)
                    {
                        groupPets.Add(cachedUnit);
                        continue;
                    }

                    if (unit.IsAlive && _npcToDefendEntries.Contains(unit.Entry))
                    {
                        NpcsToDefend.Add(cachedUnit);
                        continue;
                    }

                    if (!unit.IsAlive && unit.IsLootable)
                    {
                        LootableUnits.Add(cachedUnit);
                    }

                    if (!unit.IsAlive || unit.NotSelectable)
                    {
                        continue;
                    }

                    if (unit.Level > 1
                        && unit.Reaction <= Reaction.Neutral
                        && unit.PositionWithoutType.DistanceTo(playerPosition) <= 70)
                    {
                        enemyUnits.Add(cachedUnit);
                        ulong unitTargetGuid = unit.Target;
                        if (unitTargetGuid != 0
                            && allTeamGuids.Contains(unitTargetGuid))
                        {
                            enemyAttackingGroup.Add(cachedUnit);
                        }
                    }
                }

                Me = cachedMe;
                Target = cachedTarget;
                Pet = cachedPet;

                EnemiesAttackingGroup = enemyAttackingGroup.ToArray();
                EnemyUnitsList = enemyUnits.ToArray();
                GroupPets = groupPets.ToArray(); // Also contains totems

                long enemiesTime = enemiesWatch.ElapsedMilliseconds;

                if (watchTotal.ElapsedMilliseconds > 100)
                    Logger.LogError($"[init: {initTime}] [players: {playersTime}] [{enemyUnits.Count} enemies: {enemiesTime}]");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        // Records name and GUIDs of other players even if outside object manager
        private void CachePartyMembersInfo()
        {
            lock (cacheLock)
            {
                try
                {
                    //Stopwatch watch = Stopwatch.StartNew();
                    string pList = Lua.LuaDoString<string>(@"
                        plist='';
                        for i=1,4 do
                            local unitName = UnitName('party'..i);
                            local unitGuid = UnitGUID('party'..i);
                            if unitName then
                                plist = plist .. unitName .. '|' .. tonumber(unitGuid) ..',';
                            end
                        end
                        return plist;
                    ");
                    //long luaTime = watch.ElapsedMilliseconds;
                    if (string.IsNullOrEmpty(pList))
                    {
                        ListPartyMemberNames.Clear();
                        _listPartyMemberGuid.Clear();
                        return;
                    }

                    List<string> namesAndGuid = pList.Remove(pList.Length - 1, 1).Split(',').ToList();
                    List<string> partyNames = new List<string>();
                    List<ulong> partyGuids = new List<ulong>();
                    foreach (string nameAndGuid in namesAndGuid)
                    {
                        string[] splitNameAndGuid = nameAndGuid.Split('|');
                        if (splitNameAndGuid.Length != 2)
                        {
                            Logger.LogError($"ERROR: splitNameAndGuid's {nameAndGuid} length wasn't 2!");
                            continue;
                        }

                        if (ulong.TryParse(splitNameAndGuid[1], out ulong guid))
                        {
                            string memberName = splitNameAndGuid[0];
                            if (memberName == WholesomeDungeonCrawlerSettings.CurrentSetting.TankName)
                            {
                                _tankGuid = guid;
                            }
                            partyNames.Add(memberName);
                            partyGuids.Add(guid);
                        }
                        else
                        {
                            Logger.LogError($"ERROR: unit guid {splitNameAndGuid[1]} couldn't be parsed!");
                            continue;
                        }
                    }
                    //long parseTime = watch.ElapsedMilliseconds;

                    if (!Enumerable.SequenceEqual(ListPartyMemberNames, partyNames)
                        || !Enumerable.SequenceEqual(_listPartyMemberGuid, partyGuids))
                    {
                        Logger.Log($"Party: {string.Join(", ", partyNames)} with GUIDs {string.Join(", ", partyGuids)}");
                    }

                    ListPartyMemberNames = partyNames;
                    _listPartyMemberGuid = partyGuids;
                    //Logger.LogError($"Party UP {watch.ElapsedMilliseconds} [lua {luaTime}] [parse [{parseTime}]] ");
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                }
            }
        }

        public void CacheGroupMembers(string trigger)
        {
            Logger.LogDebug($"CacheGroupMembers called by {trigger}");
            CachePartyMembersInfo();
        }
    }
}
