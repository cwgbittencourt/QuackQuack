namespace QuackQuack.Core.Progression
{
    public readonly struct UpgradeConfirmation
    {
        public string OfferId { get; }
        public string ChoiceId { get; }
        public bool IsEvolution { get; }
        public UpgradeConfirmation(string offerId, string choiceId, bool isEvolution)
        { OfferId = offerId; ChoiceId = choiceId; IsEvolution = isEvolution; }
    }
}
