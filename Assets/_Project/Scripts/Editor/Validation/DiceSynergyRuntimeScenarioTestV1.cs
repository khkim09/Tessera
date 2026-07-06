#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>DiceSynergy 런타임 계산 연결을 검증한다.</summary>
    public static class DiceSynergyRuntimeScenarioTestV1
    {
        [MenuItem("Tools/Tessera/Validation/Run DiceSynergy Runtime Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;
            ValidateSlotPairScoreAndForce(ref failures);
            ValidatePostProcessHooks(ref failures);
            if (failures > 0)
            {
                Debug.LogError($"[DiceSynergyRuntimeScenarioTest] FAIL Count={failures}");
                return;
            }
            Debug.Log("[DiceSynergyRuntimeScenarioTest] PASS All DiceSynergy runtime scenario checks v1");
        }

        private static void ValidateSlotPairScoreAndForce(ref int failures)
        {
            PatternResult pattern = new PatternResult(RollPatternType.Chance, new List<int>{1,3,5,5,2}, 16, 0, 1, 0, 0, 15);
            SlotPairDamagePreview preview = new SlotPairDamageCalculator().Calculate(
                pattern,
                new List<int>{1,3,5,5,2},
                new List<int>{0,1,2,3,4},
                new List<SlotPairDeviceDefinition>{null,null,null,null,null},
                new SlotPairCalculationContext(0, CreateDiceTypes(tag: 10, count: 5), new List<DiceSynergyRuleData>
                {
                    new DiceSynergyRuleData("Red 2", 10, 2, DiceSynergyEffectType.AddScoreForOddDice, 2, 0f),
                    new DiceSynergyRuleData("Red 4", 10, 4, DiceSynergyEffectType.AddForceIfOddDiceCountAtLeast, 1, 0f)
                }));

            if (preview.FinalScore != 24 || Mathf.Abs(preview.FinalForce - 5f) > 0.001f)
                Fail(ref failures, "SlotPairScoreAndForce", "FinalScore=24 FinalForce=5", $"FinalScore={preview.FinalScore} FinalForce={preview.FinalForce}");
            else
                Debug.Log("[DiceSynergyRuntimeScenarioTest] PASS SlotPairScoreAndForce");
        }

        private static void ValidatePostProcessHooks(ref int failures)
        {
            DiceSynergyEvaluator evaluator = new DiceSynergyEvaluator();
            List<DiceTypeIntrinsicData> diceTypes = CreateDiceTypes(tag: 70, count: 5);
            List<DiceSynergyRuleData> rules = new List<DiceSynergyRuleData>
            {
                new DiceSynergyRuleData("Void 2", 70, 2, DiceSynergyEffectType.AddOverchargeOnBrokenCast, 1, 0f),
                new DiceSynergyRuleData("Void 4", 70, 4, DiceSynergyEffectType.IncreaseBrokenCastDamageReduction, 2, 0f)
            };

            int overcharge = evaluator.CalculateBrokenCastOverchargeBonus(diceTypes, rules);
            int reduction = evaluator.CalculateBrokenCastIncomingDamageReduction(diceTypes, rules);
            if (overcharge != 1 || reduction != 2)
                Fail(ref failures, "PostProcessHooks", "Overcharge=1 Reduction=2", $"Overcharge={overcharge} Reduction={reduction}");
            else
                Debug.Log("[DiceSynergyRuntimeScenarioTest] PASS PostProcessHooks");
        }

        private static List<DiceTypeIntrinsicData> CreateDiceTypes(int tag, int count)
        {
            List<DiceTypeIntrinsicData> result = new List<DiceTypeIntrinsicData>();
            for (int i = 0; i < count; i++)
                result.Add(new DiceTypeIntrinsicData("Test", DiceIntrinsicEffectType.None, 0, 0f, tag));
            return result;
        }

        private static void Fail(ref int failures, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[DiceSynergyRuntimeScenarioTest] FAIL Location={location} Expected={expected} Actual={actual}");
        }
    }
}
#endif
