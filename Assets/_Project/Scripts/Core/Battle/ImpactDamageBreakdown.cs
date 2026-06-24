using System;

namespace Tessera.Core
{
    /// <summary>ImpactDamage 계산 단계별 결과를 보관한다.</summary>
    public class ImpactDamageBreakdown
    {
        /// <summary>Cast 등급에서 오는 기본 Impact 값이다.</summary>
        public int BaseImpact { get; }

        /// <summary>승자 CastPower 구간에서 오는 Impact 보너스다.</summary>
        public int PowerTierBonus { get; }

        /// <summary>승리 CastPower 차이에서 오는 Impact 보너스다.</summary>
        public int MarginTierBonus { get; }

        /// <summary>Device가 제공하는 일반 Impact 보너스다.</summary>
        public int DeviceImpactBonus { get; }

        /// <summary>Device나 TableRule이 제공하는 고정 Impact 보너스다.</summary>
        public int TrueImpactDamage { get; }

        /// <summary>ImpactCap을 무시하는 관통 Impact 값이다.</summary>
        public int PiercingImpactDamage { get; }

        /// <summary>ImpactCap 적용 전 실제 피해 후보값이다.</summary>
        public int RawImpactDamage { get; }

        /// <summary>0보다 크면 RawImpactDamage에 적용되는 선택적 Impact 상한이다.</summary>
        public int ImpactCap { get; }

        /// <summary>ImpactCap만 적용한 중간 Impact 값이다.</summary>
        public int CappedImpactDamage { get; }

        /// <summary>Broken Cast, Guard, BossRule 등 최종 보정 비율이다.</summary>
        public int FinalModifierPercent { get; }

        /// <summary>최종적으로 HP에 적용되는 실제 Impact 피해량이다.</summary>
        public int AppliedImpactDamage { get; }

        /// <summary>ImpactDamage 계산 결과를 생성한다.</summary>
        public ImpactDamageBreakdown(
            int baseImpact,
            int powerTierBonus,
            int marginTierBonus,
            int deviceImpactBonus,
            int trueImpactDamage,
            int piercingImpactDamage,
            int impactCap,
            int finalModifierPercent)
        {
            BaseImpact = Math.Max(0, baseImpact);
            PowerTierBonus = Math.Max(0, powerTierBonus);
            MarginTierBonus = Math.Max(0, marginTierBonus);
            DeviceImpactBonus = Math.Max(0, deviceImpactBonus);
            TrueImpactDamage = Math.Max(0, trueImpactDamage);
            PiercingImpactDamage = Math.Max(0, piercingImpactDamage);
            ImpactCap = Math.Max(0, impactCap);
            FinalModifierPercent = Math.Max(0, finalModifierPercent);

            RawImpactDamage =
                BaseImpact +
                PowerTierBonus +
                MarginTierBonus +
                DeviceImpactBonus +
                TrueImpactDamage;

            CappedImpactDamage = ImpactCap > 0 ? Math.Min(RawImpactDamage, ImpactCap) : RawImpactDamage;
            AppliedImpactDamage = Math.Max(0, CappedImpactDamage * FinalModifierPercent / 100) + PiercingImpactDamage;
        }

        /// <summary>피해가 없는 ImpactDamage 결과를 생성한다.</summary>
        public static ImpactDamageBreakdown Zero(int impactCap)
        {
            return new ImpactDamageBreakdown(0, 0, 0, 0, 0, 0, impactCap, 100);
        }
    }
}
