using System.Collections.Generic;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace Zones
{
    public class Utils
    {
        public static List<UnturnedPlayer> GetGroupMembers(CSteamID groupID)
        {
            var groupMembers = new List<UnturnedPlayer>();
            
            foreach (var client in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(client);
                
                if (player.Player.quests.isMemberOfGroup(groupID))
                    groupMembers.Add(player);
            }

            return groupMembers;
        }
    }
}