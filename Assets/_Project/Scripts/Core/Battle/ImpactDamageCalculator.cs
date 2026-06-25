using System;

namespace Tessera.Core
{
    /// <summary>CastPower 승패 결과를 실제 ImpactDamage로 변환한다.</summary>
    public class ImpactDamageCalculator
    {
        /// <summary>승자와 패자의 CastPower 차이를 기준으로 최종 ImpactDamage를 계산한다.</summary>
        public ImpactDamageBreakdown Calculate(
            RoundRuleContext ruleContext,
            ClashCastResult attackerResult,
            ClashCastResult defenderResult,
            int finalModifierPercent)
        {
            if (ruleContext == null)
                throw new ArgumentNullException(nameof(ruleContext));

            if (attackerResult == null || attackerResult.IsBrokenCast)
                return ImpactDamageBreakdown.Zero(ruleContext.ImpactCap);

            int defenderCastPower = defenderResult != null ? defenderResult.CastPower : 0;

            return CalculateExpected(
                ruleContext,
                attackerResult,
                defenderCastPower,
                finalModifierPercent);
        }

        /// <summary>지정한 방어자 CastPower를 기준으로 예상 ImpactDamage를 계산한다.</summary>
        public ImpactDamageBreakdown CalculateExpected(
            RoundRuleContext ruleContext,
            ClashCastResult attackerResult,
            int defenderCastPower,
            int finalModifierPercent = 100)
        {
            if (ruleContext == null)
                throw new ArgumentNullException(nameof(ruleContext));

            if (attackerResult == null || attackerResult.IsBrokenCast)
                return ImpactDamageBreakdown.Zero(ruleContext.ImpactCap);

            int attackerCastPower = Math.Max(0, attackerResult.CastPower);
            int margin = Math.Max(0, attackerCastPower - Math.Max(0, defenderCastPower));

            int deviceImpactBonus = attackerResult.SlotPairDamagePreview != null
                ? attackerResult.SlotPairDamagePreview.DeviceImpactBonus
                : 0;

            int trueImpactDamage = attackerResult.SlotPairDamagePreview != null
                ? attackerResult.SlotPairDamagePreview.TrueImpactDamage
                : 0;

            return new ImpactDamageBreakdown(
                attackerResult.PatternResult.BaseImpact,
                CalculatePowerTierBonusForDebug(attackerCastPower),
                CalculateMarginTierBonusForDebug(margin),
                deviceImpactBonus,
                trueImpactDamage,
                0,
                ruleContext.ImpactCap,
                finalModifierPercent);
        }

        /// <summary>디버그 리포트와 실제 계산이 공유하는 PowerTierBonus 값을 계산한다.</summary>
        public static int CalculatePowerTierBonusForDebug(int castPower)
        {
            return Math.Min(7, Math.Max(0, castPower) / 45);
        }

        /// <summary>디버그 리포트와 실제 계산이 공유하는 MarginTierBonus 값을 계산한다.</summary>
        public static int CalculateMarginTierBonusForDebug(int margin)
        {
            return Math.Min(6, Math.Max(0, margin) / 35);
        }
    }
}
