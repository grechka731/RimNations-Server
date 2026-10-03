using System;

namespace Transfer.ModelMails
{
    [Serializable]
    public enum DiplomacyProposalKind
    {
        AllianceOffer = 1,
        PeaceOffer = 2,
        WarDeclaration = 3,
        AllianceBroken = 4
    }

    [Serializable]
    public class ModelMailDiplomacyProposal : ModelMail
    {
        public DiplomacyProposalKind Kind { get; set; }
        public string SenderLogin { get; set; }
        public string SenderState { get; set; }
        public string TargetLogin { get; set; }
        public string TargetState { get; set; }
        public string MessageText { get; set; }
        public int DurationDays { get; set; } = 7;
        public int TributeSilver { get; set; } = 0;

        public override string GetHash()
        {
            return $"Diplo_{Kind}_{SenderLogin}_{TargetLogin}_{Created.Ticks}";
        }
    }
}
