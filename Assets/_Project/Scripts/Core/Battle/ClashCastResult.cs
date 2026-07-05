using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>Clash 판정을 위해 한 주체가 확정한 Cast 계산 결과다.</summary>
    public class ClashCastResult
    {
        private readonly List<int> diceValues;
        private readonly List<int> lockSlotDiceIndexes;
        private readonly List<SlotPairDeviceDefinition> deviceDefinitions;

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

        /// <summary>Clash Cast 계산에 사용된 SlotPair Device 정의 목록이다.</summary>
        public IReadOnlyList<SlotPairDeviceDefinition> DeviceDefinitions => deviceDefinitions;

        /// <summary>Clash 승패 비교에 사용할 CastPower 값이다.</summary>
        public int CastPower { get; }

        /// <summary>UI/AI 후보 평가용 예상 ImpactDamage 값이다.</summary>
        public int ExpectedImpactDamage { get; }

        /// <summary>UI/AI 후보 평가용 예상 ImpactDamage 계산 내역이다.</summary>
        public ImpactDamageBreakdown ExpectedImpactBreakdown { get; }

        /// <summary>Broken Cast 여부.</summary>
        public bool IsBrokenCast { get; }

        /// <summary>Clash Cast 결과를 생성한다.</summary>
        public ClashCastResult(
            ClashParticipantType owner,
            PatternResult patternResult,
            SlotPairDamagePreview slotPairDamagePreview,
            TableRuleEvaluationResult tableRuleEvaluationResult,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            RoundRuleContext ruleContext,
            ImpactDamageCalculator impactDamageCalculator)
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

            this.deviceDefinitions = deviceDefinitions != null
                ? new List<SlotPairDeviceDefinition>(deviceDefinitions)
                : new List<SlotPairDeviceDefinition>();

            PatternType = patternResult.PatternType;
            CastPower = Math.Max(0, tableRuleEvaluationResult.ModifiedCastPower);
            IsBrokenCast = PatternType == RollPatternType.BrokenCast;

            ExpectedImpactBreakdown = impactDamageCalculator != null && ruleContext != null
                ? impactDamageCalculator.CalculateExpected(ruleContext, this, 0)
                : ImpactDamageBreakdown.Zero(ruleContext != null ? ruleContext.ImpactCap : 0);

            ExpectedImpactDamage = ExpectedImpactBreakdown.AppliedImpactDamage;
        }
    }
}
