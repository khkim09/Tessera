using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>현재 RoundState를 기반으로 Cast Board 표시 데이터를 생성한다.</summary>
    public class CastBoardModelBuilder
    {
        private readonly PatternEvaluator _patternEvaluator;

        /// <summary>Cast 판정기를 사용하는 Cast Board 빌더를 생성한다.</summary>
        public CastBoardModelBuilder(PatternEvaluator patternEvaluator)
        {
            _patternEvaluator = patternEvaluator ?? throw new ArgumentNullException(nameof(patternEvaluator));
        }

        /// <summary>기본 Cast 판정기를 사용하는 Cast Board 빌더를 생성한다.</summary>
        public static CastBoardModelBuilder CreateDefault()
        {
            return new CastBoardModelBuilder(PatternEvaluator.CreateDefault());
        }

        /// <summary>현재 RoundState로 Cast Board 표시 데이터를 생성한다.</summary>
        public CastBoardViewModel Build(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            List<CastBoardEntryModel> entries = new List<CastBoardEntryModel>();
            List<int> diceValues = roundState.GetCurrentDiceValues();
            RollPatternType[] orderedPatternTypes = CastBoardCatalog.GetOrderedPatternTypes();

            RollPatternType recommendedPatternType = RollPatternType.None;
            int recommendedDamage = int.MinValue;

            for (int i = 0; i < orderedPatternTypes.Length; i++)
            {
                CastBoardEntryModel entry = BuildEntry(roundState, diceValues, orderedPatternTypes[i], false);
                entries.Add(entry);

                if (entry.Status != CastBoardEntryStatus.Available)
                    continue;

                if (entry.PatternType == RollPatternType.BrokenCast)
                    continue;

                if (entry.DamageAfterTableRules <= recommendedDamage)
                    continue;

                recommendedPatternType = entry.PatternType;
                recommendedDamage = entry.DamageAfterTableRules;
            }

            if (recommendedDamage == int.MinValue)
                recommendedDamage = 0;

            List<CastBoardEntryModel> finalEntries = new List<CastBoardEntryModel>(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                CastBoardEntryModel entry = entries[i];
                bool isRecommended = entry.PatternType == recommendedPatternType && entry.Status == CastBoardEntryStatus.Available;
                finalEntries.Add(CloneWithRecommended(entry, isRecommended));
            }

            return new CastBoardViewModel(finalEntries, recommendedPatternType, recommendedDamage);
        }

        private CastBoardEntryModel BuildEntry(
            RoundState roundState,
            IReadOnlyList<int> diceValues,
            RollPatternType patternType,
            bool isRecommended)
        {
            int useCount = roundState.GetPatternUseCount(patternType);
            int maxUseCount = patternType == RollPatternType.BrokenCast
                ? roundState.RuleContext.MaxBrokenCastUsesPerRound
                : roundState.RuleContext.MaxUsesPerCastPerRound;

            bool isUsedThisRound = useCount > 0;
            bool isUsageAllowed = useCount < maxUseCount;

            bool isConditionMet = _patternEvaluator.TryEvaluateSpecificPattern(diceValues, patternType, out PatternResult patternResult);

            if (!isConditionMet)
            {
                return new CastBoardEntryModel(
                    patternType,
                    CastBoardCatalog.GetDisplayName(patternType),
                    CastBoardEntryStatus.ConditionNotMet,
                    false,
                    isUsageAllowed,
                    false,
                    isUsedThisRound,
                    useCount,
                    maxUseCount,
                    0,
                    0,
                    0,
                    0,
                    isRecommended,
                    "Cast condition is not met.");
            }

            TableRuleEvaluationResult tableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, patternResult);

            if (!isUsageAllowed)
            {
                return new CastBoardEntryModel(
                    patternType,
                    CastBoardCatalog.GetDisplayName(patternType),
                    CastBoardEntryStatus.Used,
                    true,
                    false,
                    tableRuleResult.IsCastBlocked,
                    isUsedThisRound,
                    useCount,
                    maxUseCount,
                    patternResult.RawCastScore,
                    patternResult.IncludedDiceSum,
                    tableRuleResult.OriginalDamage,
                    tableRuleResult.ModifiedDamage,
                    isRecommended,
                    "Already used in this Round.");
            }

            if (tableRuleResult.IsCastBlocked)
            {
                return new CastBoardEntryModel(
                    patternType,
                    CastBoardCatalog.GetDisplayName(patternType),
                    CastBoardEntryStatus.BlockedByTableRule,
                    true,
                    true,
                    true,
                    isUsedThisRound,
                    useCount,
                    maxUseCount,
                    patternResult.RawCastScore,
                    patternResult.IncludedDiceSum,
                    tableRuleResult.OriginalDamage,
                    tableRuleResult.ModifiedDamage,
                    isRecommended,
                    tableRuleResult.Message);
            }

            return new CastBoardEntryModel(
                patternType,
                CastBoardCatalog.GetDisplayName(patternType),
                CastBoardEntryStatus.Available,
                true,
                true,
                false,
                isUsedThisRound,
                useCount,
                maxUseCount,
                patternResult.RawCastScore,
                patternResult.IncludedDiceSum,
                tableRuleResult.OriginalDamage,
                tableRuleResult.ModifiedDamage,
                isRecommended,
                tableRuleResult.Message);
        }

        private static CastBoardEntryModel CloneWithRecommended(CastBoardEntryModel source, bool isRecommended)
        {
            return new CastBoardEntryModel(
                source.PatternType,
                source.DisplayName,
                source.Status,
                source.IsConditionMet,
                source.IsUsageAllowed,
                source.IsBlockedByTableRule,
                source.IsUsedThisRound,
                source.UseCount,
                source.MaxUseCount,
                source.RawCastScore,
                source.IncludedDiceSum,
                source.DamageBeforeTableRules,
                source.DamageAfterTableRules,
                isRecommended,
                source.Message);
        }
    }
}
