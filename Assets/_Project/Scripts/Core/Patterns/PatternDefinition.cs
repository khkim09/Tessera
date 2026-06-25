using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 카테고리의 Cast Score 보정값과 기본 Force를 정의한다.</summary>
    public class PatternDefinition
    {
        /// <summary>이 정의가 가리키는 Cast 카테고리.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>Cast Score에 더해지는 고정 보너스.</summary>
        public int FlatBonus { get; }

        /// <summary>Cast Score에 적용되는 기본 Force 값.</summary>
        public int BaseForce { get; }

        /// <summary>Force 계산 이후 더해지는 고정 Power 보너스다.</summary>
        public int TruePower { get; }

        /// <summary>Clash 승리 시 RawImpactDamage에 들어가는 Cast 등급 기본값이다.</summary>
        public int BaseImpact { get; }

        /// <summary>Cast 카테고리 계산 정의를 생성한다.</summary>
        public PatternDefinition(
            RollPatternType patternType,
            int flatBonus,
            int baseForce,
            int baseImpact,
            int truePower = 0)
        {
            if (baseForce < 0)
                throw new ArgumentOutOfRangeException(nameof(baseForce), "Base Force는 음수가 될 수 없습니다.");

            if (baseImpact < 0)
                throw new ArgumentOutOfRangeException(nameof(baseImpact), "Base Impact는 음수가 될 수 없습니다.");

            PatternType = patternType;
            FlatBonus = flatBonus;
            BaseForce = baseForce;
            BaseImpact = baseImpact;
            TruePower = truePower;
        }

        /// <summary>전투 계산 규칙의 기본 Cast 정의표를 생성</summary>
        public static IReadOnlyDictionary<RollPatternType, PatternDefinition> CreateDefaultDefinitions()
        {
            return new Dictionary<RollPatternType, PatternDefinition>
            {
                // 숫자 Cast는 기본 점수가 낮기 때문에 Force x2로 최소 전투 가치를 보장한다.
                { RollPatternType.Aces, new PatternDefinition(RollPatternType.Aces, 0, 2, 3) },
                { RollPatternType.Twos, new PatternDefinition(RollPatternType.Twos, 0, 2, 3) },
                { RollPatternType.Threes, new PatternDefinition(RollPatternType.Threes, 0, 2, 3) },
                { RollPatternType.Fours, new PatternDefinition(RollPatternType.Fours, 0, 2, 4) },
                { RollPatternType.Fives, new PatternDefinition(RollPatternType.Fives, 0, 2, 4) },
                { RollPatternType.Sixes, new PatternDefinition(RollPatternType.Sixes, 0, 2, 4) },

                // 조합 Cast는 제작 난이도와 폭발력을 반영해 Force를 차등 적용한다.
                { RollPatternType.ThreeOfAKind, new PatternDefinition(RollPatternType.ThreeOfAKind, 0, 2, 6) },
                { RollPatternType.FourOfAKind, new PatternDefinition(RollPatternType.FourOfAKind, 0, 3, 10) },
                { RollPatternType.FullHouse, new PatternDefinition(RollPatternType.FullHouse, 0, 3, 8) },
                { RollPatternType.SmallStraight, new PatternDefinition(RollPatternType.SmallStraight, 0, 3, 7) },
                { RollPatternType.LargeStraight, new PatternDefinition(RollPatternType.LargeStraight, 0, 4, 11) },
                { RollPatternType.Chance, new PatternDefinition(RollPatternType.Chance, 0, 1, 5) },
                { RollPatternType.Tessera, new PatternDefinition(RollPatternType.Tessera, 0, 5, 14) },

                // Broken Cast는 피해 0이며 Overcharge/리롤 보상용 Cast로 유지한다.
                { RollPatternType.BrokenCast, new PatternDefinition(RollPatternType.BrokenCast, 0, 0, 0) }
            };
        }
    }
}
