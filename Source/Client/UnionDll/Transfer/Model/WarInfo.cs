using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public enum WarStatus
    {
        Peace = 0,
        WarDeclared = 1, // Фаза подготовки к войне (таймер подготовки, защитник готовится)
        WarActive = 2,   // Активная война (нападение разрешено)
        Truce = 3        // Перемирие после окончания боевых действий
    }

    [Serializable]
    public class WarInfo
    {
        public string AttackerLogin { get; set; }
        public string AttackerState { get; set; }
        public string DefenderLogin { get; set; }
        public string DefenderState { get; set; }
        public WarStatus Status { get; set; }
        public DateTime DeclaredTimeUtc { get; set; }
        public DateTime ActiveTimeUtc { get; set; }
        public DateTime ExpireTimeUtc { get; set; }
        public string CasusBelli { get; set; }

        public bool IsActiveNow => Status == WarStatus.WarActive && DateTime.UtcNow <= ExpireTimeUtc;
        public bool IsInPreparation => Status == WarStatus.WarDeclared && DateTime.UtcNow < ActiveTimeUtc;
        public bool IsInTruce => Status == WarStatus.Truce && DateTime.UtcNow <= ExpireTimeUtc;
    }
}
