﻿using robotManager.Helpful;
using System.Collections.Generic;
using WholesomeDungeonCrawler.ProductCache;
using WholesomeDungeonCrawler.Profiles.Steps;
using static wManager.Wow.Class.Npc;

namespace WholesomeDungeonCrawler.Profiles
{
    public interface IProfile : ICycleable
    {
        public IStep CurrentStep { get; }
        bool ProfileIsCompleted { get; }
        int MapId { get; }
        List<Vector3> DeathRunPathList { get; }
        Dictionary<IStep, List<Vector3>> DungeonPath { get; }
        List<Vector3> AllMoveAlongNodes { get; }
        FactionType FactionType { get; }

        void AutoSetCurrentStep();
        void SetFirstLaunchStep();
    }
}
