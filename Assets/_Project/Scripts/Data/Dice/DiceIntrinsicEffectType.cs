namespace Tessera.Data
{
    /// <summary>DiceType 자체가 가지는 고유 효과 타입이다.</summary>
    public enum DiceIntrinsicEffectType
    {
        None = 0,

        AddScoreIfOdd = 10,
        AddScoreIfEven = 11,
        AddForceIfOdd = 12,
        AddForceIfEven = 13,

        AddScoreIfValueAtLeast = 20,
        AddScoreIfValueAtMost = 21,

        AddMoneyOnRoundWinIfUsed = 50,
        AddOverchargeOnBrokenCast = 60,

        ReduceIncomingDamageIfUsed = 100
    }
}
