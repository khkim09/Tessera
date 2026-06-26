using System;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 Device SO 전용 생성/수정 Editor 유틸리티.
    /// ShopProduct, WorkshopRules, DiceType, DiceFaceUpgrade, EnemyIntent, Stage/Round는 건드리지 않는다.</summary>
    public static class DeviceSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";
        private const string DevicesCommonPath = RootPath + "/Devices/ShopGrowth/Common";
        private const string DevicesRarePath = RootPath + "/Devices/ShopGrowth/Rare";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate Device SO v4.4 메뉴 항목.
        /// Device SO 26종(Common 18종 + Rare 8종)만 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate Device SO v4.4")]
        private static void GenerateFromMenu()
        {
            GenerateForPipeline();
        }

        /// <summary>v4.4 통합 생성 파이프라인에서 호출하는 진입점이다.</summary>
        public static void GenerateForPipeline()
        {
            _createdCount = 0;
            _updatedCount = 0;

            // 1. 필요한 폴더 생성
            EnsureFolder(RootPath + "/Devices", "ShopGrowth");
            EnsureFolder(RootPath + "/Devices/ShopGrowth", "Common");
            EnsureFolder(RootPath + "/Devices/ShopGrowth", "Rare");

            // 2. Device SO 26종 생성/업데이트 (Common 18종 + Rare 8종)
            CreateAllDevicesV44();

            // 3. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 검증 실행
            VerifyDeviceAssetsV44();

            // 5. 결과 출력
            Debug.Log($"[DeviceSOV44Generator] v4.4 Device SO Complete. Created: {_createdCount}, Updated: {_updatedCount}");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[DeviceSOV44Generator] Created folder: {path}");
            }
        }

        // ── 헬퍼: LoadOrCreateAsset ────────────────────────────────────

        /// <summary>지정 경로에 에셋이 있으면 로드하고, 없으면 새로 생성한다.</summary>
        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                _updatedCount++;
                Debug.Log($"[DeviceSOV44Generator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[DeviceSOV44Generator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        /// <summary>SerializedObject에서 string 필드를 설정한다.</summary>
        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DeviceSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DeviceSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DeviceSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.floatValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 enum 필드를 안전하게 설정한다.</summary>
        private static bool SetEnum<TEnum>(SerializedObject so, string fieldName, TEnum value) where TEnum : Enum
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DeviceSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            string enumName = value.ToString();
            int index = Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[DeviceSOV44Generator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.enumValueIndex = index;
            return true;
        }

        /// <summary>SerializedObject 변경사항을 적용하고 Dirty 플래그를 설정한다.</summary>
        private static void ApplyAndDirty(SerializedObject so, UnityEngine.Object asset)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
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

            // 4. Device_ForceSpring: AddForceIfDiceIncluded, intValue=1
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
                Debug.LogError("[DeviceSOV44Generator] Failed to set some fields on Device asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[DeviceSOV44Generator] Applied Device v4.4 fields: {AssetDatabase.GetAssetPath(asset)} deviceId={deviceId}");
        }

        // ── v4.4 Device SO 검증 ────────────────────────────────────────

        /// <summary>v4.4 Device SO 검증. 모든 Device가 올바르게 생성/수정되었는지 확인한다.</summary>
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
                    Debug.LogError("[DeviceSOV44Generator] v4.4 Verification failed: Device asset not found at " + path);
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(device);
                SerializedProperty deviceIdProp = so.FindProperty("deviceId");
                if (deviceIdProp == null || string.IsNullOrEmpty(deviceIdProp.stringValue))
                {
                    Debug.LogError("[DeviceSOV44Generator] v4.4 Verification failed: Device " + path + " has empty deviceId");
                    allPassed = false;
                }

                SerializedProperty deviceTypeProp = so.FindProperty("deviceType");
                if (deviceTypeProp == null || deviceTypeProp.enumValueIndex == 0)
                {
                    Debug.LogError("[DeviceSOV44Generator] v4.4 Verification failed: Device " + path + " has deviceType=None");
                    allPassed = false;
                }
            }

            if (allPassed)
            {
                Debug.Log("[DeviceSOV44Generator] v4.4 Device SO Verification passed.");
            }
            else
            {
                Debug.LogError("[DeviceSOV44Generator] v4.4 Device SO Verification failed. Some assets have default values.");
            }
        }
    }
}
