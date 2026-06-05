using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Clash 판정을 위해 한 주체가 확정한 Cast 계산 결과다.</summary>
    public class ClashCastResult
    {
        private readonly List<int> diceValues;
        private readonly List<int> lockSlotDiceIndexes;

        /// <summary>Cast 계산 주체.</summary>
        public ClashParticipantType Owner { get; }

        /// <summary>제출한 Cast 타입.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>야추식 Cast 판정 결과.</summary>
        public PatternResult PatternResult { get; }

        /// <summary>SlotPair 계산 결과.</summary>
        public SlotPairDamagePreview SlotPairDamagePreview { get; }

        /// <summary>TableRule 적용 결과.</summary>
        public TableRuleEvaluationResult TableRuleEvaluationResult { get; }

        /// <summary>계산에 사용된 주사위 값 목록.</summary>
        public IReadOnlyList<int> DiceValues => diceValues;

        /// <summary>SlotPair 슬롯별 DiceIndex 매핑.</summary>
        public IReadOnlyList<int> LockSlotDiceIndexes => lockSlotDiceIndexes;

        /// <summary>Clash 비교에 사용할 최종 피해값.</summary>
        public int FinalDamage { get; }

        /// <summary>Broken Cast 여부.</summary>
        public bool IsBrokenCast { get; }

        /// <summary>Clash Cast 결과를 생성한다.</summary>
        public ClashCastResult(
            ClashParticipantType owner,
            PatternResult patternResult,
            SlotPairDamagePreview slotPairDamagePreview,
            TableRuleEvaluationResult tableRuleEvaluationResult,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<int> lockSlotDiceIndexes)
        {
            Owner = owner;
            PatternResult = patternResult ?? throw new ArgumentNullException(nameof(patternResult));
            SlotPairDamagePreview = slotPairDamagePreview ?? throw new ArgumentNullException(nameof(slotPairDamagePreview));
            TableRuleEvaluationResult = tableRuleEvaluationResult ?? throw new ArgumentNullException(nameof(tableRuleEvaluationResult));

            this.diceValues = diceValues != null
                ? new List<int>(diceValues)
                : new List<int>();

            this.lockSlotDiceIndexes = lockSlotDiceIndexes != null
                ? new List<int>(lockSlotDiceIndexes)
                : new List<int>();

            PatternType = patternResult.PatternType;
            FinalDamage = Math.Max(0, tableRuleEvaluationResult.ModifiedDamage);
            IsBrokenCast = PatternType == RollPatternType.BrokenCast;
        }
    }
}
