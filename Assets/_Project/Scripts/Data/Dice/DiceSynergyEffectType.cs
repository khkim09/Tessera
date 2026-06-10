namespace Tessera.Data
{
    /// <summary>DiceType 태그 조합으로 활성화되는 시너지 효과 타입이다.</summary>
    public enum DiceSynergyEffectType
    {
        None = 0,

        AddScoreForOddDice = 10,
        AddScoreForEvenDice = 11,
        AddForceIfOddDiceCountAtLeast = 12,
        AddForceIfEvenDiceCountAtLeast = 13,
        AddScoreForHighDice = 14,

        IncreaseBrokenCastDamageReduction = 50,
        AddOverchargeOnBrokenCast = 51,

        AddMoneyOnRoundWin = 100
    }
}
