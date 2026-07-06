#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>RunSession에 장착된 DiceFaceUpgrade가 Core Round Pattern 평가로 전달되는지 검증한다.</summary>
    public static class DiceFaceUpgradeRuntimeConnectionScenarioTestV1
    {
        [MenuItem("Tools/Tessera/Validation/Run DiceFaceUpgrade Runtime Connection Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;
            ValidateWildUpgradeAffectsAvailablePattern(ref failures);
            ValidatePreviewUsesFaceUpgradePattern(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[DiceFaceUpgradeRuntimeConnectionScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[DiceFaceUpgradeRuntimeConnectionScenarioTest] PASS All DiceFaceUpgrade runtime connection scenario checks v1");
        }

        private static void ValidateWildUpgradeAffectsAvailablePattern(ref int failures)
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(1234);
            RoundState roundState = simulator.StartRound(
                RoundRuleContext.CreateDefault(),
                20,
                new OverchargeState(),
                null,
                null,
                CreateFaceUpgrades(0, 0, new DiceFace(DiceFaceType.Wild, 1)));

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 1, 2, 3, 4, 6 });
            bool canSubmitLargeStraight = simulator.CanSubmitPattern(roundState, RollPatternType.LargeStraight);

            if (!canSubmitLargeStraight)
                Fail(ref failures, "WildAvailablePattern", "LargeStraight can be submitted after Dice 1 Face 1 becomes Wild.");
            else
                Debug.Log("[DiceFaceUpgradeRuntimeConnectionScenarioTest] PASS WildAvailablePattern");
        }

        private static void ValidatePreviewUsesFaceUpgradePattern(ref int failures)
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(1234);
            RoundState roundState = simulator.StartRound(
                RoundRuleContext.CreateDefault(),
                20,
                new OverchargeState(),
                null,
                null,
                CreateFaceUpgrades(0, 0, new DiceFace(DiceFaceType.Wild, 1)));

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 1, 2, 3, 4, 6 });
            bool previewOk = simulator.TryBuildSlotPairDamagePreview(
                roundState,
                RollPatternType.LargeStraight,
                new List<int> { 0, 1, 2, 3, 4 },
                new List<SlotPairDeviceDefinition> { null, null, null, null, null },
                out PatternResult patternResult,
                out SlotPairDamagePreview preview,
                out TableRuleEvaluationResult tableRuleResult);

            if (!previewOk || patternResult == null || patternResult.PatternType != RollPatternType.LargeStraight || preview == null || tableRuleResult == null || tableRuleResult.IsCastBlocked)
                Fail(ref failures, "WildPreviewPattern", "SlotPair preview can build LargeStraight through equipped Wild FaceUpgrade.");
            else
                Debug.Log("[DiceFaceUpgradeRuntimeConnectionScenarioTest] PASS WildPreviewPattern");
        }

        private static List<DiceFaceUpgradeData> CreateFaceUpgrades(int diceIndex, int faceIndex, DiceFace replacementFace)
        {
            List<DiceFaceUpgradeData> result = new List<DiceFaceUpgradeData>(30);
            for (int i = 0; i < 30; i++)
                result.Add(DiceFaceUpgradeData.Empty);

            result[diceIndex * 6 + faceIndex] = new DiceFaceUpgradeData(true, replacementFace);
            return result;
        }

        private static void Fail(ref int failures, string label, string expected)
        {
            failures++;
            Debug.LogError($"[DiceFaceUpgradeRuntimeConnectionScenarioTest] FAIL {label}. Expected={expected}");
        }
    }
}
#endif
