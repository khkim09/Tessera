using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 제출 전후에 사용할 SlotPair 기반 피해 계산 결과를 담는다.</summary>
    public class SlotPairDamagePreview
    {
        private readonly List<SlotPairDamageStep> _steps;

        public RollPatternType PatternType { get; }

        public int BaseCastScore { get; }
        public int InitialScore { get; }
        public float BaseForce { get; }

        public int FinalScore { get; }
        public float FinalForce { get; }

        public int FinalTrueDamage { get; }

        public int DamageBeforeTableRules { get; }

        public IReadOnlyList<SlotPairDamageStep> Steps => _steps;

        public SlotPairDamagePreview(
            RollPatternType patternType,
            int baseCastScore,
            int initialScore,
            float baseForce,
            int finalScore,
            float finalForce,
            int finalTrueDamage,
            int damageBeforeTableRules,
            IReadOnlyList<SlotPairDamageStep> steps)
        {
            PatternType = patternType;
            BaseCastScore = baseCastScore;
            InitialScore = initialScore;
            BaseForce = baseForce;
            FinalScore = finalScore;
            FinalForce = finalForce;
            FinalTrueDamage = finalTrueDamage;
            DamageBeforeTableRules = damageBeforeTableRules;
            _steps = steps != null ? new List<SlotPairDamageStep>(steps) : new List<SlotPairDamageStep>();
        }

        public string FormatFinalForce()
        {
            if (Math.Abs(FinalForce - (int)FinalForce) < 0.001f)
                return ((int)FinalForce).ToString();

            return FinalForce.ToString("0.##");
        }
    }
}
