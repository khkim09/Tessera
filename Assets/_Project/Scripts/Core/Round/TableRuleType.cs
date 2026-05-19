namespace Tessera.Core
{
    /// <summary>Round 또는 Boss Round에서 적용되는 테이블 규칙 종류를 정의한다.</summary>
    public enum TableRuleType
    {
        None = 0,
        NonAcesDamagePercent = 1,
        DisableChance = 2,
        DisableBrokenCastReward = 3
    }
}
