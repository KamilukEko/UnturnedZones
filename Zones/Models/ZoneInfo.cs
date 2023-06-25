using System.Collections.Generic;

namespace Zones.Models
{
    public class ZoneInfo
    {
        public string RegionName;
        public ulong GroupID;
        public List<SerializablePlayerQuestFlag> QuestFlags;
        public float PayoutInterval;
        public decimal PayoutValue;
        public int MaxZonePoints;
        public uint ZonePointsPerUpdate;
        public ushort MinHourActive;
        public ushort MaxHourActive;
    }
}