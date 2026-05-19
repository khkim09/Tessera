using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>하나의 Cast 카테고리 판정 결과와 최종 피해량을 담는다.</summary>
    public class PatternResult
    {
        private readonly List<int> _includedDiceValues;

        /// <summary>판정된 Cast 카테고리.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>Cast 점수 계산에 포함된 주사위 눈금 목록.</summary>
        public IReadOnlyList<int> IncludedDiceValues => _includedDiceValues;

        /// <summary>포함 주사위 눈금 합.</summary>
        public int IncludedDiceSum { get; }

        /// <summary>야추식 기본 Cast 점수.</summary>
        public int RawCastScore { get; }

        /// <summary>Raw Cast Score에 더해지는 고정 보너스.</summary>
        public int FlatBonus { get; }

        /// <summary>Raw Cast Score와 Flat Bonus 합에 곱해지는 피해 배율.</summary>
        public int DamageMultiplier { get; }

        /// <summary>배율 계산 이후 더해지는 최종 보너스.</summary>
        public int ExtraBonus { get; }

        /// <summary>최종 피해량.</summary>
        public int FinalDamage { get; }

        /// <summary>Cast 카테고리 판정 결과를 생성한다.</summary>
        public PatternResult(
            RollPatternType patternType,
            IReadOnlyList<int> includedDiceValues,
            int rawCastScore,
            int flatBonus,
            int damageMultiplier,
            int extraBonus,
            int finalDamage)
        {
            if (includedDiceValues == null)
                throw new ArgumentNullException(nameof(includedDiceValues));

            PatternType = patternType;
            _includedDiceValues = new List<int>(includedDiceValues);
            IncludedDiceSum = Sum(includedDiceValues);
            RawCastScore = rawCastScore;
            FlatBonus = flatBonus;
            DamageMultiplier = damageMultiplier;
            ExtraBonus = extraBonus;
            FinalDamage = finalDamage;
        }

        /// <summary>디버그용 Cast 결과 문자열을 반환한다.</summary>
        public override string ToString()
        {
            return $"{PatternType} | Raw={RawCastScore}, Flat={FlatBonus}, Mult={DamageMultiplier}, Extra={ExtraBonus}, Final={FinalDamage}";
        }

        private static int Sum(IReadOnlyList<int> values)
        {
            int sum = 0;

            for (int i = 0; i < values.Count; i++)
                sum += values[i];

            return sum;
        }
    }
}
