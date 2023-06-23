using System.Collections.Generic;
using System.Linq;
using ImperialPlugins.AdvancedRegions;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Zones.Models;

namespace Zones.Commands
{
  public class Info : IRocketCommand
  {
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "strefa info";
    public string Help => string.Empty;
    public string Syntax => string.Empty;
    
    public List<string> Aliases => new List<string>()
    {
      "strefa informacje"
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
            
      UnturnedChat.Say(caller, $"Nazwa strefy: {zone.ZoneInfo.RegionName}");
      UnturnedChat.Say(caller, $"Opis strefy: {zone.ZoneInfo.Description}");
    }
  }
}