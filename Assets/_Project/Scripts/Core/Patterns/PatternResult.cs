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

        /// <summary>Force 계산 이후 더해지는 고정 Power 보너스다.</summary>
        public int TruePower { get; }

        /// <summary>Clash 승리 시 RawImpactDamage에 들어가는 Cast 등급 기본값이다.</summary>
        public int BaseImpact { get; }

        /// <summary>Clash 승패 비교에 사용하는 CastPower 값이다.</summary>
        public int CastPower { get; }

        /// <summary>Cast 카테고리 판정 결과를 생성한다.</summary>
        public PatternResult(
            RollPatternType patternType,
            IReadOnlyList<int> includedDiceValues,
            int rawCastScore,
            int flatBonus,
            int baseForce,
            int truePower,
            int baseImpact,
            int castPower)
        {
            if (includedDiceValues == null)
                throw new ArgumentNullException(nameof(includedDiceValues));

            PatternType = patternType;
            _includedDiceValues = new List<int>(includedDiceValues);
            IncludedDiceSum = Sum(includedDiceValues);
            RawCastScore = rawCastScore;
            FlatBonus = flatBonus;
            BaseForce = baseForce;
            TruePower = truePower;
            BaseImpact = baseImpact;
            CastPower = castPower;
        }

        /// <summary>디버그용 Cast 결과 문자열을 반환한다.</summary>
        public override string ToString()
        {
            return $"{PatternType} | Score={RawCastScore}, Flat={FlatBonus}, Force={BaseForce}, TruePower={TruePower}, BaseImpact={BaseImpact}, CastPower={CastPower}";
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
