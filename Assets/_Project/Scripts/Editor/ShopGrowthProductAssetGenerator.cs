using System;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>Shop 성장 상품 v1 ScriptableObject를 자동 생성/업데이트하는 Editor 유틸리티.</summary>
    public static class ShopGrowthProductAssetGenerator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";

        private const string DevicesCommonPath = RootPath + "/Devices/ShopGrowth/Common";
        private const string DevicesRarePath = RootPath + "/Devices/ShopGrowth/Rare";
        private const string DiceTypesPath = RootPath + "/DiceTypes";
        private const string DiceSynergiesPath = RootPath + "/DiceSynergies";
        private const string DiceFaceUpgradesPath = RootPath + "/DiceFaceUpgrades";
        private const string ShopDevicesPath = RootPath + "/Shop/Generated/Devices";
        private const string ShopDiceTypesPath = RootPath + "/Shop/Generated/DiceTypes";
        private const string ShopDiceFaceUpgradesPath = RootPath + "/Shop/Generated/DiceFaceUpgrades";

        // ── 기존 Device 에셋 경로 ────────────────────────────────────────
        private const string LegacyDevicePath = RootPath + "/Devices";

        private const string LegacyDeviceA = LegacyDevicePath + "/SlotPairDevice_AddScore_DiceValueX2.asset";
        private const string LegacyDeviceB = LegacyDevicePath + "/SlotPairDevice_AddForce_Flat1.asset";
        private const string LegacyDeviceC = LegacyDevicePath + "/SlotPairDevice_AddScore_Flat4.asset";
        private const string LegacyDeviceD = LegacyDevicePath + "/SlotPairDevice_ForceOver4_X1_5.asset";
        private const string LegacyDeviceE = LegacyDevicePath + "/SlotPairDevice_SamePreviousForce1.asset";

        // ── 기존 ShopProduct 에셋 경로 ───────────────────────────────────
        private const string LegacyShopPath = RootPath + "/Shop";

        private const string LegacyShopA = LegacyShopPath + "/ShopProduct_Device_DiceValueDoubler.asset";
        private const string LegacyShopB = LegacyShopPath + "/ShopProduct_Device_FlatScorer.asset";
        private const string LegacyShopC = LegacyShopPath + "/ShopProduct_Device_ForceCell.asset";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Generate Shop Growth Product Assets v1 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Shop Growth Product Assets v1")]
        private static void GenerateAllAssets()
        {
            _createdCount = 0;
            _updatedCount = 0;

            // 1. 필요한 폴더 생성
            EnsureAllFoldersExist();

            // 2. 기존 Device SO 복구
            RestoreLegacyDevices();

            // 3. Device SO 20종 생성/업데이트
            CreateAllDevices();

            // 4. DiceType SO 7종 생성/업데이트
            CreateAllDiceTypes();

            // 5. DiceSynergy SO 7종 생성/업데이트
            CreateAllDiceSynergies();

            // 6. DiceFaceUpgrade SO 11종 생성/업데이트
            CreateAllDiceFaceUpgrades();

            // 7. ShopProduct SO 생성/업데이트
            CreateAllShopProducts();

            // 8. 기존 ShopProduct SO 복구
            RestoreLegacyShopProducts();

            // 9. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 10. 검증 실행
            VerifyGeneratedAssets();

            // 11. 결과 출력
            Debug.Log($"[ShopGrowthProductAssetGenerator] Complete. Created: {_createdCount}, Updated: {_updatedCount}");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>필요한 모든 폴더를 생성한다.</summary>
        private static void EnsureAllFoldersExist()
        {
            EnsureFolder(RootPath + "/Devices", "ShopGrowth");
            EnsureFolder(RootPath + "/Devices/ShopGrowth", "Common");
            EnsureFolder(RootPath + "/Devices/ShopGrowth", "Rare");
            EnsureFolder(RootPath, "DiceTypes");
            EnsureFolder(RootPath, "DiceSynergies");
            EnsureFolder(RootPath, "DiceFaceUpgrades");
            EnsureFolder(RootPath + "/Shop", "Generated");
            EnsureFolder(RootPath + "/Shop/Generated", "Devices");
            EnsureFolder(RootPath + "/Shop/Generated", "DiceTypes");
            EnsureFolder(RootPath + "/Shop/Generated", "DiceFaceUpgrades");
        }

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[ShopGrowthProductAssetGenerator] Created folder: {path}");
            }
        }

        // ── 헬퍼: 폴더 보장 ────────────────────────────────────────────

        /// <summary>전체 폴더 경로를 재귀적으로 보장한다.</summary>
        private static void EnsureFolder(string folderPath)
        {
            // 이미 존재하면 스킵
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            // 부모 경로를 먼저 보장한 후, 마지막 폴더명으로 CreateFolder 호출
            int lastSlash = folderPath.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                Debug.LogError($"[ShopGrowthProductAssetGenerator] Invalid folder path: {folderPath}");
                return;
            }

            string parent = folderPath.Substring(0, lastSlash);
            string folderName = folderPath.Substring(lastSlash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Created folder: {folderPath}");
        }

        // ── 헬퍼: LoadOrCreateAsset ────────────────────────────────────

        /// <summary>지정 경로에 에셋이 있으면 로드하고, 없으면 새로 생성한다.</summary>
        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                _updatedCount++;
                Debug.Log($"[ShopGrowthProductAssetGenerator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[ShopGrowthProductAssetGenerator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        /// <summary>SerializedObject에서 string 필드를 설정한다.</summary>
        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.stringValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 int 필드를 설정한다.</summary>
        private static bool SetInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.intValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 float 필드를 설정한다.</summary>
        private static bool SetFloat(SerializedObject so, string fieldName, float value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.floatValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 bool 필드를 설정한다.</summary>
        private static bool SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.boolValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 enum 필드를 안전하게 설정한다.</summary>
        private static bool SetEnum<TEnum>(SerializedObject so, string fieldName, TEnum value) where TEnum : Enum
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            // enumNames 배열에서 일치하는 이름을 찾아 enumValueIndex로 설정
            string enumName = value.ToString();
            int index = Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.enumValueIndex = index;
            return true;
        }

        /// <summary>SerializedObject에서 UnityEngine.Object 참조 필드를 설정한다.</summary>
        private static bool SetObject(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.objectReferenceValue = value;
            return true;
        }

        /// <summary>SerializedObject 변경사항을 적용하고 Dirty 플래그를 설정한다.</summary>
        private static void ApplyAndDirty(SerializedObject so, UnityEngine.Object asset)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        // ── 기존 Device SO 복구 ────────────────────────────────────────

        /// <summary>기존 5종 Device SO를 복구한다.</summary>
        private static void RestoreLegacyDevices()
        {
            // 1. SlotPairDevice_AddScore_DiceValueX2
            RestoreOrCreateDeviceAsset(
                LegacyDeviceA,
                "device.add_score.dice_value_x2",
                "Dice Value Doubler",
                "Adds current dice value x2 to Score.",
                SlotPairDeviceType.AddScoreByDiceValue,
                2, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            // 2. SlotPairDevice_AddForce_Flat1
            RestoreOrCreateDeviceAsset(
                LegacyDeviceB,
                "device.force.flat_1",
                "Flat Force Cell",
                "Legacy flat force device. Currently mapped to included dice condition.",
                SlotPairDeviceType.AddForceIfDiceIncluded,
                1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            // 3. SlotPairDevice_AddScore_Flat4
            RestoreOrCreateDeviceAsset(
                LegacyDeviceC,
                "device.score.flat_4",
                "Flat Scorer",
                "Legacy flat score device. Currently mapped to any dice condition.",
                SlotPairDeviceType.AddScoreIfDiceValueAtLeast,
                4, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            // 4. SlotPairDevice_ForceOver4_X1_5
            RestoreOrCreateDeviceAsset(
                LegacyDeviceD,
                "device.force_over_4_x1_5",
                "StageThreat Amplifier",
                "Multiplies Force by 1.5 if current Force is at least 4.",
                SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast,
                0, 1.5f, 4f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            // 5. SlotPairDevice_SamePreviousForce1
            RestoreOrCreateDeviceAsset(
                LegacyDeviceE,
                "device.force.same_previous_1",
                "Echo Gear",
                "Adds Force +1 if this dice matches the previous slot dice.",
                SlotPairDeviceType.AddForceIfSameAsPrevious,
                1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);
        }

        /// <summary>기존 Device SO가 없으면 생성하고, 있으면 필드를 업데이트한다.</summary>
        private static void RestoreOrCreateDeviceAsset(
            string path,
            string deviceId,
            string displayName,
            string description,
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            RollPatternType secondaryPatternType,
            DiceValueParity requiredParity,
            int requiredMinDiceValue,
            int requiredMaxDiceValue,
            int requiredSlotIndex,
            int requiredStageThreatLevel,
            int trueDamageValue)
        {
            SlotPairDeviceDefinitionSO asset = LoadOrCreateAsset<SlotPairDeviceDefinitionSO>(path);
            ApplyDeviceFields(asset, deviceId, displayName, description, deviceType,
                intValue, floatValue, forceThreshold,
                requiredPatternType, secondaryPatternType, requiredParity,
                requiredMinDiceValue, requiredMaxDiceValue, requiredSlotIndex,
                requiredStageThreatLevel, trueDamageValue);
        }

        // ── Device SO 생성 ─────────────────────────────────────────────

        /// <summary>20종의 Device SO를 생성/업데이트한다.</summary>
        private static void CreateAllDevices()
        {
            // Common 12종
            CreateDevice(DevicesCommonPath + "/Device_SafetyPin.asset",
                "device.safety_pin", "안전 핀",
                "현재 슬롯 주사위가 짝수면 Score +12.",
                SlotPairDeviceType.AddScoreIfDiceParity, 12, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Even, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_UnstableFuse.asset",
                "device.unstable_fuse", "불안정한 도화선",
                "현재 슬롯 주사위가 홀수면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceParity, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Odd, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_LeadWeight.asset",
                "device.lead_weight", "납 추",
                "현재 슬롯 주사위가 5 이상이면 Score +15.",
                SlotPairDeviceType.AddScoreIfDiceValueAtLeast, 15, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 5, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_LowGear.asset",
                "device.low_gear", "저단 기어",
                "현재 슬롯 주사위가 2 이하이면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceValueAtMost, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 2, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_CastStampAces.asset",
                "device.cast_stamp_aces", "에이스 스탬프",
                "Aces 제출 시 Score +20.",
                SlotPairDeviceType.AddScoreIfCastType, 20, 1f, 0f,
                RollPatternType.Aces, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_CastStampChance.asset",
                "device.cast_stamp_chance", "찬스 스탬프",
                "Chance 제출 시 Score +15.",
                SlotPairDeviceType.AddScoreIfCastType, 15, 1f, 0f,
                RollPatternType.Chance, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_PairContact.asset",
                "device.pair_contact", "접점 단자",
                "현재 주사위가 Cast 계산값에 포함되면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceIncluded, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_LeftCoupler.asset",
                "device.left_coupler", "좌측 커플러",
                "이전 슬롯과 같은 눈이면 Force +1.",
                SlotPairDeviceType.AddForceIfSameAsPrevious, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_RelayMotor.asset",
                "device.relay_motor", "릴레이 모터",
                "이전 슬롯보다 현재 눈이 크면 Force x1.5.",
                SlotPairDeviceType.MultiplyForceIfGreaterThanPrevious, 0, 1.5f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_FrontLoader.asset",
                "device.front_loader", "전방 장전기",
                "1번 슬롯이면 Score +18.",
                SlotPairDeviceType.AddScoreIfSlotIndex, 18, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, 0, 0, 0);

            CreateDevice(DevicesCommonPath + "/Device_EndValveLight.asset",
                "device.end_valve_light", "소형 엔드 밸브",
                "5번 슬롯이면 True Damage +15.",
                SlotPairDeviceType.AddTrueDamageIfSlotIndex, 0, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, 4, 0, 15);

            CreateDevice(DevicesCommonPath + "/Device_PressureGaugeLight.asset",
                "device.pressure_gauge_light", "소형 압력계",
                "StageThreat 1 이상이면 Score +10.",
                SlotPairDeviceType.AddScoreIfStageThreatAtLeast, 10, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 1, 0);

            // Rare 8종
            CreateDevice(DevicesRarePath + "/Device_MirrorShard.asset",
                "device.mirror_shard", "거울 조각",
                "대칭 슬롯과 눈이 같으면 Force +2.",
                SlotPairDeviceType.AddForceIfSameAsMirrorSlot, 2, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_IsolatedGear.asset",
                "device.isolated_gear", "고립된 톱니",
                "양옆 슬롯 주사위와 현재 주사위의 차이가 각각 2 이상이면 Score +25.",
                SlotPairDeviceType.AddScoreIfIsolatedFromNeighbors, 25, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_OverdriveChip.asset",
                "device.overdrive_chip", "오버드라이브 칩",
                "Tessera 제출 시 Force x1.5.",
                SlotPairDeviceType.MultiplyForceIfCastType, 0, 1.5f, 0f,
                RollPatternType.Tessera, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_FullHouseBracket.asset",
                "device.fullhouse_bracket", "풀하우스 브래킷",
                "Full House 제출 시 Score +30.",
                SlotPairDeviceType.AddScoreIfCastType, 30, 1f, 0f,
                RollPatternType.FullHouse, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_StraightRail.asset",
                "device.straight_rail", "스트레이트 레일",
                "Small Straight 또는 Large Straight 제출 시 Force +1.",
                SlotPairDeviceType.AddForceIfCastTypeEither, 1, 1f, 0f,
                RollPatternType.SmallStraight, RollPatternType.LargeStraight,
                DiceValueParity.Any, 1, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_HighVoltagePin.asset",
                "device.high_voltage_pin", "고전압 핀",
                "현재 슬롯 주사위가 6이면 Force x2.",
                SlotPairDeviceType.MultiplyForceIfDiceValueAtLeast, 0, 2f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 6, 6, -1, 0, 0);

            CreateDevice(DevicesRarePath + "/Device_EndValve.asset",
                "device.end_valve", "엔드 밸브",
                "앞 슬롯 합이 20 이상이면 True Damage +35.",
                SlotPairDeviceType.AddTrueDamageIfPreviousSlotsSumAtLeast, 20, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 35);

            CreateDevice(DevicesRarePath + "/Device_StagePressureMeter.asset",
                "device.stage_pressure_meter", "압력계",
                "StageThreat 2 이상이면 Force +1.",
                SlotPairDeviceType.AddForceIfStageThreatAtLeast, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 2, 0);
        }

        /// <summary>Device SO를 생성하거나 업데이트한다.</summary>
        private static void CreateDevice(
            string path,
            string deviceId,
            string displayName,
            string description,
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            RollPatternType secondaryPatternType,
            DiceValueParity requiredParity,
            int requiredMinDiceValue,
            int requiredMaxDiceValue,
            int requiredSlotIndex,
            int requiredStageThreatLevel,
            int trueDamageValue)
        {
            SlotPairDeviceDefinitionSO asset = LoadOrCreateAsset<SlotPairDeviceDefinitionSO>(path);
            ApplyDeviceFields(asset, deviceId, displayName, description, deviceType,
                intValue, floatValue, forceThreshold,
                requiredPatternType, secondaryPatternType, requiredParity,
                requiredMinDiceValue, requiredMaxDiceValue, requiredSlotIndex,
                requiredStageThreatLevel, trueDamageValue);
        }

        /// <summary>SerializedObject를 통해 Device SO 필드를 설정한다.</summary>
        private static void ApplyDeviceFields(
            ScriptableObject asset,
            string deviceId,
            string displayName,
            string description,
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            RollPatternType secondaryPatternType,
            DiceValueParity requiredParity,
            int requiredMinDiceValue,
            int requiredMaxDiceValue,
            int requiredSlotIndex,
            int requiredStageThreatLevel,
            int trueDamageValue)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "deviceId", deviceId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetEnum(so, "deviceType", deviceType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);
            allOk &= SetFloat(so, "forceThreshold", forceThreshold);
            allOk &= SetEnum(so, "requiredPatternType", requiredPatternType);
            allOk &= SetEnum(so, "secondaryPatternType", secondaryPatternType);
            allOk &= SetEnum(so, "requiredParity", requiredParity);
            allOk &= SetInt(so, "requiredMinDiceValue", requiredMinDiceValue);
            allOk &= SetInt(so, "requiredMaxDiceValue", requiredMaxDiceValue);
            allOk &= SetInt(so, "requiredSlotIndex", requiredSlotIndex);
            allOk &= SetInt(so, "requiredStageThreatLevel", requiredStageThreatLevel);
            allOk &= SetInt(so, "trueDamageValue", trueDamageValue);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on Device asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied Device fields: {AssetDatabase.GetAssetPath(asset)} deviceId={deviceId}");
        }

        // ── DiceType SO 생성 ───────────────────────────────────────────

        /// <summary>7종의 DiceType SO를 생성/업데이트한다.</summary>
        private static void CreateAllDiceTypes()
        {
            CreateDiceType(DiceTypesPath + "/DiceType_Red.asset",
                "dice.red", "Red Dice",
                "Adds Score +2 when an odd result is used in SlotPair calculation.",
                Color.red, "dice.red",
                DiceSynergyTag.Red, DiceIntrinsicEffectType.AddScoreIfOdd,
                2, 0f, 1, 1, 5);

            CreateDiceType(DiceTypesPath + "/DiceType_Blue.asset",
                "dice.blue", "Blue Dice",
                "Adds Force +0.2 when an even result is used in SlotPair calculation.",
                Color.blue, "dice.blue",
                DiceSynergyTag.Blue, DiceIntrinsicEffectType.AddForceIfEven,
                0, 0.2f, 1, 1, 5);

            CreateDiceType(DiceTypesPath + "/DiceType_Iron.asset",
                "dice.iron", "Iron Dice",
                "Adds Score +3 when a result of 5 or higher is used in SlotPair calculation.",
                Color.gray, "dice.iron",
                DiceSynergyTag.Iron, DiceIntrinsicEffectType.AddScoreIfValueAtLeast,
                3, 0f, 1, 1, 5);

            CreateDiceType(DiceTypesPath + "/DiceType_Broken.asset",
                "dice.broken", "Broken Dice",
                "Candidate for Overcharge compensation when submitting Broken Cast.",
                Color.magenta, "dice.broken",
                DiceSynergyTag.Broken, DiceIntrinsicEffectType.AddOverchargeOnBrokenCast,
                1, 0f, 1, 1, 6);

            CreateDiceType(DiceTypesPath + "/DiceType_Gold.asset",
                "dice.gold", "Gold Dice",
                "Candidate for earning Money +1 when winning a Round with this dice in the Cast.",
                Color.yellow, "dice.gold",
                DiceSynergyTag.Gold, DiceIntrinsicEffectType.AddMoneyOnRoundWinIfUsed,
                1, 0f, 2, 2, 7);

            CreateDiceType(DiceTypesPath + "/DiceType_Green.asset",
                "dice.green", "Green Dice",
                "Candidate for adding Score +2 when a result of 3 or lower is used in SlotPair calculation.",
                Color.green, "dice.green",
                DiceSynergyTag.Green, DiceIntrinsicEffectType.AddScoreIfValueAtMost,
                2, 0f, 1, 1, 5);

            CreateDiceType(DiceTypesPath + "/DiceType_Void.asset",
                "dice.void", "Void Dice",
                "Candidate for reducing incoming damage when this dice is included in a lost Clash.",
                Color.black, "dice.void",
                DiceSynergyTag.Void, DiceIntrinsicEffectType.ReduceIncomingDamageIfUsed,
                2, 0f, 2, 2, 7);
        }

        /// <summary>DiceType SO를 생성하거나 업데이트한다.</summary>
        private static void CreateDiceType(
            string path,
            string diceTypeId,
            string displayName,
            string description,
            Color visualColor,
            string materialKey,
            DiceSynergyTag synergyTag,
            DiceIntrinsicEffectType intrinsicEffectType,
            int intValue,
            float floatValue,
            int rarity,
            int unlockStage,
            int baseMoneyPrice)
        {
            DiceTypeDefinitionSO asset = LoadOrCreateAsset<DiceTypeDefinitionSO>(path);
            ApplyDiceTypeFields(asset, diceTypeId, displayName, description, visualColor,
                materialKey, synergyTag, intrinsicEffectType, intValue, floatValue,
                rarity, unlockStage, baseMoneyPrice);
        }

        /// <summary>SerializedObject를 통해 DiceType SO 필드를 설정한다.</summary>
        private static void ApplyDiceTypeFields(
            ScriptableObject asset,
            string diceTypeId,
            string displayName,
            string description,
            Color visualColor,
            string materialKey,
            DiceSynergyTag synergyTag,
            DiceIntrinsicEffectType intrinsicEffectType,
            int intValue,
            float floatValue,
            int rarity,
            int unlockStage,
            int baseMoneyPrice)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "diceTypeId", diceTypeId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            // visualColor는 SerializedProperty.colorValue로 직접 설정
            {
                SerializedProperty prop = so.FindProperty("visualColor");
                if (prop == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Missing field: visualColor on " + so.targetObject.name);
                    allOk = false;
                }
                else
                {
                    prop.colorValue = visualColor;
                }
            }
            allOk &= SetString(so, "materialKey", materialKey);
            allOk &= SetEnum(so, "synergyTag", synergyTag);
            allOk &= SetEnum(so, "intrinsicEffectType", intrinsicEffectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "unlockStage", unlockStage);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on DiceType asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied DiceType fields: {AssetDatabase.GetAssetPath(asset)} diceTypeId={diceTypeId}");
        }

        // ── DiceSynergy SO 생성 ────────────────────────────────────────

        /// <summary>7종의 DiceSynergy SO를 생성/업데이트한다.</summary>
        private static void CreateAllDiceSynergies()
        {
            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Red_2.asset",
                "synergy.red.2", "Red 2 Set",
                "Adds Score +2 when odd dice are used in SlotPair calculation with 2 or more Red dice.",
                DiceSynergyTag.Red, 2, DiceSynergyEffectType.AddScoreForOddDice, 2, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Red_4.asset",
                "synergy.red.4", "Red 4 Set",
                "Adds Force +1 when the Cast has at least 3 odd results with 4 or more Red dice.",
                DiceSynergyTag.Red, 4, DiceSynergyEffectType.AddForceIfOddDiceCountAtLeast, 1, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Blue_2.asset",
                "synergy.blue.2", "Blue 2 Set",
                "Adds Score +2 when even dice are used in SlotPair calculation with 2 or more Blue dice.",
                DiceSynergyTag.Blue, 2, DiceSynergyEffectType.AddScoreForEvenDice, 2, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Blue_4.asset",
                "synergy.blue.4", "Blue 4 Set",
                "Adds Force +1 when the Cast has at least 3 even results with 4 or more Blue dice.",
                DiceSynergyTag.Blue, 4, DiceSynergyEffectType.AddForceIfEvenDiceCountAtLeast, 1, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Iron_3.asset",
                "synergy.iron.3", "Iron 3 Set",
                "Adds Score +3 for each dice of 5 or higher with 3 or more Iron dice.",
                DiceSynergyTag.Iron, 3, DiceSynergyEffectType.AddScoreForHighDice, 3, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Broken_2.asset",
                "synergy.broken.2", "Broken 2 Set",
                "Increases Broken Cast damage reduction by 10% with 2 or more Broken dice.",
                DiceSynergyTag.Broken, 2, DiceSynergyEffectType.IncreaseBrokenCastDamageReduction, 10, 0f);

            CreateDiceSynergy(DiceSynergiesPath + "/DiceSynergy_Broken_4.asset",
                "synergy.broken.4", "Broken 4 Set",
                "Adds Overcharge +1 when submitting Broken Cast with 4 or more Broken dice.",
                DiceSynergyTag.Broken, 4, DiceSynergyEffectType.AddOverchargeOnBrokenCast, 1, 0f);
        }

        /// <summary>DiceSynergy SO를 생성하거나 업데이트한다.</summary>
        private static void CreateDiceSynergy(
            string path,
            string synergyId,
            string displayName,
            string description,
            DiceSynergyTag requiredTag,
            int requiredCount,
            DiceSynergyEffectType effectType,
            int intValue,
            float floatValue)
        {
            DiceSynergyDefinitionSO asset = LoadOrCreateAsset<DiceSynergyDefinitionSO>(path);
            ApplyDiceSynergyFields(asset, synergyId, displayName, description,
                requiredTag, requiredCount, effectType, intValue, floatValue);
        }

        /// <summary>SerializedObject를 통해 DiceSynergy SO 필드를 설정한다.</summary>
        private static void ApplyDiceSynergyFields(
            ScriptableObject asset,
            string synergyId,
            string displayName,
            string description,
            DiceSynergyTag requiredTag,
            int requiredCount,
            DiceSynergyEffectType effectType,
            int intValue,
            float floatValue)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "synergyId", synergyId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetEnum(so, "requiredTag", requiredTag);
            allOk &= SetInt(so, "requiredCount", requiredCount);
            allOk &= SetEnum(so, "effectType", effectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on DiceSynergy asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied DiceSynergy fields: {AssetDatabase.GetAssetPath(asset)} synergyId={synergyId}");
        }

        // ── DiceFaceUpgrade SO 생성 ────────────────────────────────────

        /// <summary>11종의 DiceFaceUpgrade SO를 생성/업데이트한다.</summary>
        private static void CreateAllDiceFaceUpgrades()
        {
            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset",
                "face.heavy_six", "Heavy Six",
                "Treats this face as 6 and adds Score +2 when rolled.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f, 1, 4);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset",
                "face.red_odd_rune", "Red Odd Rune",
                "Adds Score +2 when rolled if the face value is odd.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f, 1, 3);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset",
                "face.blue_even_rune", "Blue Even Rune",
                "Adds Force +0.2 when rolled if the face value is even.",
                false, 2, DiceFaceType.Number, 2,
                DiceFaceUpgradeEffectType.AddForceWhenRolled, 0, 0.2f, 1, 3);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset",
                "face.iron_mark", "Iron Mark",
                "Adds Score +3 when rolled if the face value is 5 or higher.",
                false, 5, DiceFaceType.Number, 5,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 3, 0f, 1, 4);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset",
                "face.overcharge_mark", "Overcharge Mark",
                "Adds Overcharge +1 when this face is included in a submitted Cast. Limited to once per round.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddOverchargeWhenUsed, 1, 0f, 2, 5);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset",
                "face.guard_mark", "Guard Mark",
                "Reduces incoming damage by 5 when this face is included in a lost Clash.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.ReduceIncomingDamageWhenUsed, 5, 0f, 2, 5);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset",
                "face.coin_mark", "Coin Mark",
                "Earns Money +1 when winning a Round with this face included.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddMoneyOnRoundWinWhenUsed, 1, 0f, 2, 5);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset",
                "face.cracked_face", "Cracked Face",
                "Treated as 6 but increases incoming damage by 10% when losing a Clash.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.IncreaseIncomingDamageWhenLose, 10, 0f, 1, 2);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset",
                "face.blank_face", "Blank Face",
                "Does not contribute to patterns. Device effect on this slot is doubled. Currently not implemented / on hold.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f, 3, 6);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset",
                "face.wild_face", "Wild Face",
                "Treated as the most favorable number from 1 to 6. Currently not implemented / on hold.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f, 3, 8);

            CreateDiceFaceUpgrade(DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset",
                "face.mirror_face", "Mirror Face",
                "Copies the current value of the left dice. Currently not implemented / on hold.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f, 3, 8);
        }

        /// <summary>DiceFaceUpgrade SO를 생성하거나 업데이트한다.</summary>
        private static void CreateDiceFaceUpgrade(
            string path,
            string upgradeId,
            string displayName,
            string description,
            bool requiresSpecificNumber,
            int targetNumber,
            DiceFaceType replacementFaceType,
            int replacementNumberValue,
            DiceFaceUpgradeEffectType effectType,
            int intValue,
            float floatValue,
            int rarity,
            int baseMoneyPrice)
        {
            DiceFaceUpgradeDefinitionSO asset = LoadOrCreateAsset<DiceFaceUpgradeDefinitionSO>(path);
            ApplyDiceFaceUpgradeFields(asset, upgradeId, displayName, description,
                requiresSpecificNumber, targetNumber, replacementFaceType, replacementNumberValue,
                effectType, intValue, floatValue, rarity, baseMoneyPrice);
        }

        /// <summary>SerializedObject를 통해 DiceFaceUpgrade SO 필드를 설정한다.</summary>
        private static void ApplyDiceFaceUpgradeFields(
            ScriptableObject asset,
            string upgradeId,
            string displayName,
            string description,
            bool requiresSpecificNumber,
            int targetNumber,
            DiceFaceType replacementFaceType,
            int replacementNumberValue,
            DiceFaceUpgradeEffectType effectType,
            int intValue,
            float floatValue,
            int rarity,
            int baseMoneyPrice)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "upgradeId", upgradeId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetBool(so, "requiresSpecificNumber", requiresSpecificNumber);
            allOk &= SetInt(so, "targetNumber", targetNumber);
            allOk &= SetEnum(so, "replacementFaceType", replacementFaceType);
            allOk &= SetInt(so, "replacementNumberValue", replacementNumberValue);
            allOk &= SetEnum(so, "effectType", effectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on DiceFaceUpgrade asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied DiceFaceUpgrade fields: {AssetDatabase.GetAssetPath(asset)} upgradeId={upgradeId}");
        }

        // ── ShopProduct SO 생성 ────────────────────────────────────────

        /// <summary>38종의 ShopProduct SO를 생성/업데이트한다.</summary>
        private static void CreateAllShopProducts()
        {
            // ── Device ShopProduct 20종 ──
            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_SafetyPin.asset",
                "shop.device.safety_pin", "Safety Pin",
                "Adds Score +12 if the current slot dice is even.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_SafetyPin.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_UnstableFuse.asset",
                "shop.device.unstable_fuse", "Unstable Fuse",
                "Adds Force +1 if the current slot dice is odd.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_UnstableFuse.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LeadWeight.asset",
                "shop.device.lead_weight", "Lead Weight",
                "Adds Score +15 if the current slot dice is 5 or higher.",
                ShopProductType.Device, 1, 5, 0,
                DevicesCommonPath + "/Device_LeadWeight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LowGear.asset",
                "shop.device.low_gear", "Low Gear",
                "Adds Force +1 if the current slot dice is 2 or lower.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_LowGear.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_CastStampAces.asset",
                "shop.device.cast_stamp_aces", "Aces Stamp",
                "Adds Score +20 when submitting Aces.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_CastStampAces.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_CastStampChance.asset",
                "shop.device.cast_stamp_chance", "Chance Stamp",
                "Adds Score +15 when submitting Chance.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_CastStampChance.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_PairContact.asset",
                "shop.device.pair_contact", "Pair Contact",
                "Adds Force +1 if the current dice is included in the Cast calculation.",
                ShopProductType.Device, 1, 5, 0,
                DevicesCommonPath + "/Device_PairContact.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LeftCoupler.asset",
                "shop.device.left_coupler", "Left Coupler",
                "Adds Force +1 if the current dice matches the previous slot dice.",
                ShopProductType.Device, 1, 5, 0,
                DevicesCommonPath + "/Device_LeftCoupler.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_RelayMotor.asset",
                "shop.device.relay_motor", "Relay Motor",
                "Multiplies Force by 1.5 if the current dice is greater than the previous slot dice.",
                ShopProductType.Device, 1, 6, 0,
                DevicesCommonPath + "/Device_RelayMotor.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_FrontLoader.asset",
                "shop.device.front_loader", "Front Loader",
                "Adds Score +18 if this is slot index 1.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_FrontLoader.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_EndValveLight.asset",
                "shop.device.end_valve_light", "Light End Valve",
                "Adds True Damage +15 if this is slot index 5.",
                ShopProductType.Device, 1, 5, 0,
                DevicesCommonPath + "/Device_EndValveLight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_PressureGaugeLight.asset",
                "shop.device.pressure_gauge_light", "Light Pressure Gauge",
                "Adds Score +10 if Stage Threat is at least 1.",
                ShopProductType.Device, 1, 4, 0,
                DevicesCommonPath + "/Device_PressureGaugeLight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_MirrorShard.asset",
                "shop.device.mirror_shard", "Mirror Shard",
                "Adds Force +2 if the mirror slot has the same dice value.",
                ShopProductType.Device, 2, 8, 0,
                DevicesRarePath + "/Device_MirrorShard.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_IsolatedGear.asset",
                "shop.device.isolated_gear", "Isolated Gear",
                "Adds Score +25 if both neighboring dice differ by at least 2 from the current dice.",
                ShopProductType.Device, 2, 8, 0,
                DevicesRarePath + "/Device_IsolatedGear.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_OverdriveChip.asset",
                "shop.device.overdrive_chip", "Overdrive Chip",
                "Multiplies Force by 1.5 when submitting Tessera.",
                ShopProductType.Device, 2, 9, 0,
                DevicesRarePath + "/Device_OverdriveChip.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_FullHouseBracket.asset",
                "shop.device.fullhouse_bracket", "Full House Bracket",
                "Adds Score +30 when submitting Full House.",
                ShopProductType.Device, 2, 8, 0,
                DevicesRarePath + "/Device_FullHouseBracket.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_StraightRail.asset",
                "shop.device.straight_rail", "Straight Rail",
                "Adds Force +1 when submitting Small Straight or Large Straight.",
                ShopProductType.Device, 2, 8, 0,
                DevicesRarePath + "/Device_StraightRail.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_HighVoltagePin.asset",
                "shop.device.high_voltage_pin", "High Voltage Pin",
                "Multiplies Force by 2 if the current slot dice is 6.",
                ShopProductType.Device, 2, 9, 0,
                DevicesRarePath + "/Device_HighVoltagePin.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_EndValve.asset",
                "shop.device.end_valve", "End Valve",
                "Adds True Damage +35 if the sum of previous slots is at least 20.",
                ShopProductType.Device, 2, 9, 0,
                DevicesRarePath + "/Device_EndValve.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_StagePressureMeter.asset",
                "shop.device.stage_pressure_meter", "Stage Pressure Meter",
                "Adds Force +1 if Stage Threat is at least 2.",
                ShopProductType.Device, 2, 8, 0,
                DevicesRarePath + "/Device_StagePressureMeter.asset");

            // ── DiceType ShopProduct 7종 ──
            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Red.asset",
                "shop.dice.red", "Red Dice",
                "Adds Score +2 when an odd result is used in SlotPair calculation.",
                ShopProductType.DiceSet, 1, 5, 0,
                DiceTypesPath + "/DiceType_Red.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Blue.asset",
                "shop.dice.blue", "Blue Dice",
                "Adds Force +0.2 when an even result is used in SlotPair calculation.",
                ShopProductType.DiceSet, 1, 5, 0,
                DiceTypesPath + "/DiceType_Blue.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Iron.asset",
                "shop.dice.iron", "Iron Dice",
                "Adds Score +3 when a result of 5 or higher is used in SlotPair calculation.",
                ShopProductType.DiceSet, 1, 5, 0,
                DiceTypesPath + "/DiceType_Iron.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Broken.asset",
                "shop.dice.broken", "Broken Dice",
                "Candidate for Overcharge compensation when submitting Broken Cast.",
                ShopProductType.DiceSet, 1, 6, 0,
                DiceTypesPath + "/DiceType_Broken.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Gold.asset",
                "shop.dice.gold", "Gold Dice",
                "Candidate for earning Money +1 when winning a Round with this dice in the Cast.",
                ShopProductType.DiceSet, 2, 7, 0,
                DiceTypesPath + "/DiceType_Gold.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Green.asset",
                "shop.dice.green", "Green Dice",
                "Candidate for adding Score +2 when a result of 3 or lower is used in SlotPair calculation.",
                ShopProductType.DiceSet, 1, 5, 0,
                DiceTypesPath + "/DiceType_Green.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Void.asset",
                "shop.dice.void", "Void Dice",
                "Candidate for reducing incoming damage when this dice is included in a lost Clash.",
                ShopProductType.DiceSet, 2, 7, 0,
                DiceTypesPath + "/DiceType_Void.asset");

            // ── DiceFaceUpgrade ShopProduct 11종 ──
            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_HeavySix.asset",
                "shop.face.heavy_six", "Heavy Six",
                "Treats this face as 6 and adds Score +2 when rolled.",
                ShopProductType.DiceFaceUpgrade, 1, 4, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_RedOddRune.asset",
                "shop.face.red_odd_rune", "Red Odd Rune",
                "Adds Score +2 when rolled if the face value is odd.",
                ShopProductType.DiceFaceUpgrade, 1, 3, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlueEvenRune.asset",
                "shop.face.blue_even_rune", "Blue Even Rune",
                "Adds Force +0.2 when rolled if the face value is even.",
                ShopProductType.DiceFaceUpgrade, 1, 3, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_IronMark.asset",
                "shop.face.iron_mark", "Iron Mark",
                "Adds Score +3 when rolled if the face value is 5 or higher.",
                ShopProductType.DiceFaceUpgrade, 1, 4, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_OverchargeMark.asset",
                "shop.face.overcharge_mark", "Overcharge Mark",
                "Adds Overcharge +1 when this face is included in a submitted Cast. Limited to once per round.",
                ShopProductType.DiceFaceUpgrade, 2, 5, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_GuardMark.asset",
                "shop.face.guard_mark", "Guard Mark",
                "Reduces incoming damage by 5 when this face is included in a lost Clash.",
                ShopProductType.DiceFaceUpgrade, 2, 5, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CoinMark.asset",
                "shop.face.coin_mark", "Coin Mark",
                "Earns Money +1 when winning a Round with this face included.",
                ShopProductType.DiceFaceUpgrade, 2, 5, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CrackedFace.asset",
                "shop.face.cracked_face", "Cracked Face",
                "Treated as 6 but increases incoming damage by 10% when losing a Clash.",
                ShopProductType.DiceFaceUpgrade, 1, 2, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlankFace.asset",
                "shop.face.blank_face", "Blank Face",
                "Does not contribute to patterns. Device effect on this slot is doubled. Currently not implemented / on hold.",
                ShopProductType.DiceFaceUpgrade, 3, 6, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_WildFace.asset",
                "shop.face.wild_face", "Wild Face",
                "Treated as the most favorable number from 1 to 6. Currently not implemented / on hold.",
                ShopProductType.DiceFaceUpgrade, 3, 8, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_MirrorFace.asset",
                "shop.face.mirror_face", "Mirror Face",
                "Copies the current value of the left dice. Currently not implemented / on hold.",
                ShopProductType.DiceFaceUpgrade, 3, 8, 0,
                DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset");
        }

        /// <summary>Device용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductDevice(
            string path,
            string productId,
            string displayName,
            string description,
            ShopProductType productType,
            int tier,
            int baseMoneyPrice,
            int baseOverchargePrice,
            string deviceAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            SlotPairDeviceDefinitionSO deviceRef = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(deviceAssetPath);
            ApplyShopProductFields(asset, productId, displayName, description,
                productType, tier, baseMoneyPrice, baseOverchargePrice,
                deviceRef, null, null);
        }

        /// <summary>DiceType용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductDiceType(
            string path,
            string productId,
            string displayName,
            string description,
            ShopProductType productType,
            int tier,
            int baseMoneyPrice,
            int baseOverchargePrice,
            string diceTypeAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            DiceTypeDefinitionSO diceTypeRef = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(diceTypeAssetPath);
            ApplyShopProductFields(asset, productId, displayName, description,
                productType, tier, baseMoneyPrice, baseOverchargePrice,
                null, diceTypeRef, null);
        }

        /// <summary>DiceFaceUpgrade용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductFaceUpgrade(
            string path,
            string productId,
            string displayName,
            string description,
            ShopProductType productType,
            int tier,
            int baseMoneyPrice,
            int baseOverchargePrice,
            string faceUpgradeAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            DiceFaceUpgradeDefinitionSO faceUpgradeRef = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(faceUpgradeAssetPath);
            ApplyShopProductFields(asset, productId, displayName, description,
                productType, tier, baseMoneyPrice, baseOverchargePrice,
                null, null, faceUpgradeRef);
        }

        /// <summary>SerializedObject를 통해 ShopProduct SO 필드를 설정한다.</summary>
        private static void ApplyShopProductFields(
            ScriptableObject asset,
            string productId,
            string displayName,
            string description,
            ShopProductType productType,
            int tier,
            int baseMoneyPrice,
            int baseOverchargePrice,
            SlotPairDeviceDefinitionSO deviceDefinition,
            DiceTypeDefinitionSO diceTypeDefinition,
            DiceFaceUpgradeDefinitionSO diceFaceUpgradeDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "productId", productId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetEnum(so, "productType", productType);
            allOk &= SetInt(so, "tier", tier);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);
            allOk &= SetInt(so, "baseOverchargePrice", baseOverchargePrice);
            allOk &= SetObject(so, "deviceDefinition", deviceDefinition);
            allOk &= SetObject(so, "diceTypeDefinition", diceTypeDefinition);
            allOk &= SetObject(so, "diceFaceUpgradeDefinition", diceFaceUpgradeDefinition);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on ShopProduct asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied ShopProduct fields: {AssetDatabase.GetAssetPath(asset)} productId={productId}");
        }

        // ── 기존 ShopProduct SO 복구 ────────────────────────────────────

        /// <summary>기존 3종 ShopProduct SO를 복구한다.</summary>
        private static void RestoreLegacyShopProducts()
        {
            // 1. ShopProduct_Device_DiceValueDoubler -> SlotPairDevice_AddScore_DiceValueX2
            RestoreShopProductDevice(LegacyShopA,
                "shop.device.dice_value_doubler", "Dice Value Doubler",
                "Adds current dice value x2 to Score.",
                ShopProductType.Device, 1, 6, 0,
                LegacyDeviceA);

            // 2. ShopProduct_Device_FlatScorer -> SlotPairDevice_AddScore_Flat4
            RestoreShopProductDevice(LegacyShopB,
                "shop.device.flat_scorer", "Flat Scorer",
                "Legacy flat score device. Currently mapped to any dice condition.",
                ShopProductType.Device, 1, 4, 0,
                LegacyDeviceC);

            // 3. ShopProduct_Device_ForceCell -> SlotPairDevice_AddForce_Flat1
            RestoreShopProductDevice(LegacyShopC,
                "shop.device.force_cell", "Force Cell",
                "Legacy flat force device. Currently mapped to included dice condition.",
                ShopProductType.Device, 1, 4, 0,
                LegacyDeviceB);
        }

        /// <summary>기존 ShopProduct SO가 없으면 생성하고, 있으면 필드를 업데이트한다.</summary>
        private static void RestoreShopProductDevice(
            string path,
            string productId,
            string displayName,
            string description,
            ShopProductType productType,
            int tier,
            int baseMoneyPrice,
            int baseOverchargePrice,
            string deviceAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            SlotPairDeviceDefinitionSO deviceRef = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(deviceAssetPath);
            ApplyShopProductFields(asset, productId, displayName, description,
                productType, tier, baseMoneyPrice, baseOverchargePrice,
                deviceRef, null, null);
        }

        // ── 검증 ────────────────────────────────────────────────────────

        /// <summary>생성된 모든 SO의 필드가 올바르게 설정되었는지 검증한다.</summary>
        private static void VerifyGeneratedAssets()
        {
            bool allPassed = true;

            // Device SO 20종 검증
            string[] devicePaths = new string[]
            {
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

            foreach (string path in devicePaths)
            {
                SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(path);
                if (device == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: Device asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                // SerializedObject로 읽어서 검증
                SerializedObject so = new SerializedObject(device);
                SerializedProperty deviceIdProp = so.FindProperty("deviceId");
                if (deviceIdProp == null || deviceIdProp.stringValue == "device.none")
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: Device " + path + " has deviceId=device.none");
                    allPassed = false;
                }

                SerializedProperty deviceTypeProp = so.FindProperty("deviceType");
                if (deviceTypeProp == null || deviceTypeProp.enumValueIndex == 0)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: Device " + path + " has deviceType=None");
                    allPassed = false;
                }
            }

            // DiceType SO 7종 검증
            string[] diceTypePaths = new string[]
            {
                DiceTypesPath + "/DiceType_Red.asset",
                DiceTypesPath + "/DiceType_Blue.asset",
                DiceTypesPath + "/DiceType_Iron.asset",
                DiceTypesPath + "/DiceType_Broken.asset",
                DiceTypesPath + "/DiceType_Gold.asset",
                DiceTypesPath + "/DiceType_Green.asset",
                DiceTypesPath + "/DiceType_Void.asset",
            };

            foreach (string path in diceTypePaths)
            {
                DiceTypeDefinitionSO diceType = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(path);
                if (diceType == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceType asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(diceType);
                SerializedProperty diceTypeIdProp = so.FindProperty("diceTypeId");
                if (diceTypeIdProp == null || diceTypeIdProp.stringValue == "dice.standard")
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceType " + path + " has diceTypeId=dice.standard");
                    allPassed = false;
                }

                SerializedProperty synergyTagProp = so.FindProperty("synergyTag");
                if (synergyTagProp == null || synergyTagProp.enumValueIndex == 0)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceType " + path + " has synergyTag=None");
                    allPassed = false;
                }
            }

            // DiceSynergy SO 7종 검증
            string[] synergyPaths = new string[]
            {
                DiceSynergiesPath + "/DiceSynergy_Red_2.asset",
                DiceSynergiesPath + "/DiceSynergy_Red_4.asset",
                DiceSynergiesPath + "/DiceSynergy_Blue_2.asset",
                DiceSynergiesPath + "/DiceSynergy_Blue_4.asset",
                DiceSynergiesPath + "/DiceSynergy_Iron_3.asset",
                DiceSynergiesPath + "/DiceSynergy_Broken_2.asset",
                DiceSynergiesPath + "/DiceSynergy_Broken_4.asset",
            };

            foreach (string path in synergyPaths)
            {
                DiceSynergyDefinitionSO synergy = AssetDatabase.LoadAssetAtPath<DiceSynergyDefinitionSO>(path);
                if (synergy == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceSynergy asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(synergy);
                SerializedProperty synergyIdProp = so.FindProperty("synergyId");
                if (synergyIdProp == null || synergyIdProp.stringValue == "synergy.none")
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceSynergy " + path + " has synergyId=synergy.none");
                    allPassed = false;
                }

                SerializedProperty requiredTagProp = so.FindProperty("requiredTag");
                if (requiredTagProp == null || requiredTagProp.enumValueIndex == 0)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceSynergy " + path + " has requiredTag=None");
                    allPassed = false;
                }
            }

            // DiceFaceUpgrade SO 11종 검증
            string[] faceUpgradePaths = new string[]
            {
                DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset",
                DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset",
            };

            foreach (string path in faceUpgradePaths)
            {
                DiceFaceUpgradeDefinitionSO faceUpgrade = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(path);
                if (faceUpgrade == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceFaceUpgrade asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(faceUpgrade);
                SerializedProperty upgradeIdProp = so.FindProperty("upgradeId");
                if (upgradeIdProp == null || upgradeIdProp.stringValue == "face.upgrade.none")
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceFaceUpgrade " + path + " has upgradeId=face.upgrade.none");
                    allPassed = false;
                }
            }

            // ShopProduct SO 검증
            string[] shopProductPaths = new string[]
            {
                // Device ShopProduct 20종
                ShopDevicesPath + "/ShopProduct_Device_SafetyPin.asset",
                ShopDevicesPath + "/ShopProduct_Device_UnstableFuse.asset",
                ShopDevicesPath + "/ShopProduct_Device_LeadWeight.asset",
                ShopDevicesPath + "/ShopProduct_Device_LowGear.asset",
                ShopDevicesPath + "/ShopProduct_Device_CastStampAces.asset",
                ShopDevicesPath + "/ShopProduct_Device_CastStampChance.asset",
                ShopDevicesPath + "/ShopProduct_Device_PairContact.asset",
                ShopDevicesPath + "/ShopProduct_Device_LeftCoupler.asset",
                ShopDevicesPath + "/ShopProduct_Device_RelayMotor.asset",
                ShopDevicesPath + "/ShopProduct_Device_FrontLoader.asset",
                ShopDevicesPath + "/ShopProduct_Device_EndValveLight.asset",
                ShopDevicesPath + "/ShopProduct_Device_PressureGaugeLight.asset",
                ShopDevicesPath + "/ShopProduct_Device_MirrorShard.asset",
                ShopDevicesPath + "/ShopProduct_Device_IsolatedGear.asset",
                ShopDevicesPath + "/ShopProduct_Device_OverdriveChip.asset",
                ShopDevicesPath + "/ShopProduct_Device_FullHouseBracket.asset",
                ShopDevicesPath + "/ShopProduct_Device_StraightRail.asset",
                ShopDevicesPath + "/ShopProduct_Device_HighVoltagePin.asset",
                ShopDevicesPath + "/ShopProduct_Device_EndValve.asset",
                ShopDevicesPath + "/ShopProduct_Device_StagePressureMeter.asset",
                // DiceType ShopProduct 7종
                ShopDiceTypesPath + "/ShopProduct_DiceType_Red.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Blue.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Iron.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Broken.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Gold.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Green.asset",
                ShopDiceTypesPath + "/ShopProduct_DiceType_Void.asset",
                // DiceFaceUpgrade ShopProduct 11종
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_HeavySix.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_RedOddRune.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlueEvenRune.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_IronMark.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_OverchargeMark.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_GuardMark.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CoinMark.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CrackedFace.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlankFace.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_WildFace.asset",
                ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_MirrorFace.asset",
            };

            foreach (string path in shopProductPaths)
            {
                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(path);
                if (shopProduct == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: ShopProduct asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(shopProduct);
                SerializedProperty productIdProp = so.FindProperty("productId");
                if (productIdProp == null || productIdProp.stringValue == "shop.product.none")
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: ShopProduct " + path + " has productId=shop.product.none");
                    allPassed = false;
                }

                SerializedProperty productTypeProp = so.FindProperty("productType");
                if (productTypeProp == null || productTypeProp.enumValueIndex == 0)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: ShopProduct " + path + " has productType=None");
                    allPassed = false;
                }

                // Device ShopProduct의 deviceDefinition 검증
                if (path.Contains("/Devices/"))
                {
                    SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
                    if (deviceDefProp == null || deviceDefProp.objectReferenceValue == null)
                    {
                        Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: Device ShopProduct " + path + " has deviceDefinition=null");
                        allPassed = false;
                    }
                }

                // DiceType ShopProduct의 diceTypeDefinition 검증
                if (path.Contains("/DiceTypes/"))
                {
                    SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
                    if (diceTypeDefProp == null || diceTypeDefProp.objectReferenceValue == null)
                    {
                        Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: DiceType ShopProduct " + path + " has diceTypeDefinition=null");
                        allPassed = false;
                    }
                }

                // DiceFaceUpgrade ShopProduct의 diceFaceUpgradeDefinition 검증
                if (path.Contains("/DiceFaceUpgrades/"))
                {
                    SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
                    if (faceUpgradeDefProp == null || faceUpgradeDefProp.objectReferenceValue == null)
                    {
                        Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed: FaceUpgrade ShopProduct " + path + " has diceFaceUpgradeDefinition=null");
                        allPassed = false;
                    }
                }
            }

            if (allPassed)
            {
                Debug.Log("[ShopGrowthProductAssetGenerator] Verification passed.");
            }
            else
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Verification failed. Some assets have default values.");
            }
        }
    }
}
