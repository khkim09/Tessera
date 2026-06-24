namespace Tessera.Core
{
    /// <summary>Round 또는 Boss Round에서 적용되는 테이블 규칙 종류를 정의한다.</summary>
    public enum TableRuleType
    {
        None = 0, // 규칙 없음
        NonAcesCastPowerPercent = 1, // Aces가 아닌 CastPower 비율 보정
        DisableChance = 2, // Chance Cast 사용 금지
        DisableBrokenCastReward = 3 // Broken Cast 보상 금지
    }
}
