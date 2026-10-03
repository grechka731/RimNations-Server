using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class DiplomacyTreaty
    {
        public string VictorLogin { get; set; }
        public string VictorState { get; set; }
        public string DefeatedLogin { get; set; }
        public string DefeatedState { get; set; }
        public int DurationDays { get; set; } = 7;
        public int TributeSilverPerDay { get; set; } = 250;
        public float TributeWealthPercent { get; set; } = 20.0f; // Максимум 25-30%
        public DateTime CreatedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }

        public bool IsActive => DateTime.UtcNow <= ExpiresUtc;

        public DiplomacyTreaty()
        {
            CreatedUtc = DateTime.UtcNow;
            ExpiresUtc = CreatedUtc.AddDays(DurationDays);
        }
    }
}
