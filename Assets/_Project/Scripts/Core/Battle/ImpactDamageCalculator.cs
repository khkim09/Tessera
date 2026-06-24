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
                CalculatePowerTierBonus(attackerCastPower),
                CalculateMarginTierBonus(margin),
                deviceImpactBonus,
                trueImpactDamage,
                0,
                ruleContext.ImpactCap,
                finalModifierPercent);
        }

        /// <summary>CastPower 구간 보너스를 계산한다.</summary>
        private static int CalculatePowerTierBonus(int castPower)
        {
            if (castPower >= 250)
                return 4;

            if (castPower >= 160)
                return 3;

            if (castPower >= 100)
                return 2;

            if (castPower >= 50)
                return 1;

            return 0;
        }

        /// <summary>승리 CastPower 차이 구간 보너스를 계산한다.</summary>
        private static int CalculateMarginTierBonus(int margin)
        {
            if (margin >= 150)
                return 3;

            if (margin >= 75)
                return 2;

            if (margin >= 25)
                return 1;

            return 0;
        }
    }
}
