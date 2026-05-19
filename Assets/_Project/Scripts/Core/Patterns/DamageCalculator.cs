using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Cast 기본 점수와 보정값을 기반으로 최종 피해량을 계산한다.</summary>
    public class DamageCalculator
    {
        private readonly IReadOnlyDictionary<RollPatternType, PatternDefinition> _definitions;

        /// <summary>Cast 카테고리 보정표를 사용하는 피해 계산기를 생성한다.</summary>
        public DamageCalculator(IReadOnlyDictionary<RollPatternType, PatternDefinition> definitions)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        }

        /// <summary>Cast 카테고리와 Raw Cast Score로 최종 피해량을 계산한다.</summary>
        public int CalculateFinalDamage(RollPatternType patternType, int rawCastScore)
        {
            if (!_definitions.TryGetValue(patternType, out PatternDefinition definition))
                throw new KeyNotFoundException($"Cast 카테고리 보정 정의를 찾을 수 없습니다. PatternType: {patternType}");

            if (patternType == RollPatternType.BrokenCast)
                return 0;

            return (rawCastScore + definition.FlatBonus) * definition.DamageMultiplier + definition.ExtraBonus;
        }

        /// <summary>Cast 카테고리와 포함 주사위 목록으로 완성된 Cast 결과를 생성한다.</summary>
        public PatternResult CreateResult(
            RollPatternType patternType,
            IReadOnlyList<int> includedDiceValues,
            int rawCastScore)
        {
            if (includedDiceValues == null)
                throw new ArgumentNullException(nameof(includedDiceValues));

            if (!_definitions.TryGetValue(patternType, out PatternDefinition definition))
                throw new KeyNotFoundException($"Cast 카테고리 보정 정의를 찾을 수 없습니다. PatternType: {patternType}");

            int finalDamage = CalculateFinalDamage(patternType, rawCastScore);

            return new PatternResult(
                patternType,
                includedDiceValues,
                rawCastScore,
                definition.FlatBonus,
                definition.DamageMultiplier,
                definition.ExtraBonus,
                finalDamage);
        }
    }
}
