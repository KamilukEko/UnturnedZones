using System;
using System.Collections.Generic;
using System.Linq;
using ImperialPlugins.AdvancedRegions;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Zones.Models;

namespace Zones.Commands
{
    public class Attack: IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "strefa zaatakuj";
        public string Help => string.Empty;
        public string Syntax => string.Empty;

        public List<string> Aliases => new List<string>()
        {
            "strefa atakuj"
        };
        public List<string> Permissions => new List<string>()
        {
            "player"
        };
        
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer) caller;

            var regions = AdvancedRegionsPlugin.Instance.RegionsManager.GetRegionsForPlayer(player.Player);

            Zone zone = null;

            foreach (var region in regions)
            {
                zone = Main.Instance.Zones.FirstOrDefault(x => x.ZoneInfo.RegionName == region.RegionInfo.Name);
                break;
            }

            if (zone == null)
            {
                UnturnedChat.Say(caller, "Nie znajdujesz się w żadnej strefie ;(");
                return;
            }
            
            if (!player.Player.quests.isMemberOfAGroup)
            {
                UnturnedChat.Say(caller, "Musisz być członkiem grupy aby zaatakować strefę ;(");
                return;
            }
            
            if (zone.ZoneInfo.GroupID == player.Player.quests.groupID.m_SteamID)
            {
                UnturnedChat.Say(caller, "Nie możesz zaatakować własnej strefy ;(");
                return;
            }

            if (zone.UnderAttack)
            {
                UnturnedChat.Say(caller, "Ta strefa jest już atakowana ;(");
                return;
            }

            if (zone.ZoneInfo.MinHourActive < DateTime.Now.Hour || zone.ZoneInfo.MaxHourActive > DateTime.Now.Hour)
            {
                UnturnedChat.Say(caller, $"Ta strefa jest aktywna w godzinach {zone.ZoneInfo.MinHourActive}-{zone.ZoneInfo.MaxHourActive}.");
                return;
            }
            
            zone.Attack(player.Player);
        }
    }
}