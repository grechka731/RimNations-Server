using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class PacketTradeOrder
    {
        public string OrderType { get; set; } = "Sell"; // "Buy" or "Sell"
        public string ItemDefName { get; set; }
        public int Count { get; set; }
        public float PricePerUnit { get; set; }
        public int Tile { get; set; }
        public string OwnerLogin { get; set; }
    }
}
