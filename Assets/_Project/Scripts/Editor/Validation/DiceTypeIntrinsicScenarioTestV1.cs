#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using Tessera.Runtime;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>DiceSet 구매와 DiceType 고유 효과의 전투 연결을 결정적 시나리오로 검증한다.</summary>
    public static class DiceTypeIntrinsicScenarioTestV1
    {
        private const string DiceTypePathPrefix = "Assets/_Project/ScriptableObjects/DiceTypes/DiceType_";
        private const string ShopProductPathPrefix = "Assets/_Project/ScriptableObjects/Shop/Generated/DiceTypes/ShopProduct_DiceType_";
        private const string WorkshopRulesPath = "Assets/_Project/ScriptableObjects/Stages/Stage01/Stage01_WorkshopRules.asset";

        /// <summary>Editor 메뉴에서 DiceType 고유 효과 시나리오 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run DiceType Intrinsic Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;
            Dictionary<string, DiceTypeDefinitionSO> diceTypes = LoadDiceTypes(ref failures);
            ValidateShopProductsAndWorkshopRules(diceTypes, ref failures);
            ValidateDiceSetPurchase(diceTypes, ref failures);
            ValidateSlotPairIntrinsic(diceTypes, ref failures);
            ValidatePreviewSubmitParity(diceTypes, ref failures);
            ValidatePostProcessHooks(diceTypes, ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[DiceTypeScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[DiceTypeScenarioTest] PASS All DiceType intrinsic scenario checks v1");
        }

        /// <summary>검증에 필요한 DiceType SO를 로드한다.</summary>
        private static Dictionary<string, DiceTypeDefinitionSO> LoadDiceTypes(ref int failures)
        {
            string[] names = { "Red", "Blue", "Green", "Iron", "Gold", "Void" };
            Dictionary<string, DiceTypeDefinitionSO> diceTypes = new Dictionary<string, DiceTypeDefinitionSO>();

            for (int i = 0; i < names.Length; i++)
            {
                DiceTypeDefinitionSO diceType = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(DiceTypePathPrefix + names[i] + ".asset");
                if (diceType == null)
                {
                    Fail(ref failures, names[i], "LoadDiceType", "asset", "null");
                    continue;
                }

                diceTypes[names[i]] = diceType;
            }

            return diceTypes;
        }

        /// <summary>DiceType ShopProduct와 WorkshopRules 에셋 연결을 검증한다.</summary>
        private static void ValidateShopProductsAndWorkshopRules(Dictionary<string, DiceTypeDefinitionSO> diceTypes, ref int failures)
        {
            foreach (KeyValuePair<string, DiceTypeDefinitionSO> pair in diceTypes)
            {
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(ShopProductPathPrefix + pair.Key + ".asset");
                if (product == null || product.ProductType != ShopProductType.DiceSet || product.DiceTypeDefinition != pair.Value)
                    Fail(ref failures, pair.Key, "ShopProduct", "DiceSet linked", product != null ? product.ProductType.ToString() : "null");
            }

            ScriptableObject workshopRules = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WorkshopRulesPath);
            if (workshopRules == null)
                Fail(ref failures, "Stage01_WorkshopRules", "Load", "asset", "null");
            else
                Debug.Log("[DiceTypeScenarioTest] PASS WorkshopRulesHook");
        }

        /// <summary>DiceSet 적용 시 RunSession의 모든 DiceType이 교체되는지 검증한다.</summary>
        private static void ValidateDiceSetPurchase(Dictionary<string, DiceTypeDefinitionSO> diceTypes, ref int failures)
        {
            TesseraRunSession session = new TesseraRunSession(20, 20);
            DiceTypeDefinitionSO red = diceTypes["Red"];
            bool applied = session.SetDiceSetType(red);

            for (int i = 0; i < TesseraRunSession.PlayerDiceCount; i++)
            {
                if (!applied || session.GetEquippedDiceType(i) != red)
                    Fail(ref failures, "Red", "DiceSetPurchase", red.name, session.GetEquippedDiceType(i) != null ? session.GetEquippedDiceType(i).name : "null");
            }

            Debug.Log("[DiceTypeScenarioTest] PASS DiceSetPurchase");
        }

        /// <summary>SlotPair 계산에서 주요 DiceType Score/Force 보정이 반영되는지 검증한다.</summary>
        private static void ValidateSlotPairIntrinsic(Dictionary<string, DiceTypeDefinitionSO> diceTypes, ref int failures)
        {
            ValidateScoreBonus(diceTypes["Red"], 1, RollPatternType.Chance, ref failures, "Red Odd Score");
            ValidateForceBonus(diceTypes["Blue"], 2, RollPatternType.Chance, ref failures, "Blue Even Force");
            ValidateScoreBonus(diceTypes["Green"], 1, RollPatternType.Chance, ref failures, "Green AtMost Score");
            ValidateScoreBonus(diceTypes["Iron"], 5, RollPatternType.Chance, ref failures, "Iron AtLeast Score");
        }

        /// <summary>Score 보정 DiceType 시나리오의 최소 기대값을 검증한다.</summary>
        private static void ValidateScoreBonus(DiceTypeDefinitionSO diceType, int value, RollPatternType patternType, ref int failures, string label)
        {
            SlotPairDamagePreview preview = BuildPreview(diceType, value, patternType);
            int expectedMinimum = value + Mathf.Max(0, diceType.IntValue);
            if (preview == null || preview.FinalScore < expectedMinimum)
                Fail(ref failures, diceType.name, label, expectedMinimum.ToString(), preview != null ? preview.FinalScore.ToString() : "null");
            else
                Debug.Log($"[DiceTypeScenarioTest] PASS {label}");
        }

        /// <summary>Force 보정 DiceType 시나리오의 최소 기대값을 검증한다.</summary>
        private static void ValidateForceBonus(DiceTypeDefinitionSO diceType, int value, RollPatternType patternType, ref int failures, string label)
        {
            SlotPairDamagePreview preview = BuildPreview(diceType, value, patternType);
            float expectedMinimum = 1f + (diceType.FloatValue > 0f ? diceType.FloatValue : diceType.IntValue);
            if (preview == null || preview.FinalForce + 0.001f < expectedMinimum)
                Fail(ref failures, diceType.name, label, expectedMinimum.ToString("0.##"), preview != null ? preview.FinalForce.ToString("0.##") : "null");
            else
                Debug.Log($"[DiceTypeScenarioTest] PASS {label}");
        }

        /// <summary>Preview 계산과 실제 제출 계산의 CastPower 일치 여부를 검증한다.</summary>
        private static void ValidatePreviewSubmitParity(Dictionary<string, DiceTypeDefinitionSO> diceTypes, ref int failures)
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(1234);
            RoundRuleContext ruleContext = RoundRuleContext.CreateDefault();
            List<DiceTypeDefinitionSO> redTypes = CreateDiceTypes(diceTypes["Red"]);
            RoundState roundState = simulator.StartRound(ruleContext, 20, new OverchargeState(), redTypes);
            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 1, 2, 3, 4, 5 });
            List<int> lockSlots = new List<int> { 0, 1, 2, 3, 4 };
            List<SlotPairDeviceDefinition> devices = CreateEmptyDevices();

            bool previewOk = simulator.TryBuildSlotPairDamagePreview(roundState, RollPatternType.Chance, lockSlots, devices, out PatternResult previewPattern, out SlotPairDamagePreview preview, out TableRuleEvaluationResult previewRules);
            bool submitOk = simulator.TryBuildPlayerClashCastResult(roundState, RollPatternType.Chance, lockSlots, devices, out ClashCastResult submitResult);

            if (!previewOk || !submitOk || preview.CastPowerBeforeTableRules != submitResult.SlotPairDamagePreview.CastPowerBeforeTableRules)
                Fail(ref failures, "Red", "PreviewSubmitParity", previewOk ? preview.CastPowerBeforeTableRules.ToString() : "preview fail", submitOk ? submitResult.SlotPairDamagePreview.CastPowerBeforeTableRules.ToString() : "submit fail");
            else
                Debug.Log("[DiceTypeScenarioTest] PASS PreviewSubmitParity");
        }

        /// <summary>Gold와 Void 후처리 Hook 계산 결과를 검증한다.</summary>
        private static void ValidatePostProcessHooks(Dictionary<string, DiceTypeDefinitionSO> diceTypes, ref int failures)
        {
            DiceTypeIntrinsicEvaluator evaluator = new DiceTypeIntrinsicEvaluator();
            int gold = evaluator.CalculateMoneyOnRoundWinBonus(new List<DiceTypeDefinitionSO> { diceTypes["Gold"] });
            int voidReduction = evaluator.CalculateIncomingDamageReduction(new List<DiceTypeDefinitionSO> { diceTypes["Void"] });

            if (gold != Mathf.Max(0, diceTypes["Gold"].IntValue))
                Fail(ref failures, "Gold", "MoneyHook", diceTypes["Gold"].IntValue.ToString(), gold.ToString());
            else
                Debug.Log("[DiceTypeScenarioTest] PASS Gold Money Hook");

            if (voidReduction != Mathf.Max(0, diceTypes["Void"].IntValue))
                Fail(ref failures, "Void", "DamageReductionHook", diceTypes["Void"].IntValue.ToString(), voidReduction.ToString());
            else
                Debug.Log("[DiceTypeScenarioTest] PASS Void Damage Hook");
        }

        /// <summary>실제 SlotPairDamageCalculator 경로로 단일 시나리오 Preview를 생성한다.</summary>
        private static SlotPairDamagePreview BuildPreview(DiceTypeDefinitionSO diceType, int firstValue, RollPatternType patternType)
        {
            PatternResult patternResult = new PatternResult(patternType, new List<int> { firstValue }, firstValue, 0, 1, 0, 0, firstValue);
            SlotPairDamageCalculator calculator = new SlotPairDamageCalculator();
            return calculator.Calculate(
                patternResult,
                new List<int> { firstValue, 2, 3, 4, 5 },
                new List<int> { 0, -1, -1, -1, -1 },
                CreateEmptyDevices(),
                new SlotPairCalculationContext(0, CreateDiceTypes(diceType)));
        }

        /// <summary>동일 DiceType 5개로 테스트용 DiceType 목록을 생성한다.</summary>
        private static List<DiceTypeDefinitionSO> CreateDiceTypes(DiceTypeDefinitionSO diceType)
        {
            return new List<DiceTypeDefinitionSO> { diceType, diceType, diceType, diceType, diceType };
        }

        /// <summary>Device 효과가 없는 테스트용 SlotPair Device 목록을 생성한다.</summary>
        private static List<SlotPairDeviceDefinition> CreateEmptyDevices()
        {
            return new List<SlotPairDeviceDefinition> { null, null, null, null, null };
        }

        /// <summary>검증 실패를 누적하고 명확한 오류 로그를 출력한다.</summary>
        private static void Fail(ref int failures, string diceType, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[DiceTypeScenarioTest] FAIL DiceType={diceType} Location={location} Expected={expected} Actual={actual}");
        }
    }
}
#endif
