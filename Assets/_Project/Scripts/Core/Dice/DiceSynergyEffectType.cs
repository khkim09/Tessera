namespace Tessera.Core
{
    /// <summary>Core 전투 계산에서 사용하는 DiceSynergy 효과 타입이다.</summary>
    public enum DiceSynergyEffectType
    {
        None = 0,
        AddScoreForOddDice = 10,
        AddScoreForEvenDice = 11,
        AddForceIfOddDiceCountAtLeast = 12,
        AddForceIfEvenDiceCountAtLeast = 13,
        AddScoreForHighDice = 14,
        AddScoreForLowDice = 15,
        AddForceIfLowDiceCountAtLeast = 16,
        IncreaseBrokenCastDamageReduction = 50,
        AddOverchargeOnBrokenCast = 51,
        AddMoneyOnRoundWin = 100
    }
}
