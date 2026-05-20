using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>Shop 테스트용 ScriptableObject 에셋을 자동 생성/재생성하는 Editor 유틸리티.</summary>
    public static class TesseraShopTestAssetGenerator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string DevicesFolderPath = "Assets/_Project/ScriptableObjects/Devices";
        private const string ShopFolderPath = "Assets/_Project/ScriptableObjects/Shop";

        // ── Device 에셋 경로 상수 ────────────────────────────────────────
        private const string DeviceAssetPathA = DevicesFolderPath + "/SlotPairDevice_AddScore_DiceValueX2.asset";
        private const string DeviceAssetPathB = DevicesFolderPath + "/SlotPairDevice_SamePreviousForce1.asset";
        private const string DeviceAssetPathC = DevicesFolderPath + "/SlotPairDevice_ForceOver4_X1_5.asset";

        // ── Shop Product 에셋 경로 상수 ──────────────────────────────────
        private const string ShopAssetPathA = ShopFolderPath + "/ShopProduct_Device_AddScore_DiceValueX2.asset";
        private const string ShopAssetPathB = ShopFolderPath + "/ShopProduct_Device_SamePreviousForce1.asset";
        private const string ShopAssetPathC = ShopFolderPath + "/ShopProduct_Device_ForceOver4_X1_5.asset";

        /// <summary>Tools > Tessera > Test Assets > Rebuild Shop Test Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Test Assets/Rebuild Shop Test Assets")]
        private static void RebuildShopTestAssets()
        {
            // 폴더가 없으면 생성
            EnsureFoldersExist();

            // 기존 에셋 삭제
            DeleteExistingAssets();

            // 새 에셋 생성
            CreateFreshAssets();

            // 저장, 리프레시, 선택, 로그
            FinalizeCreation();
        }

        /// <summary>대상 폴더 2개가 없으면 생성한다.</summary>
        private static void EnsureFoldersExist()
        {
            EnsureFolderExists(DevicesFolderPath, "Assets/_Project/ScriptableObjects", "Devices");
            EnsureFolderExists(ShopFolderPath, "Assets/_Project/ScriptableObjects", "Shop");
        }

        /// <summary>지정한 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
                Debug.Log($"[TesseraShopTestAssetGenerator] Created folder: {fullPath}");
            }
        }

        /// <summary>기존 Device 에셋 3종과 Shop Product 에셋 3종을 모두 삭제한다.</summary>
        private static void DeleteExistingAssets()
        {
            DeleteAssetIfExists(DeviceAssetPathA);
            DeleteAssetIfExists(DeviceAssetPathB);
            DeleteAssetIfExists(DeviceAssetPathC);
            DeleteAssetIfExists(ShopAssetPathA);
            DeleteAssetIfExists(ShopAssetPathB);
            DeleteAssetIfExists(ShopAssetPathC);
        }

        /// <summary>지정 경로에 에셋이 있으면 삭제하고, 실패 시 에러 로그를 출력한다.</summary>
        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                bool deleted = AssetDatabase.DeleteAsset(path);
                if (deleted)
                {
                    Debug.Log($"[TesseraShopTestAssetGenerator] Deleted existing asset: {path}");
                }
                else
                {
                    Debug.LogError($"[TesseraShopTestAssetGenerator] Failed to delete asset: {path}");
                }
            }
        }

        /// <summary>Device 에셋 3종과 Shop Product 에셋 3종을 새로 생성한다.</summary>
        private static void CreateFreshAssets()
        {
            // Device 에셋 생성
            SlotPairDeviceDefinitionSO deviceA = CreateDeviceAssetA();
            SlotPairDeviceDefinitionSO deviceB = CreateDeviceAssetB();
            SlotPairDeviceDefinitionSO deviceC = CreateDeviceAssetC();

            // Shop Product 에셋 생성 (Device 참조 전달)
            CreateShopAssetA(deviceA);
            CreateShopAssetB(deviceB);
            CreateShopAssetC(deviceC);
        }

        /// <summary>Device 에셋 A: Dice Value Doubler (AddScoreByDiceValue, intValue=2)를 생성한다.</summary>
        private static SlotPairDeviceDefinitionSO CreateDeviceAssetA()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, DeviceAssetPathA);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.add_score.dice_value_x2";
            so.FindProperty("displayName").stringValue = "Dice Value Doubler";
            so.FindProperty("description").stringValue = "Adds current dice value x2 to Score.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.AddScoreByDiceValue;
            so.FindProperty("intValue").intValue = 2;
            so.FindProperty("floatValue").floatValue = 1f;
            so.FindProperty("forceThreshold").floatValue = 0f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>Device 에셋 B: Echo Gear (AddForceIfSameAsPrevious, intValue=1)를 생성한다.</summary>
        private static SlotPairDeviceDefinitionSO CreateDeviceAssetB()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, DeviceAssetPathB);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.same_previous.force_1";
            so.FindProperty("displayName").stringValue = "Echo Gear";
            so.FindProperty("description").stringValue = "Adds Force +1 if current dice value equals previous slot dice value.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.AddForceIfSameAsPrevious;
            so.FindProperty("intValue").intValue = 1;
            so.FindProperty("floatValue").floatValue = 1f;
            so.FindProperty("forceThreshold").floatValue = 0f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>Device 에셋 C: Pressure Amplifier (MultiplyForceIfCurrentForceAtLeast, floatValue=1.5, forceThreshold=4)를 생성한다.</summary>
        private static SlotPairDeviceDefinitionSO CreateDeviceAssetC()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, DeviceAssetPathC);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.force_over_4.x1_5";
            so.FindProperty("displayName").stringValue = "Pressure Amplifier";
            so.FindProperty("description").stringValue = "Multiplies Force by 1.5 if current Force is at least 4.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast;
            so.FindProperty("intValue").intValue = 0;
            so.FindProperty("floatValue").floatValue = 1.5f;
            so.FindProperty("forceThreshold").floatValue = 4f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>Shop Product 에셋 A: Dice Value Doubler (price=8, slotPairDevice=deviceA)를 생성한다.</summary>
        private static void CreateShopAssetA(SlotPairDeviceDefinitionSO deviceA)
        {
            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, ShopAssetPathA);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("productId").stringValue = "shop.device.add_score.dice_value_x2";
            so.FindProperty("displayName").stringValue = "Dice Value Doubler";
            so.FindProperty("description").stringValue = "Adds current dice value x2 to Score.";
            so.FindProperty("price").intValue = 8;
            so.FindProperty("productType").enumValueIndex = (int)ShopProductType.SlotPairDevice;
            so.FindProperty("slotPairDevice").objectReferenceValue = deviceA;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>Shop Product 에셋 B: Echo Gear (price=10, slotPairDevice=deviceB)를 생성한다.</summary>
        private static void CreateShopAssetB(SlotPairDeviceDefinitionSO deviceB)
        {
            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, ShopAssetPathB);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("productId").stringValue = "shop.device.same_previous.force_1";
            so.FindProperty("displayName").stringValue = "Echo Gear";
            so.FindProperty("description").stringValue = "Adds Force +1 if current dice value equals previous slot dice value.";
            so.FindProperty("price").intValue = 10;
            so.FindProperty("productType").enumValueIndex = (int)ShopProductType.SlotPairDevice;
            so.FindProperty("slotPairDevice").objectReferenceValue = deviceB;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>Shop Product 에셋 C: Pressure Amplifier (price=12, slotPairDevice=deviceC)를 생성한다.</summary>
        private static void CreateShopAssetC(SlotPairDeviceDefinitionSO deviceC)
        {
            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, ShopAssetPathC);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("productId").stringValue = "shop.device.force_over_4.x1_5";
            so.FindProperty("displayName").stringValue = "Pressure Amplifier";
            so.FindProperty("description").stringValue = "Multiplies Force by 1.5 if current Force is at least 4.";
            so.FindProperty("price").intValue = 12;
            so.FindProperty("productType").enumValueIndex = (int)ShopProductType.SlotPairDevice;
            so.FindProperty("slotPairDevice").objectReferenceValue = deviceC;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>에셋 저장, 리프레시, ShopProduct_AddScore_DiceValueX2 선택, 로그 출력을 수행한다.</summary>
        private static void FinalizeCreation()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 생성된 Shop Product A를 Project Window에서 선택
            Object shopAssetA = AssetDatabase.LoadAssetAtPath<Object>(ShopAssetPathA);
            if (shopAssetA != null)
            {
                Selection.activeObject = shopAssetA;
                EditorGUIUtility.PingObject(shopAssetA);
            }

            Debug.Log("[TesseraShopTestAssetGenerator] 6 test assets (3 devices + 3 shop products) recreated successfully.");
        }
    }
}
