using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 제출 전후에 사용할 SlotPair 기반 CastPower 계산 결과를 담는다.</summary>
    public class SlotPairDamagePreview
    {
        private readonly List<SlotPairDamageStep> steps;

        /// <summary>계산 대상 Cast 타입이다.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>Device 적용 전 원본 Cast Score 값이다.</summary>
        public int BaseCastScore { get; }

        /// <summary>FlatBonus 적용 직후 Score 값이다.</summary>
        public int InitialScore { get; }

        /// <summary>Device 적용 전 기본 Force 값이다.</summary>
        public float BaseForce { get; }

        /// <summary>SlotPair 계산 후 최종 Score 값이다.</summary>
        public int FinalScore { get; }

        /// <summary>SlotPair 계산 후 최종 Force 값이다.</summary>
        public float FinalForce { get; }

        /// <summary>SlotPair 계산 후 누적된 고정 Power 값이다.</summary>
        public int FinalTruePower { get; }

        /// <summary>TableRule 적용 전 CastPower 값이다.</summary>
        public int CastPowerBeforeTableRules { get; }

        /// <summary>RawImpactDamage에 더해질 Device Impact 보너스다.</summary>
        public int DeviceImpactBonus { get; }

        /// <summary>RawImpactDamage에 더해질 고정 ImpactDamage 값이다.</summary>
        public int TrueImpactDamage { get; }

        /// <summary>SlotPair 단계별 계산 결과 목록이다.</summary>
        public IReadOnlyList<SlotPairDamageStep> Steps => steps;

        /// <summary>SlotPair CastPower 미리보기 결과를 생성한다.</summary>
        public SlotPairDamagePreview(
            RollPatternType patternType,
            int baseCastScore,
            int initialScore,
            float baseForce,
            int finalScore,
            float finalForce,
            int finalTruePower,
            int castPowerBeforeTableRules,
            int deviceImpactBonus,
            int trueImpactDamage,
            IReadOnlyList<SlotPairDamageStep> steps)
        {
            PatternType = patternType;
            BaseCastScore = baseCastScore;
            InitialScore = initialScore;
            BaseForce = baseForce;
            FinalScore = finalScore;
            FinalForce = finalForce;
            FinalTruePower = finalTruePower;
            CastPowerBeforeTableRules = Math.Max(0, castPowerBeforeTableRules);
            DeviceImpactBonus = Math.Max(0, deviceImpactBonus);
            TrueImpactDamage = Math.Max(0, trueImpactDamage);
            this.steps = steps != null ? new List<SlotPairDamageStep>(steps) : new List<SlotPairDamageStep>();
        }

        /// <summary>최종 Force 값을 UI 표시용 문자열로 변환한다.</summary>
        public string FormatFinalForce()
        {
            if (Math.Abs(FinalForce - (int)FinalForce) < 0.001f)
                return ((int)FinalForce).ToString();

            return FinalForce.ToString("0.##");
        }
    }
}
