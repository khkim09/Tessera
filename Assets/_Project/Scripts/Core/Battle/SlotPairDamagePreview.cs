using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 제출 전후에 사용할 SlotPair 기반 피해 계산 결과를 담는다.</summary>
    public class SlotPairDamagePreview
    {
        private readonly List<SlotPairDamageStep> _steps;

        /// <summary>계산 대상 Cast 카테고리.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>야추식 기본 Cast Score.</summary>
        public int BaseCastScore { get; }

        /// <summary>FlatBonus 적용 후 초기 Score.</summary>
        public int InitialScore { get; }

        /// <summary>Cast 자체가 가진 기본 Force.</summary>
        public float BaseForce { get; }

        /// <summary>SlotPair 계산 완료 후 Score.</summary>
        public int FinalScore { get; }

        /// <summary>SlotPair 계산 완료 후 Force.</summary>
        public float FinalForce { get; }

        /// <summary>Table Rule 적용 전 피해.</summary>
        public int DamageBeforeTableRules { get; }

        /// <summary>SlotPair별 계산 단계 목록.</summary>
        public IReadOnlyList<SlotPairDamageStep> Steps => _steps;

        /// <summary>SlotPair 피해 미리보기 결과를 생성한다.</summary>
        public SlotPairDamagePreview(
            RollPatternType patternType,
            int baseCastScore,
            int initialScore,
            float baseForce,
            int finalScore,
            float finalForce,
            int damageBeforeTableRules,
            IReadOnlyList<SlotPairDamageStep> steps)
        {
            PatternType = patternType;
            BaseCastScore = baseCastScore;
            InitialScore = initialScore;
            BaseForce = baseForce;
            FinalScore = finalScore;
            FinalForce = finalForce;
            DamageBeforeTableRules = damageBeforeTableRules;
            _steps = steps != null ? new List<SlotPairDamageStep>(steps) : new List<SlotPairDamageStep>();
        }

        /// <summary>표시용 Force 문자열을 반환한다.</summary>
        public string FormatFinalForce()
        {
            if (Math.Abs(FinalForce - (int)FinalForce) < 0.001f)
                return ((int)FinalForce).ToString();

            return FinalForce.ToString("0.##");
        }
    }
}
