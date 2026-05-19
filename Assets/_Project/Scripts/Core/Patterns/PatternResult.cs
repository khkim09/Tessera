using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>하나의 Cast 판정 결과와 전투 계산용 점수 정보를 담는다.</summary>
    public class PatternResult
    {
        private readonly List<int> _includedDiceValues;

        /// <summary>판정된 Cast 카테고리.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>Cast 점수 계산에 포함된 주사위 눈금 목록.</summary>
        public IReadOnlyList<int> IncludedDiceValues => _includedDiceValues;

        /// <summary>포함 주사위 눈금 합.</summary>
        public int IncludedDiceSum { get; }

        /// <summary>야추식 기본 Cast Score.</summary>
        public int RawCastScore { get; }

        /// <summary>야추식 기본 Cast Score이며 RawCastScore와 동일하다.</summary>
        public int CastScore => RawCastScore;

        /// <summary>Cast Score에 더해지는 고정 보너스.</summary>
        public int FlatBonus { get; }

        /// <summary>Cast Score에 적용되는 기본 Force 값.</summary>
        public int BaseForce { get; }

        /// <summary>기존 코드 호환용 피해 배율 값이며 BaseForce와 동일하다.</summary>
        public int DamageMultiplier => BaseForce;

        /// <summary>Force 계산 이후 더해지는 고정 피해 보너스.</summary>
        public int ExtraBonus { get; }

        /// <summary>Table Rule 적용 전 피해량.</summary>
        public int FinalDamage { get; }

        /// <summary>Cast 카테고리 판정 결과를 생성한다.</summary>
        public PatternResult(
            RollPatternType patternType,
            IReadOnlyList<int> includedDiceValues,
            int rawCastScore,
            int flatBonus,
            int baseForce,
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
            BaseForce = baseForce;
            ExtraBonus = extraBonus;
            FinalDamage = finalDamage;
        }

        /// <summary>디버그용 Cast 결과 문자열을 반환한다.</summary>
        public override string ToString()
        {
            return $"{PatternType} | Score={RawCastScore}, Flat={FlatBonus}, Force={BaseForce}, Extra={ExtraBonus}, Damage={FinalDamage}";
        }

        /// <summary>정수 목록의 합계를 계산한다.</summary>
        private static int Sum(IReadOnlyList<int> values)
        {
            int sum = 0;

            for (int i = 0; i < values.Count; i++)
                sum += values[i];

            return sum;
        }
    }
}
