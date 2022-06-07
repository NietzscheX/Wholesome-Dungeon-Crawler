﻿using robotManager.FiniteStateMachine;
using WholesomeDungeonCrawler.Helpers;
using WholesomeDungeonCrawler.Managers;
using WholesomeDungeonCrawler.ProductCache.Entity;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.States
{
    class DungeonLogic : State, IState
    {
        private readonly IEntityCache _entityCache;
        private readonly IProfileManager _profileManager;
        private readonly IPartyChatManager _partyChatManager;

        public DungeonLogic(IEntityCache iEntityCache,
            IProfileManager profilemanager,
            IPartyChatManager partyChatManager,
            int priority)
        {
            _entityCache = iEntityCache;
            _profileManager = profilemanager;
            _partyChatManager = partyChatManager;
            Priority = priority;
        }
        public override bool NeedToRun
        {
            get
            {
                if (_profileManager.CurrentDungeonProfile?.CurrentStep == null)
                {
                    DisplayName = $"DungeonLogic: None";
                }

                if (!Conditions.InGameAndConnected
                    || !_entityCache.Me.Valid
                    || Fight.InFight
                    || _profileManager.CurrentDungeonProfile == null
                    || _profileManager.CurrentDungeonProfile.CurrentStep == null
                    || _profileManager.CurrentDungeonProfile.CurrentStep.Order > _partyChatManager.TankStatus?.StepOrder)
                {
                    return false;
                }

                return true;
            }
        }

        public override void Run()
        {
            if (_profileManager.CurrentDungeonProfile.CurrentStep.IsCompleted)
            {
                _profileManager.CurrentDungeonProfile.SetCurrentStep();
            }
            else
            {
                DisplayName = $"DungeonLogic: {_profileManager.CurrentDungeonProfile.CurrentStep.Name}";
                _profileManager.CurrentDungeonProfile.CurrentStep.Run();
            }
        }
    }
}
