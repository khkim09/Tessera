using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>Stage 1 RoundDefinitionSO를 v4.4 구조에 맞게 생성/업데이트하는 Editor 유틸리티다.</summary>
    public static class Stage01RoundAndDeviceAssetGenerator
    {
        /// <summary>Stage01 Round SO가 저장되는 폴더 경로다.</summary>
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        /// <summary>Stage01 Common Device SO가 저장되는 폴더 경로다.</summary>
        private const string CommonDeviceFolderPath = "Assets/_Project/ScriptableObjects/Devices/ShopGrowth/Common";

        /// <summary>TutorialTarget Round SO 경로다.</summary>
        private const string RoundPathTutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";

        /// <summary>Stage01 Round01 SO 경로다.</summary>
        private const string RoundPathRound01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";

        /// <summary>Stage01 Round02 SO 경로다.</summary>
        private const string RoundPathRound02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";

        /// <summary>Stage01 Boss SO 경로다.</summary>
        private const string RoundPathBoss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Tools/Tessera/Generate Stage 1 Round And Device Assets 메뉴 항목이다.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 Round And Device Assets")]
        private static void Generate()
        {
            InvokeGenerate();
        }

        /// <summary>Stage01AssetGenerator에서 호출하는 public 진입점이다.</summary>
        public static void InvokeGenerate()
        {
            EnsureFolderExists(StagesFolderPath, "Assets/_Project/ScriptableObjects/Stages", "Stage01");

            SlotPairDeviceDefinitionSO[] stage01OpponentDevicePool = LoadStage01OpponentDevicePool();

            CreateOrUpdateStageRoundTutorialTarget(stage01OpponentDevicePool);
            CreateOrUpdateStageRoundRound01(stage01OpponentDevicePool);
            CreateOrUpdateStageRoundRound02(stage01OpponentDevicePool);
            CreateOrUpdateStageRoundBoss(stage01OpponentDevicePool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Stage01RoundAndDeviceAssetGenerator] Generation complete.\n" +
                $"  Rounds: {RoundPathTutorialTarget}, {RoundPathRound01}, {RoundPathRound02}, {RoundPathBoss}\n" +
                $"  LoadedOpponentDevices: {stage01OpponentDevicePool.Length}");
        }

        /// <summary>지정 경로에 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
                return;

            AssetDatabase.CreateFolder(parentPath, folderName);
            Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created folder: {fullPath}");
        }

        /// <summary>Stage01 상대가 사용할 실제 Device 후보 풀을 로드한다.</summary>
        private static SlotPairDeviceDefinitionSO[] LoadStage01OpponentDevicePool()
        {
            List<SlotPairDeviceDefinitionSO> devices = new List<SlotPairDeviceDefinitionSO>();

            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_SafetyPin.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LeadWeight.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LowGear.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_UnstableFuse.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_AdderChip.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_OddAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_EvenAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ForceSpring.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ImpactNail.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_HeavyHammer.asset");

            return devices.ToArray();
        }

        /// <summary>지정 경로의 Device SO가 존재하면 목록에 추가한다.</summary>
        private static void AddDeviceIfExists(List<SlotPairDeviceDefinitionSO> devices, string assetPath)
        {
            SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(assetPath);

            if (device == null)
            {
                Debug.LogWarning($"[Stage01RoundAndDeviceAssetGenerator] Stage01 opponent device not found: {assetPath}");
                return;
            }

            devices.Add(device);
        }

        /// <summary>Stage01_TutorialTarget 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundTutorialTarget(SlotPairDeviceDefinitionSO[] opponentDevicePool)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathTutorialTarget);
            SerializedObject so = new SerializedObject(asset);

            bool applied = ApplyRoundCommonFields(
                so,
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
                playerMaxHP: 20,
                diceCount: 5,
                maxAttempts: 3,
                baseRollsPerAttempt: 3,
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
                opponentDevicePool: opponentDevicePool,
                minOpponentDeviceCount: 0,
                maxOpponentDeviceCount: 1,
                allowDuplicateOpponentDevices: false);

            FinishRoundAssetUpdate(asset, so, applied);
        }

        /// <summary>Stage01_Round01_TutorialNormal 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundRound01(SlotPairDeviceDefinitionSO[] opponentDevicePool)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathRound01);
            SerializedObject so = new SerializedObject(asset);

            bool applied = ApplyRoundCommonFields(
                so,
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
                playerMaxHP: 20,
                diceCount: 5,
                maxAttempts: 3,
                baseRollsPerAttempt: 3,
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
                opponentDevicePool: opponentDevicePool,
                minOpponentDeviceCount: 1,
                maxOpponentDeviceCount: 2,
                allowDuplicateOpponentDevices: false);

            FinishRoundAssetUpdate(asset, so, applied);
        }

        /// <summary>Stage01_Round02_NormalBounty 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundRound02(SlotPairDeviceDefinitionSO[] opponentDevicePool)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathRound02);
            SerializedObject so = new SerializedObject(asset);

            bool applied = ApplyRoundCommonFields(
                so,
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
                playerMaxHP: 20,
                diceCount: 5,
                maxAttempts: 3,
                baseRollsPerAttempt: 3,
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
                opponentDevicePool: opponentDevicePool,
                minOpponentDeviceCount: 1,
                maxOpponentDeviceCount: 3,
                allowDuplicateOpponentDevices: false);

            FinishRoundAssetUpdate(asset, so, applied);
        }

        /// <summary>Stage01_Boss_TheClerk 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundBoss(SlotPairDeviceDefinitionSO[] opponentDevicePool)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathBoss);
            SerializedObject so = new SerializedObject(asset);

            bool applied = ApplyRoundCommonFields(
                so,
                roundId: "stage01_boss_clerk",
                displayName: "Boss - The Clerk",
                roundType: StageRoundType.Boss,
                tutorialForcedRound: false,
                initiallyAvailable: true,
                baseRewardMoney: 25,
                bountyRank: 4,
                rewardOvercharge: 0,
                opponentMaxHP: 48,
                enemyStrikeDamage: 4,
                playerMaxHP: 20,
                diceCount: 5,
                maxAttempts: 4,
                baseRollsPerAttempt: 3,
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
                opponentDevicePool: opponentDevicePool,
                minOpponentDeviceCount: 2,
                maxOpponentDeviceCount: 4,
                allowDuplicateOpponentDevices: false);

            FinishRoundAssetUpdate(asset, so, applied);
        }

        /// <summary>지정 경로의 Round SO를 로드하거나 새로 생성한다.</summary>
        private static StageRoundDefinitionSO LoadOrCreateRoundAsset(string assetPath)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(assetPath);

            if (asset != null)
            {
                Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Updating existing round asset: {assetPath}");
                return asset;
            }

            asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            Debug.Log($"[Stage01RoundAndDeviceAssetGenerator] Created new round asset: {assetPath}");
            return asset;
        }

        /// <summary>Round SO 변경 내용을 적용하고 Dirty 처리한다.</summary>
        private static void FinishRoundAssetUpdate(StageRoundDefinitionSO asset, SerializedObject so, bool applied)
        {
            if (!applied)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Failed to apply some fields on round asset: {AssetDatabase.GetAssetPath(asset)}");
                return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>StageRoundDefinitionSO의 공통 Serialized 필드들을 설정한다.</summary>
        private static bool ApplyRoundCommonFields(
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
            int baseRollsPerAttempt,
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
            bool allOk = true;

            allOk &= SetString(so, "roundId", roundId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetEnum(so, "roundType", (int)roundType);
            allOk &= SetBool(so, "tutorialForcedRound", tutorialForcedRound);
            allOk &= SetBool(so, "initiallyAvailable", initiallyAvailable);
            allOk &= SetInt(so, "baseRewardMoney", baseRewardMoney);
            allOk &= SetInt(so, "bountyRank", bountyRank);
            allOk &= SetInt(so, "rewardOvercharge", rewardOvercharge);
            allOk &= SetInt(so, "opponentMaxHP", opponentMaxHP);
            allOk &= SetInt(so, "enemyStrikeDamage", enemyStrikeDamage);
            allOk &= SetInt(so, "playerMaxHP", playerMaxHP);
            allOk &= SetInt(so, "diceCount", diceCount);
            allOk &= SetInt(so, "maxAttempts", maxAttempts);
            allOk &= SetInt(so, "baseRollsPerAttempt", baseRollsPerAttempt);
            allOk &= SetInt(so, "impactCap", impactCap);
            allOk &= SetInt(so, "maxUsesPerCastPerRound", maxUsesPerCastPerRound);
            allOk &= SetInt(so, "maxBrokenCastUsesPerRound", maxBrokenCastUsesPerRound);
            allOk &= SetBool(so, "brokenCastGrantsOvercharge", brokenCastGrantsOvercharge);
            allOk &= SetInt(so, "brokenCastOverchargeAmount", brokenCastOverchargeAmount);
            allOk &= SetBool(so, "brokenCastGrantsNextAttemptFreeReroll", brokenCastGrantsNextAttemptFreeReroll);
            allOk &= SetInt(so, "brokenCastFreeRerollTokenAmount", brokenCastFreeRerollTokenAmount);
            allOk &= SetBool(so, "applyNonAcesCastPowerPenalty", applyNonAcesCastPowerPenalty);
            allOk &= SetInt(so, "nonAcesCastPowerPercent", nonAcesCastPowerPercent);
            allOk &= SetBool(so, "disableChance", disableChance);
            allOk &= SetBool(so, "disableBrokenCastReward", disableBrokenCastReward);
            allOk &= SetObjectArray(so, "opponentDevicePool", opponentDevicePool);
            allOk &= SetInt(so, "minOpponentDeviceCount", minOpponentDeviceCount);
            allOk &= SetInt(so, "maxOpponentDeviceCount", maxOpponentDeviceCount);
            allOk &= SetBool(so, "allowDuplicateOpponentDevices", allowDuplicateOpponentDevices);

            return allOk;
        }

        /// <summary>SerializedObject의 string 필드를 안전하게 설정한다.</summary>
        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Missing field: {fieldName} on {so.targetObject.name}");
                return false;
            }

            prop.stringValue = value ?? string.Empty;
            return true;
        }

        /// <summary>SerializedObject의 int 필드를 안전하게 설정한다.</summary>
        private static bool SetInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Missing field: {fieldName} on {so.targetObject.name}");
                return false;
            }

            prop.intValue = value;
            return true;
        }

        /// <summary>SerializedObject의 bool 필드를 안전하게 설정한다.</summary>
        private static bool SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Missing field: {fieldName} on {so.targetObject.name}");
                return false;
            }

            prop.boolValue = value;
            return true;
        }

        /// <summary>SerializedObject의 enum 필드를 안전하게 설정한다.</summary>
        private static bool SetEnum(SerializedObject so, string fieldName, int enumValueIndex)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Missing field: {fieldName} on {so.targetObject.name}");
                return false;
            }

            prop.enumValueIndex = enumValueIndex;
            return true;
        }

        /// <summary>SerializedObject의 Object 배열 필드를 안전하게 설정한다.</summary>
        private static bool SetObjectArray(
            SerializedObject so,
            string fieldName,
            SlotPairDeviceDefinitionSO[] values)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null || !prop.isArray)
            {
                Debug.LogError($"[Stage01RoundAndDeviceAssetGenerator] Missing array field: {fieldName} on {so.targetObject.name}");
                return false;
            }

            SlotPairDeviceDefinitionSO[] safeValues = values ?? new SlotPairDeviceDefinitionSO[0];

            prop.ClearArray();
            prop.arraySize = safeValues.Length;

            for (int i = 0; i < safeValues.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = safeValues[i];

            return true;
        }
    }
}
