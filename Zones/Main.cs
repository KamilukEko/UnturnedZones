using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImperialPlugins.AdvancedRegions;
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
        public List<Zone> Zones;
        public static Main Instance;
        
        protected override void Load()
        {
            Instance = this;

            foreach (var zone in Configuration.Instance.Zones)
            {
                Zones.Add(new Zone(zone));
            }
            
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerLeaveRegion += OnPlayerLeaveRegion;
            AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerEnterRegion += OnPlayerEnterRegion;
            
            StartCoroutine(nameof(UpdateCoroutine), Configuration.Instance.GlobalUpdateInterval);
            
            Logger.Log("Kamiluk || Zones plugin has been loaded.");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerLeaveRegion -= OnPlayerLeaveRegion;
            AdvancedRegionsPlugin.Instance.RegionsManager.OnPlayerEnterRegion -= OnPlayerEnterRegion;
            
            StopCoroutine(nameof(UpdateCoroutine));
            
            Logger.Log("Kamiluk || Zones plugin has been unloaded.");
        }

        private IEnumerator UpdateCoroutine(float interval)
        {
            foreach (Zone zone in Zones)
                zone.Update(interval);
            yield return new WaitForSeconds(interval);
        }

        private void OnPlayerEnterRegion(Region region, Player player)
        {
            var zone = Zones.FirstOrDefault(x => x.ZoneInfo.RegionName == region.RegionInfo.Name);
            
            if (zone == null)
                return;

            zone.OnPlayerEntered(player);
        }

        private void OnPlayerLeaveRegion(Region region, Player player)
        {
            var zone = Zones.FirstOrDefault(x => x.ZoneInfo.RegionName == region.RegionInfo.Name);
            
            if (zone == null)
                return;

            zone.OnPlayerLeft(player);
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            throw new System.NotImplementedException();
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            var quests = player.Player.quests;
            
            foreach (var zone in Zones)
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