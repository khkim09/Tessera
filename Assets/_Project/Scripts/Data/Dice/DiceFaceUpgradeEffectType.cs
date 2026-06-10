namespace Tessera.Data
{
    /// <summary>특정 Dice의 특정 Face에 부여되는 각인/강화 효과 타입이다.</summary>
    public enum DiceFaceUpgradeEffectType
    {
        None = 0,

        AddScoreWhenRolled = 10,
        AddForceWhenRolled = 11,
        TreatAsNumber = 12,

        AddOverchargeWhenUsed = 50,
        AddMoneyOnRoundWinWhenUsed = 60,

        ReduceIncomingDamageWhenUsed = 100,
        IncreaseIncomingDamageWhenLose = 101
    }
}
