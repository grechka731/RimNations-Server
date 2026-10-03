using System;
using System.Collections.Generic;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class AllianceInfo
    {
        public string Name { get; set; }
        public string LeaderLogin { get; set; }
        public List<string> MemberLogins { get; set; } = new List<string>();
        public List<string> MemberStates { get; set; } = new List<string>();
        public DateTime CreatedTimeUtc { get; set; }

        public AllianceInfo()
        {
            CreatedTimeUtc = DateTime.UtcNow;
        }

        public bool ContainsPlayer(string login)
        {
            return MemberLogins != null && MemberLogins.Contains(login);
        }

        public bool ContainsState(string stateName)
        {
            return !string.IsNullOrEmpty(stateName) && MemberStates != null && MemberStates.Contains(stateName);
        }
    }
}
