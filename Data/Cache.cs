﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WholesomeToolbox;
using wManager.Events;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.Data
{
    internal class Cache : ICycleable, ICache
    {

        private object cacheLock = new object();


        public bool IsInInstance { get; private set; }
        public bool IsPartyInviteRequest { get; private set; }

        public string StaticPopupText = Lua.LuaDoString<string>("StaticPopup1Text:GetText()");

        public Cache()
        {
        }

        public void Initialize()
        {
            //First Initalization of Variables
            IsInInstance = WTLocation.IsInInstance();
            IsPartyInviteRequest = StaticPopupText.Contains("invites you to a group");
            //Beginning of Event Subscriptions
            ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
            EventsLua.AttachEventLua("WORLD_MAP_UPDATE", m => CacheIsInInstance());
            EventsLua.AttachEventLua("PARTY_INVITE_REQUEST", m => CachePartyInviteRequest());
        }

        public void Dispose()
        {
            ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
        }


        private void OnObjectManagerPulse()
        {
            lock (cacheLock)
            {
                
            }
        }

        private void CacheIsInInstance()
        {
            lock (cacheLock)
            {
                IsInInstance = WTLocation.IsInInstance();
            }

        }
        private void CachePartyInviteRequest()
        {

            IsPartyInviteRequest = StaticPopupText.Contains("invites you to a group");
        }
    }
}
