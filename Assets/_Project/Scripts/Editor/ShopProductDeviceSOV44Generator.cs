using System.Collections.Generic;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 Device ShopProduct SO를 생성/수정하고 Stage01_WorkshopRules의 Device 슬롯 productPool을 연결하는 Editor 유틸리티다.</summary>
    public static class ShopProductDeviceSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";

        private const string DevicesCommonPath = RootPath + "/Devices/ShopGrowth/Common";
        private const string DevicesRarePath = RootPath + "/Devices/ShopGrowth/Rare";
        private const string ShopDevicesPath = RootPath + "/Shop/Generated/Devices";

        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";
        private const string WorkshopRulesPath = StagesFolderPath + "/Stage01_WorkshopRules.asset";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        private static int _skippedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate ShopProduct Device SO v4.4 메뉴 항목이다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate ShopProduct Device SO v4.4")]
        private static void GenerateFromMenu()
        {
            GenerateForPipeline();
        }

        /// <summary>v4.4 통합 생성 파이프라인에서 호출하는 진입점이다.</summary>
        public static void GenerateForPipeline()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _skippedCount = 0;

            // 1. 폴더 생성
            EnsureFolder(RootPath + "/Shop", "Generated");
            EnsureFolder(RootPath + "/Shop/Generated", "Devices");

            // 2. Device SO 26종 로드
            SlotPairDeviceDefinitionSO[] commonDevices = LoadCommonDevices();
            SlotPairDeviceDefinitionSO[] rareDevices = LoadRareDevices();

            // 3. Device ShopProduct 26종 생성/수정
            List<ShopProductDefinitionSO> createdOrUpdatedProducts = new List<ShopProductDefinitionSO>();
            CreateOrUpdateDeviceShopProducts(commonDevices, createdOrUpdatedProducts);
            CreateOrUpdateDeviceShopProducts(rareDevices, createdOrUpdatedProducts);

            // 4. Stage01_WorkshopRules Device productPool 연결
            ConnectDeviceShopProductsToWorkshopRules(createdOrUpdatedProducts);

            // 5. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. 레거시 ShopProduct 보고
            ReportLegacyCandidates();

            // 7. 검증
            ValidateGeneratedAssets();

            // 8. 결과 출력
            Debug.Log($"[ShopProductDeviceSOV44Generator] Complete. Created: {_createdCount}, Updated: {_updatedCount}, Skipped: {_skippedCount}");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[ShopProductDeviceSOV44Generator] Created folder: {path}");
            }
        }

        // ── Device SO 로드 ─────────────────────────────────────────────

        /// <summary>Common Device SO 18종을 로드한다.</summary>
        private static SlotPairDeviceDefinitionSO[] LoadCommonDevices()
        {
            List<SlotPairDeviceDefinitionSO> devices = new List<SlotPairDeviceDefinitionSO>();

            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_AdderChip.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_OddAmplifier.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_EvenAmplifier.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_ForceSpring.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_ImpactNail.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_HeavyHammer.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_SafetyPin.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_UnstableFuse.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_LeadWeight.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_LowGear.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_CastStampAces.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_CastStampChance.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_PairContact.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_LeftCoupler.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_RelayMotor.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_FrontLoader.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_EndValveLight.asset");
            AddDeviceIfExists(devices, DevicesCommonPath + "/Device_PressureGaugeLight.asset");

            return devices.ToArray();
        }

        /// <summary>Rare Device SO 8종을 로드한다.</summary>
        private static SlotPairDeviceDefinitionSO[] LoadRareDevices()
        {
            List<SlotPairDeviceDefinitionSO> devices = new List<SlotPairDeviceDefinitionSO>();

            AddDeviceIfExists(devices, DevicesRarePath + "/Device_MirrorShard.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_IsolatedGear.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_OverdriveChip.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_FullHouseBracket.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_StraightRail.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_HighVoltagePin.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_EndValve.asset");
            AddDeviceIfExists(devices, DevicesRarePath + "/Device_StagePressureMeter.asset");

            return devices.ToArray();
        }

        /// <summary>지정 경로의 Device SO가 존재하면 목록에 추가한다. 없으면 Warning을 출력한다.</summary>
        private static void AddDeviceIfExists(List<SlotPairDeviceDefinitionSO> devices, string assetPath)
        {
            SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(assetPath);

            if (device == null)
            {
                Debug.LogWarning($"[ShopProductDeviceSOV44Generator] Device not found (skipped): {assetPath}");
                return;
            }

            devices.Add(device);
        }

        // ── Device ShopProduct 생성/수정 ───────────────────────────────

        /// <summary>Device 배열에 대해 ShopProduct를 생성/수정하고 목록에 추가한다.</summary>
        private static void CreateOrUpdateDeviceShopProducts(
            SlotPairDeviceDefinitionSO[] devices,
            List<ShopProductDefinitionSO> createdOrUpdatedProducts)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                SlotPairDeviceDefinitionSO device = devices[i];

                if (device == null)
                    continue;

                // deviceId를 SerializedObject로 읽는다
                string deviceId = ReadDeviceId(device);

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] DeviceId를 읽을 수 없습니다: {AssetDatabase.GetAssetPath(device)}");
                    _skippedCount++;
                    continue;
                }

                // productId 생성: "shop." + deviceId
                string productId = "shop." + deviceId;

                // 파일명 생성: Device_XXX.asset → ShopProduct_Device_XXX.asset
                string assetPath = AssetDatabase.GetAssetPath(device);
                string fileName = System.IO.Path.GetFileName(assetPath); // e.g. Device_AdderChip.asset
                string shopProductFileName = "ShopProduct_" + fileName;
                string shopProductPath = ShopDevicesPath + "/" + shopProductFileName;

                // ShopProduct 로드 또는 생성
                ShopProductDefinitionSO shopProduct = LoadOrCreateShopProduct(shopProductPath);

                // 필드 설정
                ApplyShopProductFields(shopProduct, productId, ShopProductType.Device, device);

                createdOrUpdatedProducts.Add(shopProduct);
            }
        }

        /// <summary>Device SO의 deviceId 필드를 SerializedObject로 읽는다.</summary>
        private static string ReadDeviceId(SlotPairDeviceDefinitionSO device)
        {
            SerializedObject so = new SerializedObject(device);
            SerializedProperty deviceIdProp = so.FindProperty("deviceId");

            if (deviceIdProp == null)
                return null;

            return deviceIdProp.stringValue;
        }

        /// <summary>Device SO의 tier 필드를 SerializedObject로 읽는다.</summary>
        private static int ReadDeviceTier(SlotPairDeviceDefinitionSO device)
        {
            SerializedObject so = new SerializedObject(device);
            SerializedProperty tierProp = so.FindProperty("tier");

            if (tierProp == null)
                return 1;

            return Mathf.Max(1, tierProp.intValue);
        }

        /// <summary>지정 경로에 ShopProductDefinitionSO가 있으면 로드하고, 없으면 새로 생성한다.</summary>
        private static ShopProductDefinitionSO LoadOrCreateShopProduct(string assetPath)
        {
            ShopProductDefinitionSO existing = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(assetPath);

            if (existing != null)
            {
                _updatedCount++;
                Debug.Log($"[ShopProductDeviceSOV44Generator] Updating existing ShopProduct: {assetPath}");
                return existing;
            }

            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[ShopProductDeviceSOV44Generator] Created new ShopProduct: {assetPath}");
            return asset;
        }

        /// <summary>SerializedObject를 통해 ShopProduct SO 필드를 설정한다. Device 전용 필드만 세팅한다.</summary>
        private static void ApplyShopProductFields(
            ScriptableObject asset,
            string productId,
            ShopProductType productType,
            SlotPairDeviceDefinitionSO deviceDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            // productId
            SerializedProperty productIdProp = so.FindProperty("productId");
            if (productIdProp != null)
                productIdProp.stringValue = productId;
            else
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] Missing field: productId on " + asset.name);
                allOk = false;
            }

            // productType
            SerializedProperty productTypeProp = so.FindProperty("productType");
            if (productTypeProp != null)
                productTypeProp.intValue = (int)productType;
            else
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] Missing field: productType on " + asset.name);
                allOk = false;
            }

            // deviceDefinition
            SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
            if (deviceDefProp != null)
                deviceDefProp.objectReferenceValue = deviceDefinition;
            else
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] Missing field: deviceDefinition on " + asset.name);
                allOk = false;
            }

            // diceTypeDefinition = null
            SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
            if (diceTypeDefProp != null)
                diceTypeDefProp.objectReferenceValue = null;

            // diceFaceUpgradeDefinition = null
            SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
            if (faceUpgradeDefProp != null)
                faceUpgradeDefProp.objectReferenceValue = null;

            // consumableDefinitionPlaceholder = null
            SerializedProperty consumableProp = so.FindProperty("consumableDefinitionPlaceholder");
            if (consumableProp != null)
                consumableProp.objectReferenceValue = null;

            // permanentUpgradeDefinitionPlaceholder = null
            SerializedProperty permanentProp = so.FindProperty("permanentUpgradeDefinitionPlaceholder");
            if (permanentProp != null)
                permanentProp.objectReferenceValue = null;

            // hpRepairDefinitionPlaceholder = null
            SerializedProperty hpRepairProp = so.FindProperty("hpRepairDefinitionPlaceholder");
            if (hpRepairProp != null)
                hpRepairProp.objectReferenceValue = null;

            if (!allOk)
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] Failed to set some fields on ShopProduct asset: " + AssetDatabase.GetAssetPath(asset));
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[ShopProductDeviceSOV44Generator] Applied ShopProduct fields: {AssetDatabase.GetAssetPath(asset)} productId={productId}");
        }

        // ── WorkshopRules 연결 ─────────────────────────────────────────

        /// <summary>생성된 Device ShopProduct를 Stage01_WorkshopRules의 Device 슬롯 productPool에 연결한다.</summary>
        private static void ConnectDeviceShopProductsToWorkshopRules(List<ShopProductDefinitionSO> allDeviceProducts)
        {
            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDeviceSOV44Generator] Stage01_WorkshopRules.asset을 찾을 수 없습니다: {WorkshopRulesPath}");
                return;
            }

            SerializedObject so = new SerializedObject(workshopRules);
            so.Update();

            SerializedProperty slotRulesProp = so.FindProperty("productSlotRules");

            if (slotRulesProp == null || !slotRulesProp.isArray)
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] productSlotRules를 찾을 수 없습니다.");
                return;
            }

            int slotCount = slotRulesProp.arraySize;

            if (slotCount < 6)
            {
                Debug.LogError($"[ShopProductDeviceSOV44Generator] productSlotRules count = {slotCount}, 기대값 6");
                return;
            }

            // Tier별로 ShopProduct 분류
            List<ShopProductDefinitionSO> tier1Products = new List<ShopProductDefinitionSO>();
            List<ShopProductDefinitionSO> tier2Products = new List<ShopProductDefinitionSO>();

            for (int i = 0; i < allDeviceProducts.Count; i++)
            {
                ShopProductDefinitionSO product = allDeviceProducts[i];

                if (product == null)
                    continue;

                // 연결된 Device의 Tier를 읽는다
                SlotPairDeviceDefinitionSO device = GetDeviceFromShopProduct(product);

                if (device == null)
                {
                    Debug.LogWarning($"[ShopProductDeviceSOV44Generator] ShopProduct {product.name}의 deviceDefinition이 null입니다. 건너뜁니다.");
                    continue;
                }

                int tier = ReadDeviceTier(device);

                if (tier <= 1)
                    tier1Products.Add(product);
                else
                    tier2Products.Add(product);
            }

            // Slot 0: Left Device - Tier 1만
            SerializedProperty slot0 = slotRulesProp.GetArrayElementAtIndex(0);
            SetProductPool(slot0, tier1Products.ToArray());

            // Slot 1: Right Device - Tier 1 + Tier 2
            List<ShopProductDefinitionSO> rightDevicePool = new List<ShopProductDefinitionSO>();
            rightDevicePool.AddRange(tier1Products);
            rightDevicePool.AddRange(tier2Products);
            SerializedProperty slot1 = slotRulesProp.GetArrayElementAtIndex(1);
            SetProductPool(slot1, rightDevicePool.ToArray());

            // Slot 2~5: 비움 유지 (DiceType / DiceFaceUpgrade)
            for (int i = 2; i < 6; i++)
            {
                SerializedProperty slot = slotRulesProp.GetArrayElementAtIndex(i);
                ClearProductPool(slot);
            }

            bool applied = so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(workshopRules);

            Debug.Log($"[ShopProductDeviceSOV44Generator] WorkshopRules 연결 완료: Left Device={tier1Products.Count}개, Right Device={rightDevicePool.Count}개");
        }

        /// <summary>ShopProduct에서 deviceDefinition 참조를 SerializedObject로 읽는다.</summary>
        private static SlotPairDeviceDefinitionSO GetDeviceFromShopProduct(ShopProductDefinitionSO product)
        {
            SerializedObject so = new SerializedObject(product);
            SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");

            if (deviceDefProp == null)
                return null;

            return deviceDefProp.objectReferenceValue as SlotPairDeviceDefinitionSO;
        }

        /// <summary>슬롯 SerializedProperty의 productPool 배열을 지정 상품 목록으로 설정한다.</summary>
        private static void SetProductPool(SerializedProperty slotProp, ShopProductDefinitionSO[] products)
        {
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");

            if (productPoolProp == null || !productPoolProp.isArray)
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] productPool 필드를 찾을 수 없습니다.");
                return;
            }

            productPoolProp.ClearArray();
            productPoolProp.arraySize = products.Length;

            for (int i = 0; i < products.Length; i++)
            {
                SerializedProperty elementProp = productPoolProp.GetArrayElementAtIndex(i);
                elementProp.objectReferenceValue = products[i];
            }
        }

        /// <summary>슬롯 SerializedProperty의 productPool 배열을 비운다.</summary>
        private static void ClearProductPool(SerializedProperty slotProp)
        {
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");

            if (productPoolProp == null || !productPoolProp.isArray)
                return;

            productPoolProp.ClearArray();
            productPoolProp.arraySize = 0;
        }

        // ── 레거시 보고 ─────────────────────────────────────────────────

        /// <summary>레거시 ShopProduct 후보를 Console에 보고한다.</summary>
        private static void ReportLegacyCandidates()
        {
            // v4.4 대상 Device 이름 목록
            HashSet<string> v44DeviceNames = new HashSet<string>
            {
                "Device_AdderChip", "Device_OddAmplifier", "Device_EvenAmplifier",
                "Device_ForceSpring", "Device_ImpactNail", "Device_HeavyHammer",
                "Device_SafetyPin", "Device_UnstableFuse", "Device_LeadWeight",
                "Device_LowGear", "Device_CastStampAces", "Device_CastStampChance",
                "Device_PairContact", "Device_LeftCoupler", "Device_RelayMotor",
                "Device_FrontLoader", "Device_EndValveLight", "Device_PressureGaugeLight",
                "Device_MirrorShard", "Device_IsolatedGear", "Device_OverdriveChip",
                "Device_FullHouseBracket", "Device_StraightRail", "Device_HighVoltagePin",
                "Device_EndValve", "Device_StagePressureMeter"
            };

            // v4.4 target path의 모든 ShopProduct 검색
            string[] shopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO", new[] { ShopDevicesPath });

            for (int i = 0; i < shopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shopProductGuids[i]);
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(path);

                if (product == null)
                    continue;

                SerializedObject so = new SerializedObject(product);
                SerializedProperty productTypeProp = so.FindProperty("productType");

                if (productTypeProp == null || productTypeProp.intValue != (int)ShopProductType.Device)
                    continue;

                SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
                SlotPairDeviceDefinitionSO deviceRef = deviceDefProp?.objectReferenceValue as SlotPairDeviceDefinitionSO;

                // deviceDefinition이 null인 경우
                if (deviceRef == null)
                {
                    Debug.Log($"[ShopProductDeviceSOV44Generator] [LegacyCandidate] Device ShopProduct with null deviceDefinition: {path}");
                    continue;
                }

                string deviceName = deviceRef.name;

                // 레거시 SlotPairDevice_* 테스트 SO인 경우
                if (deviceName.StartsWith("SlotPairDevice_"))
                {
                    Debug.Log($"[ShopProductDeviceSOV44Generator] [LegacyCandidate] Device ShopProduct linked to legacy test SO '{deviceName}': {path}");
                    continue;
                }

                // v4.4 대상에 없는 Device인 경우
                if (!v44DeviceNames.Contains(deviceName))
                {
                    Debug.Log($"[ShopProductDeviceSOV44Generator] [LegacyCandidate] Device ShopProduct linked to non-v4.4 device '{deviceName}': {path}");
                    continue;
                }
            }

            // v4.4 target path 외부의 ShopProduct_Device_* 검색
            string[] allShopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO");
            for (int i = 0; i < allShopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allShopProductGuids[i]);

                // v4.4 target path에 있으면 스킵
                if (path.StartsWith(ShopDevicesPath))
                    continue;

                string fileName = System.IO.Path.GetFileName(path);
                if (fileName.StartsWith("ShopProduct_Device_"))
                {
                    Debug.Log($"[ShopProductDeviceSOV44Generator] [LegacyCandidate] Device ShopProduct outside v4.4 target path: {path}");
                }
            }
        }

        // ── 검증 ────────────────────────────────────────────────────────

        /// <summary>생성된 모든 SO와 WorkshopRules 연결 상태를 검증한다.</summary>
        private static void ValidateGeneratedAssets()
        {
            bool shopProductValid = ValidateShopProducts();
            bool workshopValid = ValidateWorkshopRules();

            if (shopProductValid)
                Debug.Log("[ShopProductDeviceSOV44Generator] Device ShopProduct 검증 통과");

            if (workshopValid)
                Debug.Log("[ShopProductDeviceSOV44Generator] WorkshopRules Device productPool 검증 통과");
        }

        /// <summary>Device ShopProduct 26종의 필드를 검증한다.</summary>
        private static bool ValidateShopProducts()
        {
            bool allPassed = true;

            // v4.4 대상 Device SO 경로 목록
            string[] devicePaths = new string[]
            {
                DevicesCommonPath + "/Device_AdderChip.asset",
                DevicesCommonPath + "/Device_OddAmplifier.asset",
                DevicesCommonPath + "/Device_EvenAmplifier.asset",
                DevicesCommonPath + "/Device_ForceSpring.asset",
                DevicesCommonPath + "/Device_ImpactNail.asset",
                DevicesCommonPath + "/Device_HeavyHammer.asset",
                DevicesCommonPath + "/Device_SafetyPin.asset",
                DevicesCommonPath + "/Device_UnstableFuse.asset",
                DevicesCommonPath + "/Device_LeadWeight.asset",
                DevicesCommonPath + "/Device_LowGear.asset",
                DevicesCommonPath + "/Device_CastStampAces.asset",
                DevicesCommonPath + "/Device_CastStampChance.asset",
                DevicesCommonPath + "/Device_PairContact.asset",
                DevicesCommonPath + "/Device_LeftCoupler.asset",
                DevicesCommonPath + "/Device_RelayMotor.asset",
                DevicesCommonPath + "/Device_FrontLoader.asset",
                DevicesCommonPath + "/Device_EndValveLight.asset",
                DevicesCommonPath + "/Device_PressureGaugeLight.asset",
                DevicesRarePath + "/Device_MirrorShard.asset",
                DevicesRarePath + "/Device_IsolatedGear.asset",
                DevicesRarePath + "/Device_OverdriveChip.asset",
                DevicesRarePath + "/Device_FullHouseBracket.asset",
                DevicesRarePath + "/Device_StraightRail.asset",
                DevicesRarePath + "/Device_HighVoltagePin.asset",
                DevicesRarePath + "/Device_EndValve.asset",
                DevicesRarePath + "/Device_StagePressureMeter.asset",
            };

            // 레거시 테스트 Device 이름 목록 (생성되면 안 됨)
            HashSet<string> legacyTestDeviceNames = new HashSet<string>
            {
                "SlotPairDevice_AddScore_DiceValueX2",
                "SlotPairDevice_AddForce_Flat1",
                "SlotPairDevice_AddScore_Flat4",
                "SlotPairDevice_ForceOver4_X1_5",
                "SlotPairDevice_SamePreviousForce1"
            };

            foreach (string devicePath in devicePaths)
            {
                SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(devicePath);

                if (device == null)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Device SO를 찾을 수 없습니다: {devicePath}");
                    allPassed = false;
                    continue;
                }

                // ShopProduct 경로 계산
                string fileName = System.IO.Path.GetFileName(devicePath); // Device_XXX.asset
                string shopProductFileName = "ShopProduct_" + fileName;
                string shopProductPath = ShopDevicesPath + "/" + shopProductFileName;

                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(shopProductPath);

                if (shopProduct == null)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: ShopProduct를 찾을 수 없습니다: {shopProductPath}");
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(shopProduct);

                // productType 검증
                SerializedProperty productTypeProp = so.FindProperty("productType");
                if (productTypeProp == null || productTypeProp.intValue != (int)ShopProductType.Device)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath} productType = {productTypeProp?.intValue}, 기대값 {(int)ShopProductType.Device}");
                    allPassed = false;
                }

                // deviceDefinition 검증
                SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
                if (deviceDefProp == null || deviceDefProp.objectReferenceValue == null)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath} deviceDefinition이 null입니다.");
                    allPassed = false;
                }
                else
                {
                    // 레거시 테스트 SO 참조 확인
                    Object referencedDevice = deviceDefProp.objectReferenceValue;
                    if (legacyTestDeviceNames.Contains(referencedDevice.name))
                    {
                        Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath}가 레거시 테스트 SO '{referencedDevice.name}'를 참조합니다.");
                        allPassed = false;
                    }
                }

                // productId 검증
                SerializedProperty productIdProp = so.FindProperty("productId");
                if (productIdProp == null || string.IsNullOrWhiteSpace(productIdProp.stringValue))
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath} productId가 비어 있습니다.");
                    allPassed = false;
                }

                // diceTypeDefinition은 null이어야 함
                SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
                if (diceTypeDefProp != null && diceTypeDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath} diceTypeDefinition이 null이 아닙니다.");
                    allPassed = false;
                }

                // diceFaceUpgradeDefinition은 null이어야 함
                SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
                if (faceUpgradeDefProp != null && faceUpgradeDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: {shopProductPath} diceFaceUpgradeDefinition이 null이 아닙니다.");
                    allPassed = false;
                }
            }

            return allPassed;
        }

        /// <summary>Stage01_WorkshopRules의 Device productPool 연결 상태를 검증한다.</summary>
        private static bool ValidateWorkshopRules()
        {
            bool allPassed = true;

            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Stage01_WorkshopRules.asset이 존재하지 않습니다: {WorkshopRulesPath}");
                return false;
            }

            System.Collections.Generic.IReadOnlyList<ShopProductSlotRule> slotRules = workshopRules.ProductSlotRules;

            if (slotRules == null || slotRules.Count != 6)
            {
                Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: ProductSlotRules count = {(slotRules != null ? slotRules.Count : 0)}, 기대값 6");
                return false;
            }

            // Tier 1 Device 수 계산
            int tier1Count = 0;
            int tier2Count = 0;

            string[] commonDevicePaths = new string[]
            {
                DevicesCommonPath + "/Device_AdderChip.asset",
                DevicesCommonPath + "/Device_OddAmplifier.asset",
                DevicesCommonPath + "/Device_EvenAmplifier.asset",
                DevicesCommonPath + "/Device_ForceSpring.asset",
                DevicesCommonPath + "/Device_ImpactNail.asset",
                DevicesCommonPath + "/Device_HeavyHammer.asset",
                DevicesCommonPath + "/Device_SafetyPin.asset",
                DevicesCommonPath + "/Device_UnstableFuse.asset",
                DevicesCommonPath + "/Device_LeadWeight.asset",
                DevicesCommonPath + "/Device_LowGear.asset",
                DevicesCommonPath + "/Device_CastStampAces.asset",
                DevicesCommonPath + "/Device_CastStampChance.asset",
                DevicesCommonPath + "/Device_PairContact.asset",
                DevicesCommonPath + "/Device_LeftCoupler.asset",
                DevicesCommonPath + "/Device_RelayMotor.asset",
                DevicesCommonPath + "/Device_FrontLoader.asset",
                DevicesCommonPath + "/Device_EndValveLight.asset",
                DevicesCommonPath + "/Device_PressureGaugeLight.asset",
            };

            string[] rareDevicePaths = new string[]
            {
                DevicesRarePath + "/Device_MirrorShard.asset",
                DevicesRarePath + "/Device_IsolatedGear.asset",
                DevicesRarePath + "/Device_OverdriveChip.asset",
                DevicesRarePath + "/Device_FullHouseBracket.asset",
                DevicesRarePath + "/Device_StraightRail.asset",
                DevicesRarePath + "/Device_HighVoltagePin.asset",
                DevicesRarePath + "/Device_EndValve.asset",
                DevicesRarePath + "/Device_StagePressureMeter.asset",
            };

            // Common = Tier 1
            tier1Count = commonDevicePaths.Length;

            // Rare = Tier 2
            tier2Count = rareDevicePaths.Length;

            // Slot 0: Left Device - Tier 1만
            ShopProductDefinitionSO[] slot0Pool = slotRules[0].ProductPool;
            if (slot0Pool == null)
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] 검증 실패: Slot 0 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot0Pool.Length != tier1Count)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Slot 0 productPool count = {slot0Pool.Length}, 기대값 {tier1Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot0Pool.Length; i++)
                {
                    if (slot0Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Slot 0 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }
            }

            // Slot 1: Right Device - Tier 1 + Tier 2
            ShopProductDefinitionSO[] slot1Pool = slotRules[1].ProductPool;
            int expectedSlot1Count = tier1Count + tier2Count;
            if (slot1Pool == null)
            {
                Debug.LogError("[ShopProductDeviceSOV44Generator] 검증 실패: Slot 1 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot1Pool.Length != expectedSlot1Count)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Slot 1 productPool count = {slot1Pool.Length}, 기대값 {expectedSlot1Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot1Pool.Length; i++)
                {
                    if (slot1Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Slot 1 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }
            }

            // Slot 2~5: productPool count == 0
            for (int i = 2; i < 6; i++)
            {
                ShopProductDefinitionSO[] pool = slotRules[i].ProductPool;
                int poolCount = (pool != null) ? pool.Length : -1;

                if (poolCount != 0)
                {
                    Debug.LogError($"[ShopProductDeviceSOV44Generator] 검증 실패: Slot {i} productPool count = {poolCount}, 기대값 0");
                    allPassed = false;
                }
            }

            return allPassed;
        }
    }
}
