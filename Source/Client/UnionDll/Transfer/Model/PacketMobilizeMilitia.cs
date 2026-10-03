using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class PacketMobilizeMilitia
    {
        public string StateName { get; set; }
        public int FromTile { get; set; }
        public int TargetTile { get; set; }
        public int SquadSize { get; set; } = 4;
        public string ArchetypeDefName { get; set; }
        public string CommanderLogin { get; set; }
    }
}
