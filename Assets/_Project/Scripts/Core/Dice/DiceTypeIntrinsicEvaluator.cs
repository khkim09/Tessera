using Tessera.Data;
using UnityEngine;

namespace Tessera.Core
{
    /// <summary>DiceTypeDefinitionSO의 intrinsic 필드를 읽어 SlotPair 및 후처리 보정을 계산한다.</summary>
    public class DiceTypeIntrinsicEvaluator
    {
        /// <summary>SlotPair 한 칸에서 현재 DiceType 고유 효과의 Score/Force 보정을 계산한다.</summary>
        public DiceTypeIntrinsicResult EvaluateSlotPair(
            int slotIndex,
            int currentDiceValue,
            RollPatternType castType,
            bool diceIncluded,
            DiceTypeDefinitionSO diceTypeDefinition)
        {
            if (diceTypeDefinition == null || !diceIncluded || currentDiceValue <= 0)
                return DiceTypeIntrinsicResult.None;

            DiceIntrinsicEffectType effectType = diceTypeDefinition.IntrinsicEffectType;

            switch (effectType)
            {
                case DiceIntrinsicEffectType.AddScoreIfOdd:
                    if (currentDiceValue % 2 != 0)
                        return BuildScoreResult(diceTypeDefinition, effectType, diceTypeDefinition.IntValue, "Score");
                    break;

                case DiceIntrinsicEffectType.AddForceIfEven:
                    if (currentDiceValue % 2 == 0)
                        return BuildForceAddResult(diceTypeDefinition, effectType);
                    break;

                case DiceIntrinsicEffectType.AddScoreIfValueAtMost:
                    if (currentDiceValue <= ResolveThreshold(diceTypeDefinition, 3))
                        return BuildScoreResult(diceTypeDefinition, effectType, diceTypeDefinition.IntValue, "Score");
                    break;

                case DiceIntrinsicEffectType.AddScoreIfValueAtLeast:
                    if (currentDiceValue >= ResolveThreshold(diceTypeDefinition, 5))
                        return BuildScoreResult(diceTypeDefinition, effectType, diceTypeDefinition.IntValue, "Score");
                    break;
            }

            return DiceTypeIntrinsicResult.None;
        }

        /// <summary>Round 승리 시 사용된 DiceType 목록에서 Money 보너스를 합산한다.</summary>
        public int CalculateMoneyOnRoundWinBonus(System.Collections.Generic.IReadOnlyList<DiceTypeDefinitionSO> usedDiceTypes)
        {
            int bonus = 0;
            if (usedDiceTypes == null)
                return bonus;

            System.Collections.Generic.HashSet<DiceTypeDefinitionSO> appliedDiceTypes = new System.Collections.Generic.HashSet<DiceTypeDefinitionSO>();
            for (int i = 0; i < usedDiceTypes.Count; i++)
            {
                DiceTypeDefinitionSO diceType = usedDiceTypes[i];
                if (diceType != null && diceType.IntrinsicEffectType == DiceIntrinsicEffectType.AddMoneyOnRoundWinIfUsed && appliedDiceTypes.Add(diceType))
                    bonus += Mathf.Max(0, diceType.IntValue);
            }

            return bonus;
        }

        /// <summary>패배 피해 적용 직전 사용된 DiceType 목록에서 수신 피해 감소량을 합산한다.</summary>
        public int CalculateIncomingDamageReduction(System.Collections.Generic.IReadOnlyList<DiceTypeDefinitionSO> usedDiceTypes)
        {
            int reduction = 0;
            if (usedDiceTypes == null)
                return reduction;

            System.Collections.Generic.HashSet<DiceTypeDefinitionSO> appliedDiceTypes = new System.Collections.Generic.HashSet<DiceTypeDefinitionSO>();
            for (int i = 0; i < usedDiceTypes.Count; i++)
            {
                DiceTypeDefinitionSO diceType = usedDiceTypes[i];
                if (diceType != null && diceType.IntrinsicEffectType == DiceIntrinsicEffectType.ReduceIncomingDamageIfUsed && appliedDiceTypes.Add(diceType))
                    reduction += Mathf.Max(0, diceType.IntValue);
            }

            return reduction;
        }

        /// <summary>Score 보정 결과를 생성한다.</summary>
        private static DiceTypeIntrinsicResult BuildScoreResult(DiceTypeDefinitionSO diceType, DiceIntrinsicEffectType effectType, int scoreBonus, string label)
        {
            int resolvedScoreBonus = Mathf.Max(0, scoreBonus);
            return new DiceTypeIntrinsicResult(resolvedScoreBonus, 0f, 1f, 0, 0, effectType, $"{label}+{resolvedScoreBonus}");
        }

        /// <summary>Force 가산 보정 결과를 생성한다.</summary>
        private static DiceTypeIntrinsicResult BuildForceAddResult(DiceTypeDefinitionSO diceType, DiceIntrinsicEffectType effectType)
        {
            float forceAdd = diceType.FloatValue > 0f ? diceType.FloatValue : diceType.IntValue;
            float resolvedForceAdd = Mathf.Max(0f, forceAdd);
            return new DiceTypeIntrinsicResult(0, resolvedForceAdd, 1f, 0, 0, effectType, $"Force+{resolvedForceAdd:0.##}");
        }

        /// <summary>DiceType 조건 기준값을 SO 필드 또는 fallback으로 결정한다.</summary>
        private static int ResolveThreshold(DiceTypeDefinitionSO diceType, int fallback)
        {
            if (diceType.FloatValue > 0f)
                return Mathf.RoundToInt(diceType.FloatValue);

            return fallback;
        }
    }
}
