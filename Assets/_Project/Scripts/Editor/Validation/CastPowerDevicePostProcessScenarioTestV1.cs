#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>CastPower 확정 이후 Device 후처리 ImpactDamage 계산을 검증한다.</summary>
    public static class CastPowerDevicePostProcessScenarioTestV1
    {
        /// <summary>Editor 메뉴에서 CastPower Device 후처리 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run CastPower Device PostProcess Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;

            ValidateTrueImpactAppliesWhenCastPowerMeetsThreshold(ref failures);
            ValidateTrueImpactDoesNotApplyBelowThreshold(ref failures);
            ValidatePreviewSubmitParity(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[CastPowerDevicePostProcessScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[CastPowerDevicePostProcessScenarioTest] PASS All CastPower Device post-process scenario checks v1");
        }

        private static void ValidateTrueImpactAppliesWhenCastPowerMeetsThreshold(ref int failures)
        {
            SlotPairDamagePreview preview = BuildPreview(requiredCastPower: 25, trueImpactDamage: 3);

            if (preview.TrueImpactDamage != 3 || !preview.Steps[0].DidApply)
                Fail(ref failures, "TrueImpactApplies", "TrueImpact=3 and Step0 applies", FormatPreview(preview));
            else
                Debug.Log("[CastPowerDevicePostProcessScenarioTest] PASS TrueImpactApplies");
        }

        private static void ValidateTrueImpactDoesNotApplyBelowThreshold(ref int failures)
        {
            SlotPairDamagePreview preview = BuildPreview(requiredCastPower: 35, trueImpactDamage: 3);

            if (preview.TrueImpactDamage != 0 || preview.Steps[0].DidApply)
                Fail(ref failures, "TrueImpactBelowThreshold", "TrueImpact=0 and Step0 inactive", FormatPreview(preview));
            else
                Debug.Log("[CastPowerDevicePostProcessScenarioTest] PASS TrueImpactBelowThreshold");
        }

        private static void ValidatePreviewSubmitParity(ref int failures)
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(4321);
            RoundState roundState = simulator.StartRound(RoundRuleContext.CreateDefault(), 20, new OverchargeState());
            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 6, 6, 6, 6, 6 });

            List<int> lockSlots = new List<int> { 0, 1, 2, 3, 4 };
            List<SlotPairDeviceDefinition> devices = CreateDevices(requiredCastPower: 25, trueImpactDamage: 3);

            bool previewOk = simulator.TryBuildSlotPairDamagePreview(
                roundState,
                RollPatternType.Chance,
                lockSlots,
                devices,
                out PatternResult previewPattern,
                out SlotPairDamagePreview preview,
                out TableRuleEvaluationResult previewRules);

            bool submitOk = simulator.TryBuildPlayerClashCastResult(
                roundState,
                RollPatternType.Chance,
                lockSlots,
                devices,
                out ClashCastResult submitResult);

            if (!previewOk || !submitOk || preview.TrueImpactDamage != submitResult.SlotPairDamagePreview.TrueImpactDamage || submitResult.ExpectedImpactBreakdown.TrueImpactDamage != 3)
            {
                string actual = $"PreviewOk={previewOk}, SubmitOk={submitOk}, Preview={FormatPreview(preview)}, SubmitTrueImpact={(submitResult != null ? submitResult.SlotPairDamagePreview.TrueImpactDamage.ToString() : "null")}, ExpectedImpactTrue={(submitResult != null ? submitResult.ExpectedImpactBreakdown.TrueImpactDamage.ToString() : "null")}";
                Fail(ref failures, "PreviewSubmitParity", "Preview/Submit TrueImpact=3", actual);
            }
            else
            {
                Debug.Log("[CastPowerDevicePostProcessScenarioTest] PASS PreviewSubmitParity");
            }
        }

        private static SlotPairDamagePreview BuildPreview(int requiredCastPower, int trueImpactDamage)
        {
            PatternResult patternResult = new PatternResult(
                RollPatternType.Chance,
                new List<int> { 6, 6, 6, 6, 6 },
                30,
                0,
                1,
                0,
                0,
                30);

            SlotPairDamageCalculator calculator = new SlotPairDamageCalculator();
            return calculator.Calculate(
                patternResult,
                new List<int> { 6, 6, 6, 6, 6 },
                new List<int> { 0, 1, 2, 3, 4 },
                CreateDevices(requiredCastPower, trueImpactDamage));
        }

        private static List<SlotPairDeviceDefinition> CreateDevices(int requiredCastPower, int trueImpactDamage)
        {
            SlotPairDeviceDefinition castPowerImpactDevice = new SlotPairDeviceDefinition(
                SlotPairDeviceType.AddTrueImpactDamageIfCastPowerAtLeast,
                requiredCastPower,
                1f,
                0f,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                0,
                trueImpactDamage,
                $"True Impact +{trueImpactDamage} if CastPower >= {requiredCastPower}.");

            return new List<SlotPairDeviceDefinition> { castPowerImpactDevice, null, null, null, null };
        }

        private static string FormatPreview(SlotPairDamagePreview preview)
        {
            if (preview == null)
                return "null";

            return $"CastPower={preview.CastPowerBeforeTableRules}, TrueImpact={preview.TrueImpactDamage}, Step0Apply={(preview.Steps.Count > 0 ? preview.Steps[0].DidApply.ToString() : "missing")}";
        }

        private static void Fail(ref int failures, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[CastPowerDevicePostProcessScenarioTest] FAIL Location={location} Expected={expected} Actual={actual}");
        }
    }
}
#endif
