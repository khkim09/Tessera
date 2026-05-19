using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 카테고리의 피해 보정값과 표시 정보를 정의한다.</summary>
    public class PatternDefinition
    {
        /// <summary>이 정의가 가리키는 Cast 카테고리.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>Raw Cast Score에 더해지는 고정 보너스.</summary>
        public int FlatBonus { get; }

        /// <summary>Raw Cast Score와 Flat Bonus 합에 곱해지는 피해 배율.</summary>
        public int DamageMultiplier { get; }

        /// <summary>배율 계산 이후 추가되는 최종 고정 보너스.</summary>
        public int ExtraBonus { get; }

        /// <summary>Cast 카테고리 피해 보정 정의를 생성한다.</summary>
        public PatternDefinition(RollPatternType patternType, int flatBonus, int damageMultiplier, int extraBonus = 0)
        {
            if (damageMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(damageMultiplier), "피해 배율은 음수가 될 수 없습니다.");

            PatternType = patternType;
            FlatBonus = flatBonus;
            DamageMultiplier = damageMultiplier;
            ExtraBonus = extraBonus;
        }

        /// <summary>초기 Core 테스트용 Cast 카테고리 보정값을 생성한다.</summary>
        public static IReadOnlyDictionary<RollPatternType, PatternDefinition> CreateDefaultDefinitions()
        {
            return new Dictionary<RollPatternType, PatternDefinition>
            {
                { RollPatternType.Aces, new PatternDefinition(RollPatternType.Aces, 0, 1) },
                { RollPatternType.Twos, new PatternDefinition(RollPatternType.Twos, 0, 1) },
                { RollPatternType.Threes, new PatternDefinition(RollPatternType.Threes, 0, 1) },
                { RollPatternType.Fours, new PatternDefinition(RollPatternType.Fours, 0, 1) },
                { RollPatternType.Fives, new PatternDefinition(RollPatternType.Fives, 0, 1) },
                { RollPatternType.Sixes, new PatternDefinition(RollPatternType.Sixes, 0, 1) },

                { RollPatternType.ThreeOfAKind, new PatternDefinition(RollPatternType.ThreeOfAKind, 0, 1) },
                { RollPatternType.FourOfAKind, new PatternDefinition(RollPatternType.FourOfAKind, 0, 1) },
                { RollPatternType.FullHouse, new PatternDefinition(RollPatternType.FullHouse, 0, 1) },
                { RollPatternType.SmallStraight, new PatternDefinition(RollPatternType.SmallStraight, 0, 1) },
                { RollPatternType.LargeStraight, new PatternDefinition(RollPatternType.LargeStraight, 0, 1) },
                { RollPatternType.Chance, new PatternDefinition(RollPatternType.Chance, 0, 1) },
                { RollPatternType.Tessera, new PatternDefinition(RollPatternType.Tessera, 0, 1) },

                { RollPatternType.BrokenCast, new PatternDefinition(RollPatternType.BrokenCast, 0, 0) }
            };
        }
    }
}
