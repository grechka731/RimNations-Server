using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class PacketUpdateTariff
    {
        public string StateName { get; set; }
        public float CustomsTariff { get; set; } = 0.05f; // Rate between 0.00f and 0.20f (0% to 20%)
        public bool BorderBlockadeActive { get; set; } = false;
    }
}
