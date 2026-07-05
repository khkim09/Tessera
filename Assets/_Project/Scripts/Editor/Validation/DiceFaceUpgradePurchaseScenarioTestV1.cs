#if UNITY_EDITOR
using Tessera.Data;
using Tessera.Runtime;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>DiceFaceUpgrade 구매 후 RunSession 장착 상태가 갱신되는지 결정적 시나리오로 검증한다.</summary>
    public static class DiceFaceUpgradePurchaseScenarioTestV1
    {
        private const string FaceUpgradePathPrefix = "Assets/_Project/ScriptableObjects/DiceFaceUpgrades/FaceUpgrade_";
        private const string ShopProductPathPrefix = "Assets/_Project/ScriptableObjects/Shop/Generated/DiceFaceUpgrades/ShopProduct_FaceUpgrade_";

        /// <summary>Editor 메뉴에서 DiceFaceUpgrade 구매/장착 상태 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run DiceFaceUpgrade Purchase Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;

            DiceFaceUpgradeDefinitionSO redOddRune = LoadFaceUpgrade("RedOddRune", ref failures);
            DiceFaceUpgradeDefinitionSO blueEvenRune = LoadFaceUpgrade("BlueEvenRune", ref failures);
            DiceFaceUpgradeDefinitionSO mirrorFace = LoadFaceUpgrade("MirrorFace", ref failures);

            ValidateShopProduct(redOddRune, "RedOddRune", ref failures);
            ValidateShopProduct(blueEvenRune, "BlueEvenRune", ref failures);
            ValidateImplementedFaceUpgradePurchase(redOddRune, blueEvenRune, ref failures);
            ValidatePendingFaceUpgradeCanBeEquippedAsState(mirrorFace, ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[DiceFaceUpgradeScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[DiceFaceUpgradeScenarioTest] PASS All DiceFaceUpgrade purchase scenario checks v1");
        }

        /// <summary>검증에 사용할 DiceFaceUpgrade SO를 로드한다.</summary>
        private static DiceFaceUpgradeDefinitionSO LoadFaceUpgrade(string assetName, ref int failures)
        {
            DiceFaceUpgradeDefinitionSO faceUpgrade = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(FaceUpgradePathPrefix + assetName + ".asset");
            if (faceUpgrade == null)
                Fail(ref failures, assetName, "LoadFaceUpgrade", "asset", "null");

            return faceUpgrade;
        }

        /// <summary>DiceFaceUpgrade ShopProduct 연결을 검증한다.</summary>
        private static void ValidateShopProduct(DiceFaceUpgradeDefinitionSO faceUpgrade, string assetName, ref int failures)
        {
            ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(ShopProductPathPrefix + assetName + ".asset");
            if (product == null || product.ProductType != ShopProductType.DiceFaceUpgrade || product.DiceFaceUpgradeDefinition != faceUpgrade)
                Fail(ref failures, assetName, "ShopProduct", "DiceFaceUpgrade linked", product != null ? product.ProductType.ToString() : "null");
            else
                Debug.Log($"[DiceFaceUpgradeScenarioTest] PASS {assetName} ShopProduct");
        }

        /// <summary>구현된 FaceUpgrade 구매가 같은 Dice의 대상 Face 슬롯을 교체하는지 검증한다.</summary>
        private static void ValidateImplementedFaceUpgradePurchase(
            DiceFaceUpgradeDefinitionSO firstUpgrade,
            DiceFaceUpgradeDefinitionSO secondUpgrade,
            ref int failures)
        {
            if (firstUpgrade == null || secondUpgrade == null)
                return;

            TesseraRunSession session = new TesseraRunSession(20, 20);

            bool firstApplied = session.TryApplyPurchasedDiceFaceUpgrade(firstUpgrade, out int firstDiceIndex, out int firstFaceIndex, out DiceFaceUpgradeDefinitionSO firstPrevious);
            if (!firstApplied || firstDiceIndex != 0 || firstFaceIndex != firstUpgrade.ReplacementNumberValue - 1 || firstPrevious != null || session.GetDiceFaceUpgrade(firstDiceIndex, firstFaceIndex) != firstUpgrade)
                Fail(ref failures, firstUpgrade != null ? firstUpgrade.name : "null", "ImplementedFaceUpgradeFirst", "Dice0 target Face equipped", BuildFaceUpgradeActual(firstApplied, firstDiceIndex, firstFaceIndex, firstPrevious, firstUpgrade, session));
            else
                Debug.Log("[DiceFaceUpgradeScenarioTest] PASS ImplementedFaceUpgrade FirstEquip");

            bool secondApplied = session.TryApplyPurchasedDiceFaceUpgrade(secondUpgrade, out int secondDiceIndex, out int secondFaceIndex, out DiceFaceUpgradeDefinitionSO secondPrevious);
            if (!secondApplied || secondDiceIndex != 0 || secondFaceIndex != secondUpgrade.ReplacementNumberValue - 1 || secondPrevious != null || session.GetDiceFaceUpgrade(secondDiceIndex, secondFaceIndex) != secondUpgrade)
                Fail(ref failures, secondUpgrade != null ? secondUpgrade.name : "null", "ImplementedFaceUpgradeSecond", "Dice0 target Face equipped", BuildFaceUpgradeActual(secondApplied, secondDiceIndex, secondFaceIndex, secondPrevious, secondUpgrade, session));
            else
                Debug.Log("[DiceFaceUpgradeScenarioTest] PASS ImplementedFaceUpgrade SecondEquip");
        }

        /// <summary>PatternEvaluator 전처리가 남은 FaceUpgrade도 구매 상태로는 장착 가능한지 검증한다.</summary>
        private static void ValidatePendingFaceUpgradeCanBeEquippedAsState(DiceFaceUpgradeDefinitionSO pendingUpgrade, ref int failures)
        {
            if (pendingUpgrade == null)
                return;

            TesseraRunSession session = new TesseraRunSession(20, 20);
            bool applied = session.TryApplyPurchasedDiceFaceUpgrade(pendingUpgrade, out int diceIndex, out int faceIndex, out DiceFaceUpgradeDefinitionSO previous);

            if (!applied || diceIndex != 0 || faceIndex != pendingUpgrade.ReplacementNumberValue - 1 || previous != null || session.GetDiceFaceUpgrade(diceIndex, faceIndex) != pendingUpgrade)
                Fail(ref failures, pendingUpgrade != null ? pendingUpgrade.name : "null", "PendingFaceUpgradeState", "Dice0 target Face equipped as state", BuildFaceUpgradeActual(applied, diceIndex, faceIndex, previous, pendingUpgrade, session));
            else
                Debug.Log("[DiceFaceUpgradeScenarioTest] PASS PendingFaceUpgrade StateEquip");
        }

        /// <summary>검증 실패를 누적하고 명확한 오류 로그를 출력한다.</summary>
        private static void Fail(ref int failures, string faceUpgrade, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[DiceFaceUpgradeScenarioTest] FAIL FaceUpgrade={faceUpgrade} Location={location} Expected={expected} Actual={actual}");
        }

        /// <summary>FaceUpgrade 구매 검증 실패 시 실제 상태를 읽기 쉬운 문자열로 만든다.</summary>
        private static string BuildFaceUpgradeActual(
            bool applied,
            int diceIndex,
            int faceIndex,
            DiceFaceUpgradeDefinitionSO previousUpgrade,
            DiceFaceUpgradeDefinitionSO expectedUpgrade,
            TesseraRunSession session)
        {
            DiceFaceUpgradeDefinitionSO current = diceIndex >= 0 && faceIndex >= 0
                ? session.GetDiceFaceUpgrade(diceIndex, faceIndex)
                : null;
            string previousName = previousUpgrade != null ? previousUpgrade.name : "null";
            string expectedName = expectedUpgrade != null ? expectedUpgrade.name : "null";
            string currentName = current != null ? current.name : "null";
            return $"Applied={applied}, DiceIndex={diceIndex}, FaceIndex={faceIndex}, Previous={previousName}, Expected={expectedName}, Current={currentName}";
        }
    }
}
#endif
