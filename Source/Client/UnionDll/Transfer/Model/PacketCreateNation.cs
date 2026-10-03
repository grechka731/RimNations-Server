using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class PacketCreateNation
    {
        public string StateName { get; set; }
        public string Color { get; set; } = "#FFD700";
        public string Description { get; set; }
        public int CapitalTile { get; set; }
        public int BorderRadius { get; set; } = 4;
        public string LeaderLogin { get; set; }
    }
}
