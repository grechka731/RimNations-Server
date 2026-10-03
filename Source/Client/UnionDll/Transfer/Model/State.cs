using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class State
    {
        public string Name { get; set; }

        public string Color { get; set; }

        public string Description { get; set; }

        public int MarketValueRanking { get; set; }

        public int MarketValueRankingLast { get; set; }

        public int BorderRadius { get; set; } = 5;

        public int CapitalTile { get; set; }

        public float CustomsTariff { get; set; } = 0.05f;

        public bool BorderBlockadeActive { get; set; } = false;
    }
}
