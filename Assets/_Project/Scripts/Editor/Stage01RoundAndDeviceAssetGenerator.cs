using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// Stage 1 (Broken Cast) 테스트용 SlotPairDeviceDefinitionSO 및 StageRoundDefinitionSO 에셋을 생성/업데이트하는 Editor 유틸리티.
    /// Tools/Tessera/Generate Stage 1 Round And Device Assets 메뉴에서 실행한다.
    /// </summary>
    public static class Stage01RoundAndDeviceAssetGenerator
    {
        private const string DevicesFolderPath = "Assets/_Project/ScriptableObjects/Devices";
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        // ── Device 에셋 경로 ─────────────────────────────────────────────
        private const string DevicePath_DiceValueDoubler = DevicesFolderPath + "/SlotPairDevice_AddScore_DiceValueX2.asset";
        private const string DevicePath_PressureAmplifier = DevicesFolderPath + "/SlotPairDevice_ForceOver4_X1_5.asset";
        private const string DevicePath_EchoGear = DevicesFolderPath + "/SlotPairDevice_SamePreviousForce1.asset";
        private const string DevicePath_FlatScorer = DevicesFolderPath + "/SlotPairDevice_AddScore_Flat4.asset";
        private const string DevicePath_ForceCell = DevicesFolderPath + "/SlotPairDevice_AddForce_Flat1.asset";

        // ── Stage Round 에셋 경로 ────────────────────────────────────────
        private const string RoundPath_TutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";
        private const string RoundPath_Round01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";
        private const string RoundPath_Round02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";
        private const string RoundPath_Boss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Tools/Tessera/Generate Stage 1 Round And Device Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 Round And Device Assets")]
        private static void Generate()
        {
            EnsureFolderExists(DevicesFolderPath, "Assets/_Project/ScriptableObjects", "Devices");
            EnsureFolderExists(StagesFolderPath, "Assets/_Project/ScriptableObjects/Stages", "Stage01");

            // 1. Device 에셋 생성/업데이트
            SlotPairDeviceDefinitionSO deviceDiceValueDoubler = CreateOrUpdateDevice(
                DevicePath_DiceValueDoubler,
                "device.add_score.dice_value_x2",
                "Dice Value Doubler",
                "Adds current dice value x2 to Score.",
                SlotPairDeviceType.AddScoreByDiceValue,
                intValue: 2,
                floatValue: 1f,
                forceThreshold: 0f,
                RollPatternType.None);

            SlotPairDeviceDefinitionSO devicePressureAmplifier = CreateOrUpdateDevice(
                DevicePath_PressureAmplifier,
                "device.force_over_4_x1_5",
                "Pressure Amplifier",
                "Multiplies Force by 1.5 if current Force is at least 4.",
                SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast,
                intValue: 0,
                floatValue: 1.5f,
                forceThreshold: 4f,
                RollPatternType.None);

            SlotPairDeviceDefinitionSO deviceEchoGear = CreateOrUpdateDevice(
                DevicePath_EchoGear,
                "device.force.same_previous_1",
                "Echo Gear",
                "Adds Force +1 if this dice matches the previous slot dice.",
                SlotPairDeviceType.AddForceIfSameAsPrevious,
                intValue: 1,
                floatValue: 1f,
                forceThreshold: 0f,
                RollPatternType.None);

            // TODO: AddScoreFlat 타입이 존재하지 않아 AddScoreIfCastType(None)으로 대체.
            // 추후 AddScoreFlat enum이 추가되면 SlotPairDeviceType.AddScoreFlat으로 변경 필요.
            SlotPairDeviceDefinitionSO deviceFlatScorer = CreateOrUpdateDevice(
                DevicePath_FlatScorer,
                "device.add_score.flat_4",
                "Flat Scorer",
                "Adds Score +4 regardless of dice value.",
                SlotPairDeviceType.AddScoreIfCastType,
                intValue: 4,
                floatValue: 1f,
                forceThreshold: 0f,
                RollPatternType.None);

            // TODO: AddForceFlat 타입이 존재하지 않아 AddForceIfDiceIncluded로 대체.
            // 추후 AddForceFlat enum이 추가되면 SlotPairDeviceType.AddForceFlat으로 변경 필요.
            SlotPairDeviceDefinitionSO deviceForceCell = CreateOrUpdateDevice(
                DevicePath_ForceCell,
                "device.force.flat_1",
                "Force Cell",
                "Adds Force +1 if this dice is included in the cast calculation.",
                SlotPairDeviceType.AddForceIfDiceIncluded,
                intValue: 1,
                floatValue: 1f,
                forceThreshold: 0f,
                RollPatternType.None);

            AssetDatabase.SaveAssets();

            // 2. Stage Round 에셋 생성/업데이트
            CreateOrUpdateStageRound_TutorialTarget(
                deviceDiceValueDoubler,
                deviceEchoGear,
                devicePressureAmplifier);

            CreateOrUpdateStageRound_Round01(
                deviceDiceValueDoubler,
                deviceEchoGear,
                deviceForceCell);

            CreateOrUpdateStageRound_Round02(
                deviceDiceValueDoubler,
                deviceEchoGear,
                devicePressureAmplifier,
                deviceFlatScorer,
                deviceForceCell);

            CreateOrUpdateStageRound_Boss(
                deviceDiceValueDoubler,
                deviceEchoGear,
                devicePressureAmplifier,
                deviceFlatScorer,
                deviceForceCell);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Generation complete.\n" +
                      $"  Devices: {DevicePath_DiceValueDoubler}, {DevicePath_PressureAmplifier}, {DevicePath_EchoGear}, {DevicePath_FlatScorer}, {DevicePath_ForceCell}\n" +
                      $"  Rounds: {RoundPath_TutorialTarget}, {RoundPath_Round01}, {RoundPath_Round02}, {RoundPath_Boss}");
        }

        // ── 폴더 생성 ─────────────────────────────────────────────────────

        /// <summary>지정 경로에 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created folder: {fullPath}");
            }
        }

        // ── Device 생성/업데이트 ──────────────────────────────────────────

        /// <summary>
        /// 지정 경로에 SlotPairDeviceDefinitionSO 에셋이 없으면 새로 생성하고,
        /// 이미 존재하면 기존 에셋을 로드하여 필드 값을 업데이트한다.
        /// </summary>
        private static SlotPairDeviceDefinitionSO CreateOrUpdateDevice(
            string assetPath,
            string deviceId,
            string displayName,
            string description,
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType)
        {
            SlotPairDeviceDefinitionSO asset = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new device asset: {assetPath}");
            }
            else
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing device asset: {assetPath}");
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = deviceId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("deviceType").enumValueIndex = (int)deviceType;
            so.FindProperty("intValue").intValue = intValue;
            so.FindProperty("floatValue").floatValue = floatValue;
            so.FindProperty("forceThreshold").floatValue = forceThreshold;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)requiredPatternType;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ── Stage Round 생성/업데이트 ─────────────────────────────────────

        /// <summary>Stage01_TutorialTarget 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRound_TutorialTarget(
            SlotPairDeviceDefinitionSO deviceDiceValueDoubler,
            SlotPairDeviceDefinitionSO deviceEchoGear,
            SlotPairDeviceDefinitionSO devicePressureAmplifier)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPath_TutorialTarget);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
                AssetDatabase.CreateAsset(asset, RoundPath_TutorialTarget);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new round asset: {RoundPath_TutorialTarget}");
            }
            else
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing round asset: {RoundPath_TutorialTarget}");
            }

            SerializedObject so = new SerializedObject(asset);
            ApplyRoundCommonFields(so,
                roundId: "stage01_tutorial_normal",
                displayName: "Tutorial Target",
                roundType: StageRoundType.Normal,
                tutorialForcedRound: true,
                initiallyAvailable: true,
                baseRewardMoney: 10,
                bountyRank: 1,
                rewardOvercharge: 0,
                opponentMaxHP: 18,
                enemyStrikeDamage: 2,
                playerMaxHP: 100,
                diceCount: 5,
                maxAttempts: 3,
                roundRollPool: 8,
                impactCap: 0,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                applyNonAcesCastPowerPenalty: false,
                nonAcesCastPowerPercent: 50,
                disableChance: false,
                disableBrokenCastReward: false,
                opponentDevicePool: new SlotPairDeviceDefinitionSO[]
                {
                    deviceDiceValueDoubler,
                    deviceEchoGear,
                    devicePressureAmplifier
                },
                minOpponentDeviceCount: 0,
                maxOpponentDeviceCount: 1,
                allowDuplicateOpponentDevices: false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Stage01_Round01_TutorialNormal 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRound_Round01(
            SlotPairDeviceDefinitionSO deviceDiceValueDoubler,
            SlotPairDeviceDefinitionSO deviceEchoGear,
            SlotPairDeviceDefinitionSO deviceForceCell)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPath_Round01);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
                AssetDatabase.CreateAsset(asset, RoundPath_Round01);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new round asset: {RoundPath_Round01}");
            }
            else
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing round asset: {RoundPath_Round01}");
            }

            SerializedObject so = new SerializedObject(asset);
            ApplyRoundCommonFields(so,
                roundId: "stage01_round01_tutorial_normal",
                displayName: "Round 01 Tutorial Normal",
                roundType: StageRoundType.Normal,
                tutorialForcedRound: false,
                initiallyAvailable: true,
                baseRewardMoney: 12,
                bountyRank: 1,
                rewardOvercharge: 0,
                opponentMaxHP: 24,
                enemyStrikeDamage: 3,
                playerMaxHP: 100,
                diceCount: 5,
                maxAttempts: 3,
                roundRollPool: 8,
                impactCap: 0,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                applyNonAcesCastPowerPenalty: false,
                nonAcesCastPowerPercent: 50,
                disableChance: false,
                disableBrokenCastReward: false,
                opponentDevicePool: new SlotPairDeviceDefinitionSO[]
                {
                    deviceDiceValueDoubler,
                    deviceEchoGear,
                    deviceForceCell
                },
                minOpponentDeviceCount: 1,
                maxOpponentDeviceCount: 2,
                allowDuplicateOpponentDevices: false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Stage01_Round02_NormalBounty 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRound_Round02(
            SlotPairDeviceDefinitionSO deviceDiceValueDoubler,
            SlotPairDeviceDefinitionSO deviceEchoGear,
            SlotPairDeviceDefinitionSO devicePressureAmplifier,
            SlotPairDeviceDefinitionSO deviceFlatScorer,
            SlotPairDeviceDefinitionSO deviceForceCell)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPath_Round02);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
                AssetDatabase.CreateAsset(asset, RoundPath_Round02);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new round asset: {RoundPath_Round02}");
            }
            else
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing round asset: {RoundPath_Round02}");
            }

            SerializedObject so = new SerializedObject(asset);
            ApplyRoundCommonFields(so,
                roundId: "stage01_round02_normal_bounty",
                displayName: "Round 02 Normal Bounty",
                roundType: StageRoundType.Normal,
                tutorialForcedRound: false,
                initiallyAvailable: true,
                baseRewardMoney: 15,
                bountyRank: 2,
                rewardOvercharge: 0,
                opponentMaxHP: 28,
                enemyStrikeDamage: 3,
                playerMaxHP: 100,
                diceCount: 5,
                maxAttempts: 3,
                roundRollPool: 8,
                impactCap: 0,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                applyNonAcesCastPowerPenalty: false,
                nonAcesCastPowerPercent: 50,
                disableChance: false,
                disableBrokenCastReward: false,
                opponentDevicePool: new SlotPairDeviceDefinitionSO[]
                {
                    deviceDiceValueDoubler,
                    deviceEchoGear,
                    devicePressureAmplifier,
                    deviceFlatScorer,
                    deviceForceCell
                },
                minOpponentDeviceCount: 1,
                maxOpponentDeviceCount: 3,
                allowDuplicateOpponentDevices: false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Stage01_Boss_TheClerk 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRound_Boss(
            SlotPairDeviceDefinitionSO deviceDiceValueDoubler,
            SlotPairDeviceDefinitionSO deviceEchoGear,
            SlotPairDeviceDefinitionSO devicePressureAmplifier,
            SlotPairDeviceDefinitionSO deviceFlatScorer,
            SlotPairDeviceDefinitionSO deviceForceCell)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPath_Boss);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
                AssetDatabase.CreateAsset(asset, RoundPath_Boss);
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new round asset: {RoundPath_Boss}");
            }
            else
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing round asset: {RoundPath_Boss}");
            }

            SerializedObject so = new SerializedObject(asset);
            ApplyRoundCommonFields(so,
                roundId: "stage01_boss_clerk",
                displayName: "Boss - The Clerk",
                roundType: StageRoundType.Boss,
                tutorialForcedRound: false,
                initiallyAvailable: true,
                baseRewardMoney: 25,
                bountyRank: 3,
                rewardOvercharge: 0,
                opponentMaxHP: 48,
                enemyStrikeDamage: 4,
                playerMaxHP: 100,
                diceCount: 5,
                maxAttempts: 4,
                roundRollPool: 8,
                impactCap: 20,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                applyNonAcesCastPowerPenalty: true,
                nonAcesCastPowerPercent: 50,
                disableChance: false,
                disableBrokenCastReward: false,
                opponentDevicePool: new SlotPairDeviceDefinitionSO[]
                {
                    deviceDiceValueDoubler,
                    deviceEchoGear,
                    devicePressureAmplifier,
                    deviceFlatScorer,
                    deviceForceCell
                },
                minOpponentDeviceCount: 3,
                maxOpponentDeviceCount: 5,
                allowDuplicateOpponentDevices: false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        // ── 공통 필드 설정 ────────────────────────────────────────────────

        /// <summary>StageRoundDefinitionSO의 공통 Serialized 필드들을 설정한다.</summary>
        private static void ApplyRoundCommonFields(
            SerializedObject so,
            string roundId,
            string displayName,
            StageRoundType roundType,
            bool tutorialForcedRound,
            bool initiallyAvailable,
            int baseRewardMoney,
            int bountyRank,
            int rewardOvercharge,
            int opponentMaxHP,
            int enemyStrikeDamage,
            int playerMaxHP,
            int diceCount,
            int maxAttempts,
            int roundRollPool,
            int impactCap,
            int maxUsesPerCastPerRound,
            int maxBrokenCastUsesPerRound,
            bool brokenCastGrantsOvercharge,
            int brokenCastOverchargeAmount,
            bool brokenCastGrantsNextAttemptFreeReroll,
            int brokenCastFreeRerollTokenAmount,
            bool applyNonAcesCastPowerPenalty,
            int nonAcesCastPowerPercent,
            bool disableChance,
            bool disableBrokenCastReward,
            SlotPairDeviceDefinitionSO[] opponentDevicePool,
            int minOpponentDeviceCount,
            int maxOpponentDeviceCount,
            bool allowDuplicateOpponentDevices)
        {
            so.FindProperty("roundId").stringValue = roundId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("roundType").enumValueIndex = (int)roundType;
            so.FindProperty("tutorialForcedRound").boolValue = tutorialForcedRound;
            so.FindProperty("initiallyAvailable").boolValue = initiallyAvailable;
            so.FindProperty("baseRewardMoney").intValue = baseRewardMoney;
            so.FindProperty("bountyRank").intValue = bountyRank;
            so.FindProperty("rewardOvercharge").intValue = rewardOvercharge;
            so.FindProperty("opponentMaxHP").intValue = opponentMaxHP;
            so.FindProperty("enemyStrikeDamage").intValue = enemyStrikeDamage;
            so.FindProperty("playerMaxHP").intValue = playerMaxHP;
            so.FindProperty("diceCount").intValue = diceCount;
            so.FindProperty("maxAttempts").intValue = maxAttempts;
            so.FindProperty("roundRollPool").intValue = roundRollPool;
            so.FindProperty("impactCap").intValue = impactCap;
            so.FindProperty("maxUsesPerCastPerRound").intValue = maxUsesPerCastPerRound;
            so.FindProperty("maxBrokenCastUsesPerRound").intValue = maxBrokenCastUsesPerRound;
            so.FindProperty("brokenCastGrantsOvercharge").boolValue = brokenCastGrantsOvercharge;
            so.FindProperty("brokenCastOverchargeAmount").intValue = brokenCastOverchargeAmount;
            so.FindProperty("brokenCastGrantsNextAttemptFreeReroll").boolValue = brokenCastGrantsNextAttemptFreeReroll;
            so.FindProperty("brokenCastFreeRerollTokenAmount").intValue = brokenCastFreeRerollTokenAmount;
            so.FindProperty("applyNonAcesCastPowerPenalty").boolValue = applyNonAcesCastPowerPenalty;
            so.FindProperty("nonAcesCastPowerPercent").intValue = nonAcesCastPowerPercent;
            so.FindProperty("disableChance").boolValue = disableChance;
            so.FindProperty("disableBrokenCastReward").boolValue = disableBrokenCastReward;

            // Opponent Device Pool 배열 설정
            SerializedProperty poolProp = so.FindProperty("opponentDevicePool");
            if (poolProp != null && poolProp.isArray)
            {
                poolProp.ClearArray();
                poolProp.arraySize = opponentDevicePool.Length;
                for (int i = 0; i < opponentDevicePool.Length; i++)
                {
                    poolProp.GetArrayElementAtIndex(i).objectReferenceValue = opponentDevicePool[i];
                }
            }

            so.FindProperty("minOpponentDeviceCount").intValue = minOpponentDeviceCount;
            so.FindProperty("maxOpponentDeviceCount").intValue = maxOpponentDeviceCount;
            so.FindProperty("allowDuplicateOpponentDevices").boolValue = allowDuplicateOpponentDevices;
        }
    }
}
