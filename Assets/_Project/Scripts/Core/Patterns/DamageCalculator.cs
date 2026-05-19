using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast Score와 Base Force를 기반으로 Table Rule 적용 전 피해량을 계산한다.</summary>
    public class DamageCalculator
    {
        private readonly IReadOnlyDictionary<RollPatternType, PatternDefinition> _definitions;

        /// <summary>Cast 카테고리 정의표를 사용하는 피해 계산기를 생성한다.</summary>
        public DamageCalculator(IReadOnlyDictionary<RollPatternType, PatternDefinition> definitions)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        }

        /// <summary>Cast 카테고리와 Cast Score로 Table Rule 적용 전 피해량을 계산한다.</summary>
        public int CalculateFinalDamage(RollPatternType patternType, int rawCastScore)
        {
            PatternDefinition definition = GetDefinition(patternType);

            if (patternType == RollPatternType.BrokenCast)
                return 0;

            // 기본식: (Cast Score + Flat Bonus) x Base Force + Extra Bonus
            return (rawCastScore + definition.FlatBonus) * definition.BaseForce + definition.ExtraBonus;
        }

        /// <summary>Cast 카테고리와 포함 주사위 목록으로 완성된 Cast 결과를 생성한다.</summary>
        public PatternResult CreateResult(
            RollPatternType patternType,
            IReadOnlyList<int> includedDiceValues,
            int rawCastScore)
        {
            if (includedDiceValues == null)
                throw new ArgumentNullException(nameof(includedDiceValues));

            PatternDefinition definition = GetDefinition(patternType);
            int finalDamage = CalculateFinalDamage(patternType, rawCastScore);

            return new PatternResult(
                patternType,
                includedDiceValues,
                rawCastScore,
                definition.FlatBonus,
                definition.BaseForce,
                definition.ExtraBonus,
                finalDamage);
        }

        /// <summary>Cast 카테고리에 해당하는 정의를 찾는다.</summary>
        private PatternDefinition GetDefinition(RollPatternType patternType)
        {
            if (!_definitions.TryGetValue(patternType, out PatternDefinition definition))
                throw new KeyNotFoundException($"Cast 카테고리 정의를 찾을 수 없습니다. PatternType: {patternType}");

            return definition;
        }
    }
}
