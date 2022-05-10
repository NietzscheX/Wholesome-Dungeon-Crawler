﻿using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WholesomeDungeonCrawler.Data;
using WholesomeDungeonCrawler.Data.Model;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.Profiles;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.Manager
{
    class ProfileManager : IProfileManager
    {
        private object profileLock = new object();

        public Profile CurrentDungeonProfile { get; private set; }

        private readonly IEntityCache _entityCache;

        public ProfileManager(IEntityCache entityCache)
        {
            _entityCache = entityCache;
        }
        public void Initialize()
        {
            CachePlayerEnteringWorld();
            //starting with Event Substcription
            EventsLua.AttachEventLua("PLAYER_ENTERING_WORLD", m => CachePlayerEnteringWorld());
        }

        private void CachePlayerEnteringWorld()
        {
            lock (profileLock)
            {
                LoadProfile();
            }
        }

        private void LoadProfile()
        {
            DungeonModel dungeon = CheckandChooseactualDungeon();
            if (dungeon != null)
            {
                var profilePath = System.IO.Directory.CreateDirectory($@"{Others.GetCurrentDirectory}/Profiles/WholesomeDungeonCrawler/{dungeon.Name}");
                var profilecount = profilePath.GetFiles().Count();
                if (profilecount > 0)
                {
                    var files = profilePath.GetFiles();
                    var chosenFile = files[new Random().Next(0, files.Length)];
                    Logger.Log($"Randomly selected {chosenFile.Name} from the {dungeon.Name} folder.");
                    var profile = chosenFile.FullName;
                    var deserializedProfile = JsonConvert.DeserializeObject<ProfileModel>(File.ReadAllText(profile), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                    if (deserializedProfile.MapId == dungeon.MapId)
                    {
                        CurrentDungeonProfile = new Profile(deserializedProfile);
                        Logger.Log($"Dungeon Profile loaded: {deserializedProfile.Name}.{Environment.NewLine} with the MapID { deserializedProfile.MapId}.{ Environment.NewLine} with at Total Steps { deserializedProfile.StepModels.Count()}.{ Environment.NewLine}");
                        //PathFinder.OffMeshConnections.AddRange(dungeonProfile.offMeshConnections); <-- in its current state, Profile doesn´t hold any Offmeshes
                    } else
                        Logger.Log($"Dungeon Profile not loaded: {deserializedProfile.Name}.{Environment.NewLine} with the DungeonID { deserializedProfile.MapId} did not match the dungeon id of your current dungeon {dungeon.Name}: {dungeon.MapId}.");
                }
            }
            Logger.Log("No Profile found!");
            return;
        }

        private DungeonModel CheckandChooseactualDungeon()
        {
            if (CheckactualDungeonProfileInList())
            {
                if (Lists.AllDungeons.Count(d => d.MapId == Usefuls.ContinentId) > 1)
                {
                    return Lists.AllDungeons.Where(d => d.MapId == Usefuls.ContinentId).OrderBy(o => o.Start.DistanceTo(_entityCache.Me.PositionWithoutType)).FirstOrDefault();
                }
                if (Lists.AllDungeons.Count(d => d.MapId == Usefuls.ContinentId) == 1)
                {
                    return Lists.AllDungeons.Where(d => d.MapId == Usefuls.ContinentId).FirstOrDefault();
                }
            }
            return null;
        }
        private bool CheckactualDungeonProfileInList()
        {
            return Lists.AllDungeons.Any(d => d.MapId == Usefuls.ContinentId);
        }

        public void Dispose()
        {

        }
    }
}
