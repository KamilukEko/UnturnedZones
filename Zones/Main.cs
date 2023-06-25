using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Zones.Models;
using Logger = Rocket.Core.Logging.Logger;

namespace Zones
{
    public class Main: RocketPlugin<Config>
    {
        private List<Zone> _zones;
        private IEnumerator _updateCoroutine;
        public static Main Instance;
        protected override void Load()
        {
            Instance = this;
            _zones = new List<Zone>();
            
            foreach (var zone in Configuration.Instance.Zones)
            {
                _zones.Add(new Zone(zone));
            }
            
            U.Events.OnPlayerConnected += OnPlayerConnected;
            _updateCoroutine = UpdateCoroutine(Configuration.Instance.GlobalUpdateInterval);
            StartCoroutine(_updateCoroutine);
            
            Logger.Log("Kamiluk || Zones plugin has been loaded.");
        }

        private void OnLevelWasLoaded(int level)
        {
            ImperialPlugins.AdvancedRegions.AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerLeaveRegion +=
                OnPlayerLeaveRegion;
            ImperialPlugins.AdvancedRegions.AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerEnterRegion +=
                OnPlayerEnterRegion;
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            ImperialPlugins.AdvancedRegions.AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerLeaveRegion -= OnPlayerLeaveRegion;
            ImperialPlugins.AdvancedRegions.AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerEnterRegion -= OnPlayerEnterRegion;
            
            StopCoroutine(_updateCoroutine);
            
            Logger.Log("Kamiluk || Zones plugin has been unloaded.");
        }

        private IEnumerator UpdateCoroutine(float interval)
        {
            while (true)
            {
                foreach (Zone zone in _zones)
                    zone.Update(interval);
                yield return new WaitForSeconds(interval);;
            }
        }

        private void OnPlayerEnterRegion(ImperialPlugins.AdvancedRegions.Region region, Player player)
        {
            var zone = _zones.FirstOrDefault(x => x.ZoneInfo.RegionName == region.RegionInfo.Name);
            
            if (zone == null)
                return;

            zone.OnPlayerEntered(player);
        }

        private void OnPlayerLeaveRegion(ImperialPlugins.AdvancedRegions.Region region, Player player)
        {
            var zone = _zones.FirstOrDefault(x => x.ZoneInfo.RegionName == region.RegionInfo.Name);

            if (zone == null)
                return;

            zone.OnPlayerLeft(player);
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            var quests = player.Player.quests;
            
            foreach (var zone in _zones)
            {
                if (quests.groupID.m_SteamID == zone.ZoneInfo.GroupID)
                {
                    UnturnedChat.Say(player.CSteamID, $"Twoja grupa jest w posiadaniu strefy - {zone.ZoneInfo.RegionName}.");
                    
                    foreach (var questFlag in zone.ZoneInfo.QuestFlags)
                    {
                        quests.setFlag(questFlag.Id, questFlag.Value);
                    }
                    
                    continue;
                }
                
                foreach (var questFlag in zone.ZoneInfo.QuestFlags)
                {
                    quests.removeFlag(questFlag.Id);
                }
            }

        }
    }
}