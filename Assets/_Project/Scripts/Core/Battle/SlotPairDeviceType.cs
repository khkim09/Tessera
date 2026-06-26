namespace Tessera.Core
{
    /// <summary>LockSlot과 1:1 대응되는 Device 효과 종류를 정의한다.</summary>
    public enum SlotPairDeviceType
    {
        None = 0,

        AddScoreByDiceValue = 1,
        AddForceIfDiceIncluded = 2,
        AddForceIfSameAsPrevious = 3,
        MultiplyForceIfCurrentForceAtLeast = 4,
        AddScoreIfCastType = 5,

        AddScoreIfDiceParity = 10,
        AddForceIfDiceParity = 11,
        MultiplyForceIfDiceParity = 12,

        AddScoreIfDiceValueAtLeast = 20,
        AddScoreIfDiceValueAtMost = 21,
        AddForceIfDiceValueAtLeast = 22,
        AddForceIfDiceValueAtMost = 23,
        MultiplyForceIfDiceValueAtLeast = 24,

        AddScoreIfSlotIndex = 30,
        AddTruePowerIfSlotIndex = 31,

        AddForceIfGreaterThanPrevious = 40,
        MultiplyForceIfGreaterThanPrevious = 41,
        AddForceIfSameAsMirrorSlot = 42,
        AddScoreIfIsolatedFromNeighbors = 43,

        AddScoreIfCastTypeEither = 50,
        AddForceIfCastTypeEither = 51,
        MultiplyForceIfCastType = 52,

        AddTruePowerIfPreviousSlotsSumAtLeast = 60,

        AddScoreIfStageThreatAtLeast = 70,
        AddForceIfStageThreatAtLeast = 71,

        // ImpactDamage 계열
        AddDeviceImpactBonusIfSlotActive = 80, // 해당 Device 슬롯이 활성 상태이면 DeviceImpactBonus 증가
        AddDeviceImpactBonusIfDiceValueAtLeast = 81, // 주사위 값이 기준 이상이면 DeviceImpactBonus 증가
        AddTrueImpactDamageIfCastPowerAtLeast = 82, // CastPower가 기준 이상이면 TrueImpactDamage 증가

        // 1차 계산기 적용 보류. BrokenCast/Clash 후처리 단계에서 별도 처리한다.
        AddOverchargeOnBrokenCast = 200,
        ReduceIncomingDamageOnBrokenCast = 201
    }
}
