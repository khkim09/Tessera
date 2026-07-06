#if UNITY_EDITOR
using System.IO;
using Tessera.Data;
using Tessera.Runtime;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>Shop 카드 배경과 확장 상품 타입의 기본 런타임 연결을 검증한다.</summary>
    public static class ShopProductVisualAndTypeScenarioTestV1
    {
        private const string ShopProductDefinitionPath = "Assets/_Project/Scripts/Data/Shop/ShopProductDefinitionSO.cs";
        private const string ShopProductCardViewPath = "Assets/_Project/Scripts/UI/Shop/ShopProductCardView.cs";
        private const string StageFlowControllerPath = "Assets/_Project/Scripts/Runtime/Stage/StageBountyFlowController.cs";

        [MenuItem("Tools/Tessera/Validation/Run Shop Product Visual And Type Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;
            ValidateCardBackgroundWiring(ref failures);
            ValidateFormalProductDefinitions(ref failures);
            ValidateRunSessionUpgradeHooks(ref failures);
            ValidateRuntimePurchaseSwitchWiring(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[ShopProductVisualTypeScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[ShopProductVisualTypeScenarioTest] PASS All ShopProduct visual/type scenario checks v1");
        }

        private static void ValidateCardBackgroundWiring(ref int failures)
        {
            string definitionSource = File.ReadAllText(ShopProductDefinitionPath);
            string cardSource = File.ReadAllText(ShopProductCardViewPath);

            if (!definitionSource.Contains("CardBackgroundSprite") || !cardSource.Contains("product.CardBackgroundSprite"))
                Fail(ref failures, "CardBackgroundWiring", "ShopProductDefinitionSO CardBackgroundSprite is assigned by ShopProductCardView.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS CardBackgroundWiring");
        }

        private static void ValidateFormalProductDefinitions(ref int failures)
        {
            ShopProductDefinitionSO consumableProduct = CreateProduct(ShopProductType.Consumable, "consumableDefinition", CreateConsumable());
            ShopProductDefinitionSO permanentProduct = CreateProduct(ShopProductType.PermanentUpgrade, "permanentUpgradeDefinition", CreatePermanentUpgrade());
            ShopProductDefinitionSO repairProduct = CreateProduct(ShopProductType.HpRepair, "hpRepairDefinition", CreateHpRepair());

            if (!consumableProduct.IsPurchasableInCurrentBuild() || consumableProduct.ItemDefinition == null)
                Fail(ref failures, "ConsumableProduct", "Consumable product resolves formal definition.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS ConsumableProduct");

            if (!permanentProduct.IsPurchasableInCurrentBuild() || permanentProduct.ItemDefinition == null)
                Fail(ref failures, "PermanentUpgradeProduct", "PermanentUpgrade product resolves formal definition.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS PermanentUpgradeProduct");

            if (!repairProduct.IsPurchasableInCurrentBuild() || repairProduct.ItemDefinition == null)
                Fail(ref failures, "HpRepairProduct", "HpRepair product resolves formal definition.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS HpRepairProduct");
        }

        private static void ValidateRunSessionUpgradeHooks(ref int failures)
        {
            TesseraRunSession session = new TesseraRunSession(startMoney: 0, playerMaxHP: 20);
            session.SetPlayerCurrentHP(10);
            int increased = session.IncreasePlayerMaxHP(5);
            int repaired = session.RepairPlayerHP(3);
            session.AddOvercharge(2);
            session.SetWorkshopTier(1);
            session.SetWorkshopTier(session.CurrentWorkshopTier + 1);

            if (increased != 5 || repaired != 3 || session.PlayerMaxHP != 25 || session.PlayerCurrentHP != 18 || session.Overcharge != 2 || session.CurrentWorkshopTier != 2)
                Fail(ref failures, "RunSessionUpgradeHooks", "MaxHP/HP/Overcharge/WorkshopTier hooks apply expected values.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS RunSessionUpgradeHooks");
        }

        private static void ValidateRuntimePurchaseSwitchWiring(ref int failures)
        {
            string source = File.ReadAllText(StageFlowControllerPath);
            bool ok = source.Contains("ShopProductType.Consumable")
                && source.Contains("TryApplyPurchasedConsumableProduct")
                && source.Contains("ShopProductType.PermanentUpgrade")
                && source.Contains("TryApplyPurchasedPermanentUpgradeProduct")
                && source.Contains("ShopProductType.HpRepair")
                && source.Contains("TryApplyPurchasedHpRepairProduct");

            if (!ok)
                Fail(ref failures, "RuntimePurchaseSwitchWiring", "StageBountyFlowController handles Consumable/PermanentUpgrade/HpRepair.");
            else
                Debug.Log("[ShopProductVisualTypeScenarioTest] PASS RuntimePurchaseSwitchWiring");
        }

        private static ShopProductDefinitionSO CreateProduct(ShopProductType type, string fieldName, ScriptableObject item)
        {
            ShopProductDefinitionSO product = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            SerializedObject serialized = new SerializedObject(product);
            serialized.FindProperty("productType").intValue = (int)type;
            serialized.FindProperty(fieldName).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return product;
        }

        private static ShopConsumableDefinitionSO CreateConsumable()
        {
            ShopConsumableDefinitionSO asset = ScriptableObject.CreateInstance<ShopConsumableDefinitionSO>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("effectType").intValue = (int)ShopConsumableEffectType.AddOvercharge;
            serialized.FindProperty("intValue").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static ShopPermanentUpgradeDefinitionSO CreatePermanentUpgrade()
        {
            ShopPermanentUpgradeDefinitionSO asset = ScriptableObject.CreateInstance<ShopPermanentUpgradeDefinitionSO>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("effectType").intValue = (int)ShopPermanentUpgradeEffectType.IncreaseMaxHP;
            serialized.FindProperty("intValue").intValue = 5;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static ShopHpRepairDefinitionSO CreateHpRepair()
        {
            ShopHpRepairDefinitionSO asset = ScriptableObject.CreateInstance<ShopHpRepairDefinitionSO>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("healAmount").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void Fail(ref int failures, string label, string expected)
        {
            failures++;
            Debug.LogError($"[ShopProductVisualTypeScenarioTest] FAIL {label}. Expected={expected}");
        }
    }
}
#endif
