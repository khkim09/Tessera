using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tessera.Data;

namespace Tessera.Editor
{
    /// <summary>Stage01 WorkshopRules의 Slot Rule 설정을 자동으로 업데이트한다.</summary>
    public static class StageWorkshopSlotRuleConfigurator
    {
        private const string MenuPath = "Tools/Tessera/Configure Stage 1 Workshop Slot Rules";

        // --- Device 상품 경로 (Stage01 Tier 1 중심) ---
        private static readonly string[] DevicePaths =
        {
            // Stage01 Tier 1 기본 Device 6종
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_AdderChip.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_OddAmplifier.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_EvenAmplifier.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_LeftBooster.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_RightBooster.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_ForceSpring.asset",
            // Stage01 Tier 1 Impact Device 2종
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_ImpactNail.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_HeavyHammer.asset",
            // 기존 Common Device 12종
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_SafetyPin.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_UnstableFuse.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_FrontLoader.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_PressureGaugeLight.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_LeadWeight.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_LowGear.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_PairContact.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_LeftCoupler.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_RelayMotor.asset",
            "Assets/_Project/ScriptableObjects/Shop/Generated/Devices/ShopProduct_Device_StagePressureMeter.asset",
        };

        // --- DiceType 상품 경로 (Stage01: DiceType 효과 미구현이므로 제외) ---
        private static readonly string[] DiceTypePaths =
        {
            // DiceType 효과가 실제 전투 계산에 반영되지 않으므로 Stage01 풀에서 제외.
            // 내부 placeholder는 유지하되 ShopProduct로 노출하지 않는다.
        };

        // --- DiceFaceUpgrade 상품 경로 (Stage01: FaceUpgrade 효과 미구현이므로 제외) ---
        private static readonly string[] DiceFaceUpgradePaths =
        {
            // DiceFaceUpgrade 효과가 실제 전투 계산에 반영되지 않으므로 Stage01 풀에서 제외.
            // 내부 placeholder는 유지하되 ShopProduct로 노출하지 않는다.
        };

        /// <summary>Stage01 WorkshopRules의 Slot Rule을 설정한다.</summary>
        [MenuItem(MenuPath)]
        private static void ConfigureSlotRules()
        {
            // 1. Stage01_WorkshopRules 에셋 찾기
            StageWorkshopRulesSO rules = FindStageWorkshopRules();
            if (rules == null)
            {
                Debug.LogError("[StageWorkshopSlotRuleConfigurator] Stage01_WorkshopRules 에셋을 찾을 수 없습니다.");
                return;
            }

            // 2. ShopProduct 에셋 로드
            List<ShopProductDefinitionSO> allDevices = LoadProducts(DevicePaths, "Device");
            List<ShopProductDefinitionSO> allDiceTypes = LoadProducts(DiceTypePaths, "DiceType");
            List<ShopProductDefinitionSO> allDiceFaceUpgrades = LoadProducts(DiceFaceUpgradePaths, "DiceFaceUpgrade");

            // Slot 0: Left Device (Tier 1) — StagePressureMeter 제외
            List<ShopProductDefinitionSO> slot0Devices = new List<ShopProductDefinitionSO>(allDevices);
            slot0Devices.RemoveAll(p => p != null && p.name.Contains("StagePressureMeter"));

            // Slot 1: Right Device (Tier 1~2) — 전부 포함
            List<ShopProductDefinitionSO> slot1Devices = new List<ShopProductDefinitionSO>(allDevices);

            // 3. SerializedObject로 업데이트
            SerializedObject serializedObject = new SerializedObject(rules);

            // productSlotCount = 6
            SerializedProperty slotCountProp = serializedObject.FindProperty("productSlotCount");
            if (slotCountProp == null)
            {
                Debug.LogError("[StageWorkshopSlotRuleConfigurator] productSlotCount 필드를 찾을 수 없습니다.");
                return;
            }
            slotCountProp.intValue = 6;

            // allowDuplicateProducts = false
            SerializedProperty allowDupProp = serializedObject.FindProperty("allowDuplicateProducts");
            if (allowDupProp == null)
            {
                Debug.LogError("[StageWorkshopSlotRuleConfigurator] allowDuplicateProducts 필드를 찾을 수 없습니다.");
                return;
            }
            allowDupProp.boolValue = false;

            // productSlotRules 배열
            SerializedProperty slotRulesProp = serializedObject.FindProperty("productSlotRules");
            if (slotRulesProp == null)
            {
                Debug.LogError("[StageWorkshopSlotRuleConfigurator] productSlotRules 필드를 찾을 수 없습니다.");
                return;
            }

            // Size = 6
            slotRulesProp.arraySize = 6;

            // Slot 0: Left Device
            ConfigureSlot(slotRulesProp, 0,
                "Left Device",
                new[] { ShopProductType.Device },
                slot0Devices.ToArray(),
                1, 1);

            // Slot 1: Right Device
            ConfigureSlot(slotRulesProp, 1,
                "Right Device",
                new[] { ShopProductType.Device },
                slot1Devices.ToArray(),
                1, 2);

            // Slot 2: Left Dice Type
            ConfigureSlot(slotRulesProp, 2,
                "Left Dice Type",
                new[] { ShopProductType.DiceSet, ShopProductType.SingleDice, ShopProductType.DiceTypeUpgrade },
                allDiceTypes.ToArray(),
                1, 1);

            // Slot 3: Right Dice Type
            ConfigureSlot(slotRulesProp, 3,
                "Right Dice Type",
                new[] { ShopProductType.DiceSet, ShopProductType.SingleDice, ShopProductType.DiceTypeUpgrade },
                allDiceTypes.ToArray(),
                1, 2);

            // Slot 4: Left Face Upgrade
            ConfigureSlot(slotRulesProp, 4,
                "Left Face Upgrade",
                new[] { ShopProductType.DiceFaceUpgrade },
                allDiceFaceUpgrades.ToArray(),
                1, 1);

            // Slot 5: Right Face Upgrade
            ConfigureSlot(slotRulesProp, 5,
                "Right Face Upgrade",
                new[] { ShopProductType.DiceFaceUpgrade },
                allDiceFaceUpgrades.ToArray(),
                1, 2);

            // 4. 변경 사항 적용
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rules);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. 검증 및 로그 출력
            VerifyAndLog(rules, allDevices, allDiceTypes, allDiceFaceUpgrades);
        }

        /// <summary>이름으로 StageWorkshopRulesSO 에셋을 찾아 반환한다.</summary>
        private static StageWorkshopRulesSO FindStageWorkshopRules()
        {
            // 우선 지정 경로에서 로드 시도
            string path = "Assets/_Project/ScriptableObjects/Stages/Stage01/Stage01_WorkshopRules.asset";
            StageWorkshopRulesSO rules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(path);
            if (rules != null)
                return rules;

            // 경로에 없으면 FindAssets로 검색
            string[] guids = AssetDatabase.FindAssets("Stage01_WorkshopRules t:StageWorkshopRulesSO");
            if (guids.Length > 0)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(foundPath);
            }

            return null;
        }

        /// <summary>지정 경로 목록에서 ShopProductDefinitionSO를 로드한다. 누락 시 Warning을 출력한다.</summary>
        private static List<ShopProductDefinitionSO> LoadProducts(string[] paths, string categoryName)
        {
            List<ShopProductDefinitionSO> results = new List<ShopProductDefinitionSO>();
            foreach (string path in paths)
            {
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(path);
                if (product != null)
                {
                    results.Add(product);
                }
                else
                {
                    // 경로에 없으면 이름으로 검색
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    string[] guids = AssetDatabase.FindAssets($"{fileName} t:ShopProductDefinitionSO");
                    if (guids.Length > 0)
                    {
                        string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(foundPath);
                        if (product != null)
                        {
                            results.Add(product);
                            continue;
                        }
                    }

                    Debug.LogWarning($"[StageWorkshopSlotRuleConfigurator] {categoryName} 상품을 찾을 수 없습니다: {fileName} (경로: {path})");
                }
            }
            return results;
        }

        /// <summary>지정 인덱스의 Slot Rule을 SerializedProperty로 설정한다.</summary>
        private static void ConfigureSlot(
            SerializedProperty slotRulesProp,
            int index,
            string displayName,
            ShopProductType[] allowedTypes,
            ShopProductDefinitionSO[] productPool,
            int minTier,
            int maxTier)
        {
            SerializedProperty slotProp = slotRulesProp.GetArrayElementAtIndex(index);

            // displayName
            SerializedProperty nameProp = slotProp.FindPropertyRelative("displayName");
            if (nameProp == null)
            {
                Debug.LogError($"[StageWorkshopSlotRuleConfigurator] Slot {index}의 displayName 필드를 찾을 수 없습니다.");
                return;
            }
            nameProp.stringValue = displayName;

            // allowedProductTypes
            SerializedProperty typesProp = slotProp.FindPropertyRelative("allowedProductTypes");
            if (typesProp == null)
            {
                Debug.LogError($"[StageWorkshopSlotRuleConfigurator] Slot {index}의 allowedProductTypes 필드를 찾을 수 없습니다.");
                return;
            }
            typesProp.arraySize = allowedTypes.Length;
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                SerializedProperty elemProp = typesProp.GetArrayElementAtIndex(i);
                // enumValueIndex는 enum 선언 순서상의 인덱스(0-based)를 사용한다.
                // ShopProductType은 사용자 정의 값을 가지므로 ordinal index로 변환한다.
                elemProp.enumValueIndex = ShopProductTypeToIndex(allowedTypes[i]);
            }

            // productPool
            SerializedProperty poolProp = slotProp.FindPropertyRelative("productPool");
            if (poolProp == null)
            {
                Debug.LogError($"[StageWorkshopSlotRuleConfigurator] Slot {index}의 productPool 필드를 찾을 수 없습니다.");
                return;
            }
            poolProp.arraySize = productPool.Length;
            for (int i = 0; i < productPool.Length; i++)
            {
                SerializedProperty elemProp = poolProp.GetArrayElementAtIndex(i);
                elemProp.objectReferenceValue = productPool[i];
            }

            // minTierOverride
            SerializedProperty minTierProp = slotProp.FindPropertyRelative("minTierOverride");
            if (minTierProp == null)
            {
                Debug.LogError($"[StageWorkshopSlotRuleConfigurator] Slot {index}의 minTierOverride 필드를 찾을 수 없습니다.");
                return;
            }
            minTierProp.intValue = minTier;

            // maxTierOverride
            SerializedProperty maxTierProp = slotProp.FindPropertyRelative("maxTierOverride");
            if (maxTierProp == null)
            {
                Debug.LogError($"[StageWorkshopSlotRuleConfigurator] Slot {index}의 maxTierOverride 필드를 찾을 수 없습니다.");
                return;
            }
            maxTierProp.intValue = maxTier;
        }

        /// <summary>ShopProductType enum 값을 선언 순서상의 인덱스(0-based)로 변환한다.</summary>
        private static int ShopProductTypeToIndex(ShopProductType type)
        {
            // ShopProductType 선언 순서:
            // None=0, Device=10, DiceSet=20, SingleDice=21, DiceTypeUpgrade=22,
            // DiceFaceUpgrade=23, Consumable=30, PermanentUpgrade=40, HpRepair=50,
            // WorkshopReroll=100, WorkshopTierUpgrade=101, RareSlotUnlock=102
            switch (type)
            {
                case ShopProductType.None: return 0;
                case ShopProductType.Device: return 1;
                case ShopProductType.DiceSet: return 2;
                case ShopProductType.SingleDice: return 3;
                case ShopProductType.DiceTypeUpgrade: return 4;
                case ShopProductType.DiceFaceUpgrade: return 5;
                case ShopProductType.Consumable: return 6;
                case ShopProductType.PermanentUpgrade: return 7;
                case ShopProductType.HpRepair: return 8;
                case ShopProductType.WorkshopReroll: return 9;
                case ShopProductType.WorkshopTierUpgrade: return 10;
                case ShopProductType.RareSlotUnlock: return 11;
                default: return 0;
            }
        }

        /// <summary>설정 결과를 검증하고 로그를 출력한다.</summary>
        private static void VerifyAndLog(
            StageWorkshopRulesSO rules,
            List<ShopProductDefinitionSO> allDevices,
            List<ShopProductDefinitionSO> allDiceTypes,
            List<ShopProductDefinitionSO> allDiceFaceUpgrades)
        {
            Debug.Log("=== StageWorkshopSlotRuleConfigurator 설정 완료 ===");
            Debug.Log($"대상 에셋: {AssetDatabase.GetAssetPath(rules)}");
            Debug.Log($"productSlotCount: {rules.ProductSlotCount}");
            Debug.Log($"allowDuplicateProducts: {rules.AllowDuplicateProducts}");
            Debug.Log($"productSlotRules 배열 크기: {(rules.ProductSlotRules != null ? rules.ProductSlotRules.Count : 0)}");

            if (rules.ProductSlotRules != null)
            {
                for (int i = 0; i < rules.ProductSlotRules.Count; i++)
                {
                    var slot = rules.ProductSlotRules[i];
                    if (slot == null)
                    {
                        Debug.LogWarning($"Slot {i}: null");
                        continue;
                    }

                    int poolCount = (slot.ProductPool != null) ? slot.ProductPool.Length : 0;
                    Debug.Log($"Slot {i}: \"{slot.DisplayName}\" | AllowedTypes={slot.AllowedProductTypes?.Length ?? 0} | Pool={poolCount} | Tier={slot.MinTierOverride}~{slot.MaxTierOverride}");
                }
            }

            // 누락된 상품 확인
            int loadedDevices = allDevices.Count;
            int loadedDiceTypes = allDiceTypes.Count;
            int loadedFaceUpgrades = allDiceFaceUpgrades.Count;

            Debug.Log($"로드된 Device 상품: {loadedDevices}/{DevicePaths.Length}");
            Debug.Log($"로드된 DiceType 상품: {loadedDiceTypes}/{DiceTypePaths.Length}");
            Debug.Log($"로드된 DiceFaceUpgrade 상품: {loadedFaceUpgrades}/{DiceFaceUpgradePaths.Length}");

            if (loadedDevices < DevicePaths.Length)
                Debug.LogWarning($"Device 상품 {DevicePaths.Length - loadedDevices}개 누락됨.");
            if (loadedDiceTypes < DiceTypePaths.Length)
                Debug.LogWarning($"DiceType 상품 {DiceTypePaths.Length - loadedDiceTypes}개 누락됨.");
            if (loadedFaceUpgrades < DiceFaceUpgradePaths.Length)
                Debug.LogWarning($"DiceFaceUpgrade 상품 {DiceFaceUpgradePaths.Length - loadedFaceUpgrades}개 누락됨.");

            Debug.Log("=== StageWorkshopSlotRuleConfigurator 검증 완료 ===");
        }
    }
}
