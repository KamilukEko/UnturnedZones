using System;
using fr34kyn01535.Uconomy;
using ImperialPlugins.AdvancedRegions;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;

namespace Zones.Models
{
    public class Zone
    {
        public readonly ZoneInfo ZoneInfo;
        private float _payoutTime;

        private bool _underAttack;
        private long _zonePoints;
        private uint _defenders;
        private uint _attackers;
        private CSteamID _attackingGroupID;

        public Zone(ZoneInfo zoneInfo)
        {
            ZoneInfo = zoneInfo;
            _zonePoints = ZoneInfo.MaxZonePoints;
        }
        private void UpdateUI(long newValue)
        {
            string text = new string('|', (int)(224 * (ZoneInfo.MaxZonePoints - newValue) / ZoneInfo.MaxZonePoints));

            foreach (var player in AdvancedRegionsPlugin.Instance.RegionsManager.GetRegion(ZoneInfo.RegionName).Players)
            {
                if (newValue == 0)
                {
                    EffectManager.askEffectClearByID(21370, player.channel.GetOwnerTransportConnection());
                    continue;
                }
                
                EffectManager.sendUIEffectText(2137, player.channel.GetOwnerTransportConnection(), true, "{0}", _attackers.ToString());
                EffectManager.sendUIEffectText(2137, player.channel.GetOwnerTransportConnection(), true, "{1}", _defenders.ToString());
                EffectManager.sendUIEffectText(2137, player.channel.GetOwnerTransportConnection(), true, "{2}", text);
            }
        }

        public void Update(float interval)
        {
            if (_underAttack)
            {
                long newZonePoints;
                
                if (_attackers > _defenders)
                {
                    newZonePoints = _zonePoints - ZoneInfo.ZonePointsPerUpdate;

                    if (newZonePoints <= 0)
                    {
                        _zonePoints = ZoneInfo.MaxZonePoints;
                        _attackers = 0;
                        _defenders = 0;
                        _underAttack = false;
                        UpdateUI(0);
                        ChangeOwnerGroup(_attackingGroupID);
                        return;
                    }
                }
                else
                {
                    newZonePoints = _zonePoints + ZoneInfo.ZonePointsPerUpdate;

                    if (newZonePoints >= ZoneInfo.MaxZonePoints)
                    {
                        _zonePoints = ZoneInfo.MaxZonePoints;
                        _attackers = 0;
                        _defenders = 0;
                        _underAttack = false;
                        UpdateUI(0);
                        SendMessageToGroup(_attackingGroupID, $"Twoja grupa przegrała walkę o strefę {ZoneInfo.RegionName}");
                        SendMessageToGroup(new CSteamID(ZoneInfo.GroupID), $"Twoja grupa obroniła strefę {ZoneInfo.RegionName}");
                        return;
                    }
                }

                _zonePoints = newZonePoints;
                UpdateUI(newZonePoints);
                return;
            }
            
            if (ZoneInfo.PayoutValue == 0)
                return;
            
            _payoutTime -= interval;

            if (ZoneInfo.MinHourActive >= DateTime.Now.Hour || ZoneInfo.MaxHourActive <= DateTime.Now.Hour)
                return;

            if (_payoutTime > 0)
                return;

            foreach (var groupMember in Utils.GetGroupMembers(new CSteamID(ZoneInfo.GroupID)))
            {
                Uconomy.Instance.Database.IncreaseBalance(groupMember.Id, ZoneInfo.PayoutValue);
                UnturnedChat.Say(groupMember.CSteamID, $"Zyskałeś {ZoneInfo.PayoutValue} dzięki strefie - {ZoneInfo.RegionName}");
            }
            
            _payoutTime = ZoneInfo.PayoutInterval;
        }

        public void ChangeOwnerGroup(CSteamID newOwnerGroupID)
        {
            foreach (var groupMember in Utils.GetGroupMembers(new CSteamID(ZoneInfo.GroupID)))
            {
                foreach (var questFlag in ZoneInfo.QuestFlags)
                {
                    groupMember.Player.quests.removeFlag(questFlag.Id);
                }
                
                UnturnedChat.Say(groupMember.CSteamID, $"Twoja grupa straciła strefę - {ZoneInfo.RegionName}");
            }

            Main.Instance.Configuration.Instance.Zones.Remove(ZoneInfo);
            ZoneInfo.GroupID = newOwnerGroupID.m_SteamID;
            Main.Instance.Configuration.Instance.Zones.Add(ZoneInfo);
            Main.Instance.Configuration.Save();

            foreach (var groupMember in Utils.GetGroupMembers(newOwnerGroupID))
            {
                foreach (var questFlag in ZoneInfo.QuestFlags)
                {
                    groupMember.Player.quests.setFlag(questFlag.Id, questFlag.Value);
                }
                
                UnturnedChat.Say(groupMember.CSteamID, $"Twoja grupa przejeła strefę - {ZoneInfo.RegionName}");
            }
        }

        private void SendMessageToGroup(CSteamID groupID, string message)
        {
            foreach (var groupMember in Utils.GetGroupMembers(groupID))
            {
                UnturnedChat.Say(groupMember.CSteamID, message);
            }
        }
        
        public void Attack(Player player)
        {
            _attackingGroupID = player.quests.groupID;
            _underAttack = true;

            foreach (var playerInRegion in AdvancedRegionsPlugin.Instance.RegionsManager.GetRegion(ZoneInfo.RegionName).Players)
            {
                if (playerInRegion.quests.isMemberOfGroup(_attackingGroupID))
                    _attackers += 1;

                if (playerInRegion.quests.isMemberOfGroup(new CSteamID(ZoneInfo.GroupID)))
                    _defenders += 1;
                
                EffectManager.sendUIEffect(21370, 2137, player.channel.GetOwnerTransportConnection(), true,
                    _attackers.ToString(), _defenders.ToString(), "");
                EffectManager.sendUIEffectText(2137, player.channel.GetOwnerTransportConnection(), true, "{2}", "");
            }
            
            
            SendMessageToGroup(_attackingGroupID, $"Twoja grupa rozpoczeła atak na strefę - {ZoneInfo.RegionName}");
            foreach (var groupMember in Utils.GetGroupMembers(new CSteamID(ZoneInfo.GroupID)))
            {
                EffectManager.sendEffect(Main.Instance.Configuration.Instance.ZoneAttackNotificationEffectID,
                    groupMember.CSteamID, groupMember.Position);
                UnturnedChat.Say(groupMember.CSteamID, $"Rozpoczął się atak na twoją strefę - {ZoneInfo.RegionName}");
            }
        }
        
        public void OnPlayerEntered(Player player)
        {
            if (!_underAttack)
            {
                if (!player.quests.isMemberOfAGroup)
                {
                    UnturnedChat.Say(player.channel.owner.playerID.steamID, "Musisz być członkiem grupy aby zaatakować strefę");
                    return;
                }
            
                if (ZoneInfo.GroupID == player.quests.groupID.m_SteamID)
                {
                    UnturnedChat.Say(player.channel.owner.playerID.steamID, "Wchodzisz na teren własnej strefy");
                    return;
                }

                if (ZoneInfo.MinHourActive >= DateTime.Now.Hour || ZoneInfo.MaxHourActive <= DateTime.Now.Hour)
                {
                    UnturnedChat.Say(player.channel.owner.playerID.steamID, $"Ta strefa jest aktywna w godzinach {ZoneInfo.MinHourActive}-{ZoneInfo.MaxHourActive}.");
                    return;
                }

                Attack(player);
                return;
            }

            string text = new string('|', (int)(224 * (ZoneInfo.MaxZonePoints - _zonePoints) / ZoneInfo.MaxZonePoints));
            EffectManager.askEffectClearByID(21370, player.channel.GetOwnerTransportConnection());
            EffectManager.sendUIEffect(21370, 2137, player.channel.GetOwnerTransportConnection(), true,
                _attackers.ToString(), _defenders.ToString(), text);
            EffectManager.sendUIEffectText(2137, player.channel.GetOwnerTransportConnection(), true, "{2}", text);

            if (player.quests.isMemberOfGroup(_attackingGroupID))
            {
                _attackers += 1;
                return;
            }
            
            if (player.quests.isMemberOfGroup(new CSteamID(ZoneInfo.GroupID)))
            {
                _defenders += 1;
                return;
            }
            
            UnturnedChat.Say(player.channel.owner.playerID.steamID, "Ta strefa jest przejmowana przez inną grupę.");
        }
        
        public void OnPlayerLeft(Player player)
        {
            if (!_underAttack)
                return;
          
            EffectManager.askEffectClearByID(21370, player.channel.GetOwnerTransportConnection());
            
            if (player.quests.isMemberOfGroup(_attackingGroupID))
            {
                _attackers -= 1;
                return;
            }
            
            if (player.quests.isMemberOfGroup(new CSteamID(ZoneInfo.GroupID)))
                _defenders -= 1;
        }
    }
}