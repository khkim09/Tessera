using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>장착 DiceType 태그 개수에 따라 활성화되는 DiceSynergy 효과를 계산한다.</summary>
    public class DiceSynergyEvaluator
    {
        public DiceTypeIntrinsicResult EvaluateSlotPair(
            int diceValue,
            IReadOnlyList<int> currentDiceValues,
            IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes,
            IReadOnlyList<DiceSynergyRuleData> synergyRules)
        {
            int scoreBonus = 0;
            float forceAdd = 0f;
            List<string> messages = new List<string>();
            List<DiceSynergyRuleData> activeRules = CollectActiveRules(equippedDiceTypes, synergyRules);
            int oddCount = CountParity(currentDiceValues, odd: true);
            int evenCount = CountParity(currentDiceValues, odd: false);
            int lowCount = CountAtMost(currentDiceValues, 3);

            for (int i = 0; i < activeRules.Count; i++)
            {
                DiceSynergyRuleData rule = activeRules[i];
                switch (rule.EffectType)
                {
                    case DiceSynergyEffectType.AddScoreForOddDice:
                        if (diceValue % 2 != 0) { scoreBonus += rule.IntValue; messages.Add($"{rule.DisplayName} Score +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddScoreForEvenDice:
                        if (diceValue > 0 && diceValue % 2 == 0) { scoreBonus += rule.IntValue; messages.Add($"{rule.DisplayName} Score +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddForceIfOddDiceCountAtLeast:
                        if (diceValue % 2 != 0 && oddCount >= rule.RequiredCount) { forceAdd += rule.IntValue; messages.Add($"{rule.DisplayName} Force +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddForceIfEvenDiceCountAtLeast:
                        if (diceValue > 0 && diceValue % 2 == 0 && evenCount >= rule.RequiredCount) { forceAdd += rule.IntValue; messages.Add($"{rule.DisplayName} Force +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddScoreForHighDice:
                        if (diceValue >= 5) { scoreBonus += rule.IntValue; messages.Add($"{rule.DisplayName} Score +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddScoreForLowDice:
                        if (diceValue > 0 && diceValue <= 3) { scoreBonus += rule.IntValue; messages.Add($"{rule.DisplayName} Score +{rule.IntValue}"); }
                        break;
                    case DiceSynergyEffectType.AddForceIfLowDiceCountAtLeast:
                        if (diceValue > 0 && diceValue <= 3 && lowCount >= rule.RequiredCount) { forceAdd += rule.IntValue; messages.Add($"{rule.DisplayName} Force +{rule.IntValue}"); }
                        break;
                }
            }

            return new DiceTypeIntrinsicResult(
                scoreBonus,
                forceAdd,
                1f,
                0,
                0,
                DiceIntrinsicEffectType.None,
                string.Join(". ", messages));
        }

        public int CalculateMoneyOnRoundWinBonus(IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes, IReadOnlyList<DiceSynergyRuleData> synergyRules) =>
            SumActiveRuleIntValue(equippedDiceTypes, synergyRules, DiceSynergyEffectType.AddMoneyOnRoundWin);

        public int CalculateBrokenCastOverchargeBonus(IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes, IReadOnlyList<DiceSynergyRuleData> synergyRules) =>
            SumActiveRuleIntValue(equippedDiceTypes, synergyRules, DiceSynergyEffectType.AddOverchargeOnBrokenCast);

        public int CalculateBrokenCastIncomingDamageReduction(IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes, IReadOnlyList<DiceSynergyRuleData> synergyRules) =>
            SumActiveRuleIntValue(equippedDiceTypes, synergyRules, DiceSynergyEffectType.IncreaseBrokenCastDamageReduction);

        private static int SumActiveRuleIntValue(IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes, IReadOnlyList<DiceSynergyRuleData> synergyRules, DiceSynergyEffectType effectType)
        {
            int total = 0;
            List<DiceSynergyRuleData> activeRules = CollectActiveRules(equippedDiceTypes, synergyRules);
            for (int i = 0; i < activeRules.Count; i++)
                if (activeRules[i].EffectType == effectType)
                    total += activeRules[i].IntValue;
            return total;
        }

        private static List<DiceSynergyRuleData> CollectActiveRules(IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes, IReadOnlyList<DiceSynergyRuleData> synergyRules)
        {
            List<DiceSynergyRuleData> result = new List<DiceSynergyRuleData>();
            if (equippedDiceTypes == null || synergyRules == null) return result;
            for (int i = 0; i < synergyRules.Count; i++)
            {
                DiceSynergyRuleData rule = synergyRules[i];
                if (rule.IsValid && CountTag(equippedDiceTypes, rule.RequiredTagValue) >= rule.RequiredCount)
                    result.Add(rule);
            }
            return result;
        }

        private static int CountTag(IReadOnlyList<DiceTypeIntrinsicData> diceTypes, int tag)
        {
            int count = 0;
            for (int i = 0; i < diceTypes.Count; i++) if (diceTypes[i].SynergyTagValue == tag) count++;
            return count;
        }
        private static int CountParity(IReadOnlyList<int> values, bool odd)
        {
            int count = 0; if (values == null) return 0;
            for (int i = 0; i < values.Count; i++) if (values[i] > 0 && (values[i] % 2 != 0) == odd) count++;
            return count;
        }
        private static int CountAtMost(IReadOnlyList<int> values, int max)
        {
            int count = 0; if (values == null) return 0;
            for (int i = 0; i < values.Count; i++) if (values[i] > 0 && values[i] <= max) count++;
            return count;
        }
    }
}
