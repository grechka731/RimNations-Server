using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class PacketDeclareWar
    {
        public string AttackerLogin { get; set; }
        public string DefenderLogin { get; set; }
        public string DefenderState { get; set; }
        public string CasusBelli { get; set; } = "Territorial Dispute";
    }
}
