#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>BrokenCast/Clash 후처리 Device 효과를 검증한다.</summary>
    public static class BrokenCastDevicePostProcessScenarioTestV1
    {
        /// <summary>Editor 메뉴에서 BrokenCast/Clash Device 후처리 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run BrokenCast Device PostProcess Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;

            ValidateBrokenCastOverchargeBonus(ref failures);
            ValidateBrokenCastIncomingDamageReduction(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[BrokenCastDevicePostProcessScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[BrokenCastDevicePostProcessScenarioTest] PASS All BrokenCast Device post-process scenario checks v1");
        }

        private static void ValidateBrokenCastOverchargeBonus(ref int failures)
        {
            ClashResolveResult result = BuildBrokenCastLossResult(
                new List<SlotPairDeviceDefinition>
                {
                    CreateBrokenCastOverchargeDevice(2),
                    null,
                    null,
                    null,
                    null
                });

            if (result == null || !result.DidGrantOvercharge || result.GrantedOverchargeAmount != 3)
                Fail(ref failures, "BrokenCastOverchargeBonus", "Base 1 + Device 2 = 3", FormatResult(result));
            else
                Debug.Log("[BrokenCastDevicePostProcessScenarioTest] PASS BrokenCastOverchargeBonus");
        }

        private static void ValidateBrokenCastIncomingDamageReduction(ref int failures)
        {
            ClashResolveResult baseline = BuildBrokenCastLossResult(CreateEmptyDevices());
            ClashResolveResult reduced = BuildBrokenCastLossResult(
                new List<SlotPairDeviceDefinition>
                {
                    null,
                    CreateBrokenCastIncomingDamageReductionDevice(3),
                    null,
                    null,
                    null
                });

            int expectedReducedDamage = Mathf.Max(0, baseline.AppliedImpactDamageToPlayer - 3);
            if (reduced == null || reduced.IncomingDamageReductionFromDevice != 3 || reduced.AppliedImpactDamageToPlayer != expectedReducedDamage)
            {
                string actual = $"Baseline={FormatResult(baseline)}, Reduced={FormatResult(reduced)}, ExpectedReducedDamage={expectedReducedDamage}";
                Fail(ref failures, "BrokenCastIncomingDamageReduction", "DeviceReduction=3 and player damage reduced by 3", actual);
            }
            else
            {
                Debug.Log("[BrokenCastDevicePostProcessScenarioTest] PASS BrokenCastIncomingDamageReduction");
            }
        }

        private static ClashResolveResult BuildBrokenCastLossResult(IReadOnlyList<SlotPairDeviceDefinition> playerDevices)
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(2468);
            RoundState roundState = simulator.StartRound(RoundRuleContext.CreateDefault(), 20, new OverchargeState());
            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 1, 2, 3, 4, 5 });

            List<int> lockSlots = new List<int> { 0, 1, 2, 3, 4 };
            bool playerOk = simulator.TryBuildPlayerClashCastResult(
                roundState,
                RollPatternType.BrokenCast,
                lockSlots,
                playerDevices,
                out ClashCastResult playerResult);

            bool opponentOk = simulator.TryBuildClashCastResult(
                roundState,
                ClashParticipantType.Opponent,
                RollPatternType.Tessera,
                lockSlots,
                CreateEmptyDevices(),
                new List<int> { 6, 6, 6, 6, 6 },
                out ClashCastResult opponentResult);

            if (!playerOk || !opponentOk)
                return null;

            return simulator.ResolveClash(roundState, playerResult, opponentResult);
        }

        private static List<SlotPairDeviceDefinition> CreateEmptyDevices()
        {
            return new List<SlotPairDeviceDefinition> { null, null, null, null, null };
        }

        private static SlotPairDeviceDefinition CreateBrokenCastOverchargeDevice(int overchargeAmount)
        {
            return CreatePostProcessDevice(
                SlotPairDeviceType.AddOverchargeOnBrokenCast,
                overchargeAmount,
                $"Overcharge +{overchargeAmount} on BrokenCast.");
        }

        private static SlotPairDeviceDefinition CreateBrokenCastIncomingDamageReductionDevice(int reductionAmount)
        {
            return CreatePostProcessDevice(
                SlotPairDeviceType.ReduceIncomingDamageOnBrokenCast,
                reductionAmount,
                $"IncomingDamage -{reductionAmount} on BrokenCast.");
        }

        private static SlotPairDeviceDefinition CreatePostProcessDevice(
            SlotPairDeviceType deviceType,
            int intValue,
            string description)
        {
            return SlotPairDeviceDefinition.Create(
                deviceType,
                intValue,
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
                description);
        }

        private static string FormatResult(ClashResolveResult result)
        {
            if (result == null)
                return "null";

            return $"Overcharge={result.GrantedOverchargeAmount}, DeviceReduction={result.IncomingDamageReductionFromDevice}, DamageToPlayer={result.AppliedImpactDamageToPlayer}";
        }

        private static void Fail(ref int failures, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[BrokenCastDevicePostProcessScenarioTest] FAIL Location={location} Expected={expected} Actual={actual}");
        }
    }
}
#endif
