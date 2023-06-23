using System;
using fr34kyn01535.Uconomy;
using ImperialPlugins.AdvancedRegions;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;

namespace Zones.Models
{
    public class Zone
    {
        public readonly ZoneInfo ZoneInfo;
        private float _payoutTime;
        
        public bool UnderAttack;
        private int _zonePoints;
        private uint _defenders;
        private uint _attackers;
        private CSteamID _attackingGroupID;

        protected internal Zone(ZoneInfo zoneInfo)
        {
            ZoneInfo = zoneInfo;
        }
        private void UpdateZonePoints(long newValue)
        {
            string text = new string('|', 224 * ZoneInfo.MaxZonePoints / _zonePoints);
            
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
            _payoutTime -= interval;

            if (UnderAttack)
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
                        UnderAttack = false;
                        UpdateZonePoints(0);
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
                        UnderAttack = false;
                        UpdateZonePoints(0);
                        SendMessageToGroup(_attackingGroupID, $"Twoja grupa przegrała walkę o strefę {ZoneInfo.RegionName} :(");
                        SendMessageToGroup(new CSteamID(ZoneInfo.GroupID), $"Twoja grupa obroniła strefę {ZoneInfo.RegionName} :)");
                        return;
                    }
                }
                
                UpdateZonePoints(newZonePoints);
                return;
            }
            
            if (ZoneInfo.MinHourActive < DateTime.Now.Hour || ZoneInfo.MaxHourActive > DateTime.Now.Hour)
                return;

            if (_payoutTime > 0)
                return;

            foreach (var groupMember in Utils.GetGroupMembers(new CSteamID(ZoneInfo.GroupID)))
            {
                Uconomy.Instance.Database.IncreaseBalance(groupMember.Id, ZoneInfo.PayoutValue);
                UnturnedChat.Say(groupMember.CSteamID, $"Zyskałeś {ZoneInfo.PayoutValue} dzięki strefie - {ZoneInfo.RegionName} :)");
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
            ZoneInfo.GroupID = _attackingGroupID.m_SteamID;
            Main.Instance.Configuration.Instance.Zones.Add(ZoneInfo);
            Main.Instance.Configuration.Save();

            foreach (var groupMember in Utils.GetGroupMembers(_attackingGroupID))
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
            UnderAttack = true;

            foreach (var playerInRegion in AdvancedRegionsPlugin.Instance.RegionsManager.GetRegion(ZoneInfo.RegionName).Players)
            {
                if (playerInRegion.quests.isMemberOfGroup(_attackingGroupID))
                    _attackers += 1;

                if (playerInRegion.quests.isMemberOfGroup(new CSteamID(ZoneInfo.GroupID)))
                    _defenders += 1;
            }
            
            SendMessageToGroup(_attackingGroupID, $"Twoja grupa rozpoczeła atak na strefę - {ZoneInfo.RegionName}");
            SendMessageToGroup(new CSteamID(ZoneInfo.GroupID), $"Rozpoczął się atak na twoją strefę - {ZoneInfo.RegionName}");
        }
        
        public void OnPlayerEntered(Player player)
        {
            if (!UnderAttack)
                return;
            
            string text = new string('|', 224 * _zonePoints / ZoneInfo.MaxZonePoints);
            EffectManager.askEffectClearByID(21370, player.channel.GetOwnerTransportConnection());
            EffectManager.sendUIEffect(21370, 2137, player.channel.GetOwnerTransportConnection(), true,
                _attackers.ToString(), _defenders.ToString(), text);

            if (player.quests.isMemberOfGroup(_attackingGroupID))
            {
                _attackers += 1;
                return;
            }
            
            if (player.quests.isMemberOfGroup(new CSteamID(ZoneInfo.GroupID)))
                _defenders += 1;
        }
        
        public void OnPlayerLeft(Player player)
        {
            if (!UnderAttack)
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