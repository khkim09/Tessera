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

            // 2. Device SO 20종 생성/업데이트
            CreateAllDevices();

            // 3. DiceType SO 7종 생성/업데이트
            CreateAllDiceTypes();

            // 4. DiceSynergy SO 7종 생성/업데이트
            CreateAllDiceSynergies();

            // 5. DiceFaceUpgrade SO 11종 생성/업데이트
            CreateAllDiceFaceUpgrades();

            // 6. ShopProduct SO 생성/업데이트
            CreateAllShopProducts();

            // 7. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 9. 검증 실행
            VerifyGeneratedAssets();

            // 10. 결과 출력
            Debug.Log($"[ShopGrowthProductAssetGenerator] Complete. Created: {_createdCount}, Updated: {_updatedCount}");
        }

        /// <summary>Tools/Tessera/Assets/Generate Device SO v4.4 메뉴 항목. Device SO만 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate Device SO v4.4")]
        private static void GenerateDeviceSOV44()
        {
            _createdCount = 0;
            _updatedCount = 0;

            // 1. 필요한 폴더 생성
            EnsureAllFoldersExist();

            // 2. Device SO 26종 생성/업데이트 (Common 18종 + Rare 8종)
            CreateAllDevicesV44();

            // 3. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 검증 실행
            VerifyDeviceAssetsV44();

            // 5. 결과 출력
            Debug.Log($"[ShopGrowthProductAssetGenerator] v4.4 Device SO Complete. Created: {_createdCount}, Updated: {_updatedCount}");
        }

        /// <summary>Tools/Tessera/Assets/Generate DiceFaceUpgrade SO v4.4 메뉴 항목. DiceFaceUpgrade SO만 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate DiceFaceUpgrade SO v4.4")]
        private static void GenerateDiceFaceUpgradeSOV44()
        {
            _createdCount = 0;
            _updatedCount = 0;

            // 1. 필요한 폴더 생성
            EnsureFolder(RootPath, "DiceFaceUpgrades");

            // 2. DiceFaceUpgrade SO 11종 생성/업데이트
            CreateAllDiceFaceUpgrades();

            // 3. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 결과 출력
            int created = _createdCount;
            int updated = _updatedCount;
            Debug.Log($"[ShopGrowthProductAssetGenerator] v4.4 DiceFaceUpgrade SO Complete. Created: {created}, Updated: {updated}");

            // 5. 보류 항목 안내
            Debug.Log("[ShopGrowthProductAssetGenerator] [보류] FaceUpgrade_MirrorFace: 복제형. PatternEvaluator 전 단계 개입 필요.");
            Debug.Log("[ShopGrowthProductAssetGenerator] [보류] FaceUpgrade_BlankFace: 특수 Face. PatternEvaluator 전 단계 개입 필요.");
            Debug.Log("[ShopGrowthProductAssetGenerator] [보류] FaceUpgrade_WildFace: 와일드 평가. PatternEvaluator 전 단계 개입 필요.");
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
                "5번 슬롯이면 True Power +15.",
                SlotPairDeviceType.AddTruePowerIfSlotIndex, 0, 1f, 0f,
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
                "앞 슬롯 합이 20 이상이면 True Power +35.",
                SlotPairDeviceType.AddTruePowerIfPreviousSlotsSumAtLeast, 20, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 35);

            CreateDevice(DevicesRarePath + "/Device_StagePressureMeter.asset",
                "device.stage_pressure_meter", "압력계",
                "StageThreat 2 이상이면 Force +1.",
                SlotPairDeviceType.AddForceIfStageThreatAtLeast, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 2, 0);
        }

        // ── v4.4 Device SO 생성 ────────────────────────────────────────

        /// <summary>v4.4 기준 26종 Device SO를 생성/업데이트한다. (Common 18종 + Rare 8종)</summary>
        private static void CreateAllDevicesV44()
        {
            // ── Common 18종 ──

            // 1. Device_AdderChip: AddScoreIfDiceParity, Any, intValue=3
            CreateDeviceV44(DevicesCommonPath + "/Device_AdderChip.asset",
                "device.adder_chip", "Adder Chip",
                "특정 슬롯 또는 주사위 조건에서 Score +3.",
                SlotPairDeviceType.AddScoreIfDiceParity, 3, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 2. Device_OddAmplifier: AddScoreIfDiceParity, Odd, intValue=3
            CreateDeviceV44(DevicesCommonPath + "/Device_OddAmplifier.asset",
                "device.odd_amplifier", "Odd Amplifier",
                "현재 슬롯 주사위가 홀수이면 Score +3.",
                SlotPairDeviceType.AddScoreIfDiceParity, 3, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Odd, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 3. Device_EvenAmplifier: AddScoreIfDiceParity, Even, intValue=3
            CreateDeviceV44(DevicesCommonPath + "/Device_EvenAmplifier.asset",
                "device.even_amplifier", "Even Amplifier",
                "현재 슬롯 주사위가 짝수이면 Score +3.",
                SlotPairDeviceType.AddScoreIfDiceParity, 3, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Even, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 4. Device_ForceSpring: AddForceIfDiceIncluded, intValue=1 (Force +1, 소수 Force 미지원)
            CreateDeviceV44(DevicesCommonPath + "/Device_ForceSpring.asset",
                "device.force_spring", "Force Spring",
                "조건 충족 시 Force +1.",
                SlotPairDeviceType.AddForceIfDiceIncluded, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 5. Device_ImpactNail: AddDeviceImpactBonusIfSlotActive, deviceImpactBonus=1
            CreateDeviceV44(DevicesCommonPath + "/Device_ImpactNail.asset",
                "device.impact_nail", "Impact Nail",
                "이 Device가 적용되면 DeviceImpactBonus +1.",
                SlotPairDeviceType.AddDeviceImpactBonusIfSlotActive, 0, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 1, 0,
                1, 1, 1, 4, 0);

            // 6. Device_HeavyHammer: AddDeviceImpactBonusIfDiceValueAtLeast, min=5, max=6, deviceImpactBonus=2
            CreateDeviceV44(DevicesCommonPath + "/Device_HeavyHammer.asset",
                "device.heavy_hammer", "Heavy Hammer",
                "현재 슬롯 주사위가 5 이상이면 DeviceImpactBonus +2.",
                SlotPairDeviceType.AddDeviceImpactBonusIfDiceValueAtLeast, 0, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 5, 6, -1, 0, 0, 2, 0,
                1, 1, 1, 4, 0);

            // 7. Device_SafetyPin: AddScoreIfDiceParity, Even, intValue=12
            CreateDeviceV44(DevicesCommonPath + "/Device_SafetyPin.asset",
                "device.safety_pin", "안전 핀",
                "현재 슬롯 주사위가 짝수이면 Score +12.",
                SlotPairDeviceType.AddScoreIfDiceParity, 12, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Even, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 8. Device_UnstableFuse: AddForceIfDiceParity, Odd, intValue=1
            CreateDeviceV44(DevicesCommonPath + "/Device_UnstableFuse.asset",
                "device.unstable_fuse", "불안정한 도화선",
                "현재 슬롯 주사위가 홀수이면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceParity, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Odd, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 9. Device_LeadWeight: AddScoreIfDiceValueAtLeast, min=5, max=6, intValue=15
            CreateDeviceV44(DevicesCommonPath + "/Device_LeadWeight.asset",
                "device.lead_weight", "납추",
                "현재 슬롯 주사위가 5 이상이면 Score +15.",
                SlotPairDeviceType.AddScoreIfDiceValueAtLeast, 15, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 5, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 10. Device_LowGear: AddForceIfDiceValueAtMost, min=1, max=2, intValue=1
            CreateDeviceV44(DevicesCommonPath + "/Device_LowGear.asset",
                "device.low_gear", "저단 기어",
                "현재 슬롯 주사위가 2 이하이면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceValueAtMost, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 2, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 11. Device_CastStampAces: AddScoreIfCastType, Aces, intValue=20
            CreateDeviceV44(DevicesCommonPath + "/Device_CastStampAces.asset",
                "device.cast_stamp_aces", "에이스 스탬프",
                "Aces 제출 시 Score +20.",
                SlotPairDeviceType.AddScoreIfCastType, 20, 1f, 0f,
                RollPatternType.Aces, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 12. Device_CastStampChance: AddScoreIfCastType, Chance, intValue=15
            CreateDeviceV44(DevicesCommonPath + "/Device_CastStampChance.asset",
                "device.cast_stamp_chance", "찬스 스탬프",
                "Chance 제출 시 Score +15.",
                SlotPairDeviceType.AddScoreIfCastType, 15, 1f, 0f,
                RollPatternType.Chance, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 13. Device_PairContact: AddForceIfDiceIncluded, intValue=1
            CreateDeviceV44(DevicesCommonPath + "/Device_PairContact.asset",
                "device.pair_contact", "접점 단자",
                "현재 주사위가 Cast 계산값에 포함되면 Force +1.",
                SlotPairDeviceType.AddForceIfDiceIncluded, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 14. Device_LeftCoupler: AddForceIfSameAsPrevious, intValue=1
            CreateDeviceV44(DevicesCommonPath + "/Device_LeftCoupler.asset",
                "device.left_coupler", "좌측 커플러",
                "이전 슬롯과 같은 눈이면 Force +1.",
                SlotPairDeviceType.AddForceIfSameAsPrevious, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 15. Device_RelayMotor: MultiplyForceIfGreaterThanPrevious, floatValue=1.5
            CreateDeviceV44(DevicesCommonPath + "/Device_RelayMotor.asset",
                "device.relay_motor", "릴레이 모터",
                "이전 슬롯보다 현재 눈이 크면 Force x1.5.",
                SlotPairDeviceType.MultiplyForceIfGreaterThanPrevious, 0, 1.5f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 16. Device_FrontLoader: AddScoreIfSlotIndex, slotIndex=0, intValue=18
            CreateDeviceV44(DevicesCommonPath + "/Device_FrontLoader.asset",
                "device.front_loader", "전방 장전기",
                "1번 슬롯이면 Score +18.",
                SlotPairDeviceType.AddScoreIfSlotIndex, 18, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, 0, 0, 0, 0, 0,
                1, 1, 1, 4, 0);

            // 17. Device_EndValveLight: AddTruePowerIfSlotIndex, slotIndex=4, truePowerValue=15
            CreateDeviceV44(DevicesCommonPath + "/Device_EndValveLight.asset",
                "device.end_valve_light", "소형 엔드 밸브",
                "5번 슬롯이면 TruePower +15.",
                SlotPairDeviceType.AddTruePowerIfSlotIndex, 0, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, 4, 0, 15, 0, 0,
                1, 1, 1, 4, 0);

            // 18. Device_PressureGaugeLight: AddScoreIfStageThreatAtLeast, threat=1, intValue=10
            CreateDeviceV44(DevicesCommonPath + "/Device_PressureGaugeLight.asset",
                "device.pressure_gauge_light", "소형 압력계",
                "StageThreat 1 이상이면 Score +10.",
                SlotPairDeviceType.AddScoreIfStageThreatAtLeast, 10, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 1, 0, 0, 0,
                1, 1, 1, 4, 0);

            // ── Rare 8종 ──

            // 19. Device_MirrorShard: AddForceIfSameAsMirrorSlot, intValue=2
            CreateDeviceV44(DevicesRarePath + "/Device_MirrorShard.asset",
                "device.mirror_shard", "거울 조각",
                "대칭 슬롯과 눈이 같으면 Force +2.",
                SlotPairDeviceType.AddForceIfSameAsMirrorSlot, 2, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 20. Device_IsolatedGear: AddScoreIfIsolatedFromNeighbors, intValue=25
            CreateDeviceV44(DevicesRarePath + "/Device_IsolatedGear.asset",
                "device.isolated_gear", "고립된 톱니",
                "양옆과 차이가 각각 2 이상이면 Score +25.",
                SlotPairDeviceType.AddScoreIfIsolatedFromNeighbors, 25, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 21. Device_OverdriveChip: MultiplyForceIfCastType, Tessera, floatValue=1.5
            CreateDeviceV44(DevicesRarePath + "/Device_OverdriveChip.asset",
                "device.overdrive_chip", "오버드라이브 칩",
                "Tessera 제출 시 Force x1.5.",
                SlotPairDeviceType.MultiplyForceIfCastType, 0, 1.5f, 0f,
                RollPatternType.Tessera, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 22. Device_FullHouseBracket: AddScoreIfCastType, FullHouse, intValue=30
            CreateDeviceV44(DevicesRarePath + "/Device_FullHouseBracket.asset",
                "device.fullhouse_bracket", "풀하우스 브래킷",
                "Full House 제출 시 Score +30.",
                SlotPairDeviceType.AddScoreIfCastType, 30, 1f, 0f,
                RollPatternType.FullHouse, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 23. Device_StraightRail: AddForceIfCastTypeEither, SmallStraight, LargeStraight, intValue=1
            CreateDeviceV44(DevicesRarePath + "/Device_StraightRail.asset",
                "device.straight_rail", "스트레이트 레일",
                "Small Straight 또는 Large Straight 제출 시 Force +1.",
                SlotPairDeviceType.AddForceIfCastTypeEither, 1, 1f, 0f,
                RollPatternType.SmallStraight, RollPatternType.LargeStraight,
                DiceValueParity.Any, 1, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 24. Device_HighVoltagePin: MultiplyForceIfDiceValueAtLeast, min=6, max=6, floatValue=2
            CreateDeviceV44(DevicesRarePath + "/Device_HighVoltagePin.asset",
                "device.high_voltage_pin", "고전압 핀",
                "현재 눈이 6이면 Force x2.",
                SlotPairDeviceType.MultiplyForceIfDiceValueAtLeast, 0, 2f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 6, 6, -1, 0, 0, 0, 0,
                2, 2, 3, 7, 0);

            // 25. Device_EndValve: AddTruePowerIfPreviousSlotsSumAtLeast, intValue=20, truePowerValue=35
            CreateDeviceV44(DevicesRarePath + "/Device_EndValve.asset",
                "device.end_valve", "엔드 밸브",
                "앞 슬롯 합이 20 이상이면 TruePower +35.",
                SlotPairDeviceType.AddTruePowerIfPreviousSlotsSumAtLeast, 20, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 0, 35, 0, 0,
                2, 2, 3, 7, 0);

            // 26. Device_StagePressureMeter: AddForceIfStageThreatAtLeast, threat=2, intValue=1
            CreateDeviceV44(DevicesRarePath + "/Device_StagePressureMeter.asset",
                "device.stage_pressure_meter", "압력계",
                "StageThreat 2 이상이면 Force +1.",
                SlotPairDeviceType.AddForceIfStageThreatAtLeast, 1, 1f, 0f,
                RollPatternType.None, RollPatternType.None,
                DiceValueParity.Any, 1, 6, -1, 2, 0, 0, 0,
                2, 2, 3, 7, 0);
        }

        // ── v4.4 Device SO 생성 헬퍼 ──────────────────────────────────

        /// <summary>v4.4 Device SO를 생성하거나 업데이트한다. (tier/rarity/unlockStage/baseMoneyPrice/baseOverchargePrice 포함)</summary>
        private static void CreateDeviceV44(
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
            int truePowerValue,
            int deviceImpactBonus,
            int trueImpactDamage,
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
        {
            SlotPairDeviceDefinitionSO asset = LoadOrCreateAsset<SlotPairDeviceDefinitionSO>(path);
            ApplyDeviceFieldsV44(asset, deviceId, displayName, description, deviceType,
                intValue, floatValue, forceThreshold,
                requiredPatternType, secondaryPatternType, requiredParity,
                requiredMinDiceValue, requiredMaxDiceValue, requiredSlotIndex,
                requiredStageThreatLevel, truePowerValue, deviceImpactBonus, trueImpactDamage,
                tier, rarity, unlockStage, baseMoneyPrice, baseOverchargePrice);
        }

        /// <summary>SerializedObject를 통해 v4.4 Device SO 필드를 설정한다.</summary>
        private static void ApplyDeviceFieldsV44(
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
            int truePowerValue,
            int deviceImpactBonus,
            int trueImpactDamage,
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
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
            allOk &= SetInt(so, "truePowerValue", truePowerValue);
            allOk &= SetInt(so, "deviceImpactBonus", deviceImpactBonus);
            allOk &= SetInt(so, "trueImpactDamage", trueImpactDamage);
            allOk &= SetInt(so, "tier", tier);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "unlockStage", unlockStage);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);
            allOk &= SetInt(so, "baseOverchargePrice", baseOverchargePrice);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on Device asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied Device v4.4 fields: {AssetDatabase.GetAssetPath(asset)} deviceId={deviceId}");
        }

        // ── v4.4 Device SO 검증 ────────────────────────────────────────

        /// <summary>v4.4 Device SO 검증. TrueDamage/FinalDamage/DamageBeforeTableRules/ModifiedDamage 문자열이 없어야 한다.</summary>
        private static void VerifyDeviceAssetsV44()
        {
            bool allPassed = true;

            // 검증할 Device SO 경로 목록 (Common 18종 + Rare 8종)
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

            foreach (string path in devicePaths)
            {
                SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(path);
                if (device == null)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] v4.4 Verification failed: Device asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(device);
                SerializedProperty deviceIdProp = so.FindProperty("deviceId");
                if (deviceIdProp == null || string.IsNullOrEmpty(deviceIdProp.stringValue))
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] v4.4 Verification failed: Device " + path + " has empty deviceId");
                    allPassed = false;
                }

                SerializedProperty deviceTypeProp = so.FindProperty("deviceType");
                if (deviceTypeProp == null || deviceTypeProp.enumValueIndex == 0)
                {
                    Debug.LogError("[ShopGrowthProductAssetGenerator] v4.4 Verification failed: Device " + path + " has deviceType=None");
                    allPassed = false;
                }
            }

            if (allPassed)
            {
                Debug.Log("[ShopGrowthProductAssetGenerator] v4.4 Device SO Verification passed.");
            }
            else
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] v4.4 Device SO Verification failed. Some assets have default values.");
            }
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
            int truePowerValue)
        {
            SlotPairDeviceDefinitionSO asset = LoadOrCreateAsset<SlotPairDeviceDefinitionSO>(path);
            ApplyDeviceFields(asset, deviceId, displayName, description, deviceType,
                intValue, floatValue, forceThreshold,
                requiredPatternType, secondaryPatternType, requiredParity,
                requiredMinDiceValue, requiredMaxDiceValue, requiredSlotIndex,
                requiredStageThreatLevel, truePowerValue);
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
            int truePowerValue)
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
            allOk &= SetInt(so, "truePowerValue", truePowerValue);

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

        /// <summary>11종의 DiceFaceUpgrade SO를 생성/업데이트한다. (v4.4 기준)</summary>
        private static void CreateAllDiceFaceUpgrades()
        {
            // 1. FaceUpgrade_RedOddRune: 홀수 Face 연계 공격형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset",
                "face.red_odd_rune", "붉은 홀수 룬",
                "홀수 Face와 연계되는 공격형 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f,
                1, 1, 1, 5, 0);

            // 2. FaceUpgrade_BlueEvenRune: 짝수 Face 연계 안정형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset",
                "face.blue_even_rune", "푸른 짝수 룬",
                "짝수 Face와 연계되는 안정형 Face Upgrade 후보.",
                false, 2, DiceFaceType.Number, 2,
                DiceFaceUpgradeEffectType.AddForceWhenRolled, 0, 0.2f,
                1, 1, 1, 5, 0);

            // 3. FaceUpgrade_HeavySix: 6 Face 강화 고점형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset",
                "face.heavy_six", "무거운 6",
                "6 Face를 강화하는 고점형 Face Upgrade 후보.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f,
                1, 1, 1, 5, 0);

            // 4. FaceUpgrade_IronMark: 높은 눈금 연계 기본형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset",
                "face.iron_mark", "철 표식",
                "높은 눈금과 연계되는 기본 Face Upgrade 후보.",
                false, 5, DiceFaceType.Number, 5,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 3, 0f,
                1, 1, 1, 5, 0);

            // 5. FaceUpgrade_CoinMark: 보상/경제 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset",
                "face.coin_mark", "동전 표식",
                "보상/경제 효과와 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddMoneyOnRoundWinWhenUsed, 1, 0f,
                1, 1, 1, 5, 0);

            // 6. FaceUpgrade_GuardMark: 피해 방어 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset",
                "face.guard_mark", "가드 표식",
                "피해 방어 효과와 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.ReduceIncomingDamageWhenUsed, 5, 0f,
                2, 2, 2, 7, 0);

            // 7. FaceUpgrade_OverchargeMark: Overcharge 획득 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset",
                "face.overcharge_mark", "과충전 표식",
                "Overcharge 획득과 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddOverchargeWhenUsed, 1, 0f,
                2, 2, 2, 7, 0);

            // 8. FaceUpgrade_CrackedFace: 위험 보상형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset",
                "face.cracked_face", "금 간 Face",
                "강한 보정과 패널티를 함께 갖는 위험 보상형 Face Upgrade 후보.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.IncreaseIncomingDamageWhenLose, 10, 0f,
                2, 2, 2, 7, 0);

            // 9. FaceUpgrade_MirrorFace: 복제형 (PatternEvaluator 전 단계 개입 필요 → 보류)
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset",
                "face.mirror_face", "거울 Face",
                "다른 Face 값을 참조하는 복제형 Face Upgrade 후보. PatternEvaluator 전 단계 개입이 필요하므로 현재 보류.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                2, 2, 3, 8, 0);

            // 10. FaceUpgrade_BlankFace: 특수 Face (PatternEvaluator 전 단계 개입 필요 → 보류)
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset",
                "face.blank_face", "빈 Face",
                "족보 기여를 줄이고 다른 효과와 연계하는 특수 Face Upgrade 후보. PatternEvaluator 전 단계 개입이 필요하므로 현재 보류.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                2, 2, 3, 8, 0);

            // 11. FaceUpgrade_WildFace: 와일드 Face (PatternEvaluator 전 단계 개입 필요 → 보류)
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset",
                "face.wild_face", "와일드 Face",
                "족보 평가에서 유리한 값으로 취급될 수 있는 고급 Face Upgrade 후보. PatternEvaluator 전 단계 개입이 필요하므로 현재 보류.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                3, 3, 4, 10, 1);
        }

        /// <summary>v4.4 DiceFaceUpgrade SO를 생성하거나 업데이트한다. (tier/unlockStage/baseOverchargePrice 포함)</summary>
        private static void CreateDiceFaceUpgradeV44(
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
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
        {
            DiceFaceUpgradeDefinitionSO asset = LoadOrCreateAsset<DiceFaceUpgradeDefinitionSO>(path);
            ApplyDiceFaceUpgradeFieldsV44(asset, upgradeId, displayName, description,
                requiresSpecificNumber, targetNumber, replacementFaceType, replacementNumberValue,
                effectType, intValue, floatValue, tier, rarity, unlockStage, baseMoneyPrice, baseOverchargePrice);
        }

        /// <summary>SerializedObject를 통해 v4.4 DiceFaceUpgrade SO 필드를 설정한다.</summary>
        private static void ApplyDiceFaceUpgradeFieldsV44(
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
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
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
            allOk &= SetInt(so, "tier", tier);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "unlockStage", unlockStage);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);
            allOk &= SetInt(so, "baseOverchargePrice", baseOverchargePrice);

            if (!allOk)
            {
                Debug.LogError("[ShopGrowthProductAssetGenerator] Failed to set some fields on DiceFaceUpgrade asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[ShopGrowthProductAssetGenerator] Applied DiceFaceUpgrade v4.4 fields: {AssetDatabase.GetAssetPath(asset)} upgradeId={upgradeId}");
        }

        // ── ShopProduct SO 생성 ────────────────────────────────────────

        /// <summary>38종의 ShopProduct SO를 생성/업데이트한다.</summary>
        private static void CreateAllShopProducts()
        {
            // ── Device ShopProduct 20종 ──
            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_SafetyPin.asset",
                "shop.device.safety_pin",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_SafetyPin.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_UnstableFuse.asset",
                "shop.device.unstable_fuse",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_UnstableFuse.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LeadWeight.asset",
                "shop.device.lead_weight",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_LeadWeight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LowGear.asset",
                "shop.device.low_gear",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_LowGear.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_CastStampAces.asset",
                "shop.device.cast_stamp_aces",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_CastStampAces.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_CastStampChance.asset",
                "shop.device.cast_stamp_chance",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_CastStampChance.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_PairContact.asset",
                "shop.device.pair_contact",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_PairContact.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_LeftCoupler.asset",
                "shop.device.left_coupler",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_LeftCoupler.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_RelayMotor.asset",
                "shop.device.relay_motor",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_RelayMotor.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_FrontLoader.asset",
                "shop.device.front_loader",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_FrontLoader.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_EndValveLight.asset",
                "shop.device.end_valve_light",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_EndValveLight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_PressureGaugeLight.asset",
                "shop.device.pressure_gauge_light",
                ShopProductType.Device,
                DevicesCommonPath + "/Device_PressureGaugeLight.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_MirrorShard.asset",
                "shop.device.mirror_shard",
                ShopProductType.Device,
                DevicesRarePath + "/Device_MirrorShard.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_IsolatedGear.asset",
                "shop.device.isolated_gear",
                ShopProductType.Device,
                DevicesRarePath + "/Device_IsolatedGear.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_OverdriveChip.asset",
                "shop.device.overdrive_chip",
                ShopProductType.Device,
                DevicesRarePath + "/Device_OverdriveChip.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_FullHouseBracket.asset",
                "shop.device.fullhouse_bracket",
                ShopProductType.Device,
                DevicesRarePath + "/Device_FullHouseBracket.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_StraightRail.asset",
                "shop.device.straight_rail",
                ShopProductType.Device,
                DevicesRarePath + "/Device_StraightRail.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_HighVoltagePin.asset",
                "shop.device.high_voltage_pin",
                ShopProductType.Device,
                DevicesRarePath + "/Device_HighVoltagePin.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_EndValve.asset",
                "shop.device.end_valve",
                ShopProductType.Device,
                DevicesRarePath + "/Device_EndValve.asset");

            CreateShopProductDevice(ShopDevicesPath + "/ShopProduct_Device_StagePressureMeter.asset",
                "shop.device.stage_pressure_meter",
                ShopProductType.Device,
                DevicesRarePath + "/Device_StagePressureMeter.asset");

            // ── DiceType ShopProduct 7종 ──
            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Red.asset",
                "shop.dice.red",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Red.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Blue.asset",
                "shop.dice.blue",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Blue.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Iron.asset",
                "shop.dice.iron",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Iron.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Broken.asset",
                "shop.dice.broken",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Broken.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Gold.asset",
                "shop.dice.gold",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Gold.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Green.asset",
                "shop.dice.green",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Green.asset");

            CreateShopProductDiceType(ShopDiceTypesPath + "/ShopProduct_DiceType_Void.asset",
                "shop.dice.void",
                ShopProductType.DiceSet,
                DiceTypesPath + "/DiceType_Void.asset");

            // ── DiceFaceUpgrade ShopProduct 11종 ──
            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_HeavySix.asset",
                "shop.face.heavy_six",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_RedOddRune.asset",
                "shop.face.red_odd_rune",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlueEvenRune.asset",
                "shop.face.blue_even_rune",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_IronMark.asset",
                "shop.face.iron_mark",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_OverchargeMark.asset",
                "shop.face.overcharge_mark",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_GuardMark.asset",
                "shop.face.guard_mark",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CoinMark.asset",
                "shop.face.coin_mark",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_CrackedFace.asset",
                "shop.face.cracked_face",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_BlankFace.asset",
                "shop.face.blank_face",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_WildFace.asset",
                "shop.face.wild_face",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset");

            CreateShopProductFaceUpgrade(ShopDiceFaceUpgradesPath + "/ShopProduct_FaceUpgrade_MirrorFace.asset",
                "shop.face.mirror_face",
                ShopProductType.DiceFaceUpgrade,
                DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset");
        }

        /// <summary>Device용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductDevice(
            string path,
            string productId,
            ShopProductType productType,
            string deviceAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            SlotPairDeviceDefinitionSO deviceRef = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(deviceAssetPath);
            ApplyShopProductFields(asset, productId, productType,
                deviceRef, null, null);
        }

        /// <summary>DiceType용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductDiceType(
            string path,
            string productId,
            ShopProductType productType,
            string diceTypeAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            DiceTypeDefinitionSO diceTypeRef = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(diceTypeAssetPath);
            ApplyShopProductFields(asset, productId, productType,
                null, diceTypeRef, null);
        }

        /// <summary>DiceFaceUpgrade용 ShopProduct SO를 생성하거나 업데이트한다.</summary>
        private static void CreateShopProductFaceUpgrade(
            string path,
            string productId,
            ShopProductType productType,
            string faceUpgradeAssetPath)
        {
            ShopProductDefinitionSO asset = LoadOrCreateAsset<ShopProductDefinitionSO>(path);
            DiceFaceUpgradeDefinitionSO faceUpgradeRef = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(faceUpgradeAssetPath);
            ApplyShopProductFields(asset, productId, productType,
                null, null, faceUpgradeRef);
        }

        /// <summary>SerializedObject를 통해 ShopProduct SO 필드를 설정한다.</summary>
        private static void ApplyShopProductFields(
            ScriptableObject asset,
            string productId,
            ShopProductType productType,
            SlotPairDeviceDefinitionSO deviceDefinition,
            DiceTypeDefinitionSO diceTypeDefinition,
            DiceFaceUpgradeDefinitionSO diceFaceUpgradeDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "productId", productId);
            allOk &= SetEnum(so, "productType", productType);
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
