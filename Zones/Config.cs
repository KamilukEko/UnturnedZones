using System.Collections.Generic;
using Rocket.API;
using Zones.Models;

namespace Zones
{
    public class Config : IRocketPluginConfiguration
    {
        public float GlobalUpdateInterval;
        public ushort ZoneAttackNotificationEffectID;
        public List<ZoneInfo> Zones;

        public void LoadDefaults()
        {
            GlobalUpdateInterval = 1.0f;
            ZoneAttackNotificationEffectID = 56;
            
            Zones = new List<ZoneInfo>()
            {
                new ZoneInfo()
                {
                    RegionName = "Test",
                    GroupID = ulong.MaxValue,
                    PayoutInterval = 120.0f,
                    PayoutValue = 100,
                    MaxZonePoints = 6000,
                    ZonePointsPerUpdate = 100,
                    MinHourActive = 10,
                    MaxHourActive = 24,
                    QuestFlags = new List<SerializablePlayerQuestFlag>()
                    {
                        new SerializablePlayerQuestFlag()
                        {
                            Id = 0,
                            Value = 0
                        }
                    }
                }
            };
        }
    }
}