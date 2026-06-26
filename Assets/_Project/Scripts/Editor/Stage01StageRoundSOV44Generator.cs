using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>Stage 1 Round SO를 v4.4 구조에 맞게 생성/업데이트하는 Editor 유틸리티다.</summary>
    public static class Stage01StageRoundSOV44Generator
    {
        /// <summary>Stage01 Round SO가 저장되는 폴더 경로다.</summary>
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        /// <summary>Stage01 Common Device SO 폴더 경로다.</summary>
        private const string CommonDeviceFolderPath = "Assets/_Project/ScriptableObjects/Devices/ShopGrowth/Common";

        /// <summary>Stage01 Rare Device SO 폴더 경로다.</summary>
        private const string RareDeviceFolderPath = "Assets/_Project/ScriptableObjects/Devices/ShopGrowth/Rare";

        /// <summary>Stage01 Enemy Intent Profile 폴더 경로다.</summary>
        private const string ProfilesFolderPath = "Assets/_Project/ScriptableObjects/Enemies/IntentProfiles";

        /// <summary>Stage01 Enemy Dice Loadout 폴더 경로다.</summary>
        private const string DiceLoadoutsFolderPath = "Assets/_Project/ScriptableObjects/Enemies/DiceLoadouts";

        /// <summary>Stage01_TutorialTarget Round SO 경로다.</summary>
        private const string RoundPathTutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";

        /// <summary>Stage01_Round01_TutorialNormal Round SO 경로다.</summary>
        private const string RoundPathRound01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";

        /// <summary>Stage01_Round02_NormalBounty Round SO 경로다.</summary>
        private const string RoundPathRound02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";

        /// <summary>Stage01_Boss_TheClerk Round SO 경로다.</summary>
        private const string RoundPathBoss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Stage_01_BrokenLedger StageDefinitionSO 경로다.</summary>
        private const string StageDefinitionPath = StagesFolderPath + "/Stage_01_BrokenLedger.asset";

        /// <summary>Stage01 WorkshopRules SO 경로다.</summary>
        private const string WorkshopRulesPath = StagesFolderPath + "/Stage01_WorkshopRules.asset";

        /// <summary>Tools/Tessera/Assets/Generate Stage Round SO v4.4 메뉴 항목이다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate Stage Round SO v4.4")]
        private static void GenerateFromMenu()
        {
            GenerateForPipeline();
        }

        /// <summary>v4.4 통합 생성 파이프라인에서 호출하는 진입점이다.</summary>
        public static void GenerateForPipeline()
        {
            List<string> createdOrUpdatedAssets = new List<string>();

            EnsureFolderExists(StagesFolderPath, "Assets/_Project/ScriptableObjects/Stages", "Stage01");

            // 1. StageRoundDefinitionSO 4종 생성/수정
            SlotPairDeviceDefinitionSO[] commonDevices = LoadCommonDevicePool();
            SlotPairDeviceDefinitionSO[] bossDevices = LoadBossDevicePool();

            EnemyIntentProfileSO profileTutorial = LoadProfile("Profile_Tutorial_CleanStrike");
            EnemyIntentProfileSO profilePlayerOpening = LoadProfile("Profile_Stage01_PlayerOpening");
            EnemyIntentProfileSO profileAggression = LoadProfile("Profile_Stage01_Aggression");
            EnemyIntentProfileSO profileBoss = LoadProfile("Profile_Stage01_Boss");

            EnemyDiceLoadoutDefinitionSO loadoutTutorial = LoadLoadout("Loadout_TutorialLow");
            EnemyDiceLoadoutDefinitionSO loadoutBalanced = LoadLoadout("Loadout_Stage01Balanced");
            EnemyDiceLoadoutDefinitionSO loadoutAggressive = LoadLoadout("Loadout_Stage01Aggressive");
            EnemyDiceLoadoutDefinitionSO loadoutBoss = LoadLoadout("Loadout_Stage01Boss");

            CreateOrUpdateStageRoundTutorialTarget(commonDevices, profileTutorial, loadoutTutorial, createdOrUpdatedAssets);
            CreateOrUpdateStageRoundRound01(commonDevices, profilePlayerOpening, loadoutBalanced, createdOrUpdatedAssets);
            CreateOrUpdateStageRoundRound02(commonDevices, profileAggression, loadoutAggressive, createdOrUpdatedAssets);
            CreateOrUpdateStageRoundBoss(bossDevices, profileBoss, loadoutBoss, createdOrUpdatedAssets);

            // 2. StageWorkshopRulesSO 생성/수정
            StageWorkshopRulesSO workshopRules = CreateOrUpdateWorkshopRules(createdOrUpdatedAssets);

            // 3. StageDefinitionSO 생성/수정 및 RoundDefinitions + WorkshopRules 연결
            CreateOrUpdateStageDefinition(workshopRules, createdOrUpdatedAssets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 생성 후 검증
            ValidateGeneratedAssets();

            Debug.Log("[Stage01StageRoundSOV44Generator] Generation complete.\n" +
                      "  Created/Updated assets:\n" +
                      "  " + string.Join("\n  ", createdOrUpdatedAssets));
        }

        /// <summary>지정 경로에 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
                return;

            AssetDatabase.CreateFolder(parentPath, folderName);
            Debug.Log($"[Stage01StageRoundSOV44Generator] Created folder: {fullPath}");
        }

        /// <summary>Stage01 상대가 사용할 Common Device 후보 풀을 로드한다.</summary>
        private static SlotPairDeviceDefinitionSO[] LoadCommonDevicePool()
        {
            List<SlotPairDeviceDefinitionSO> devices = new List<SlotPairDeviceDefinitionSO>();

            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_SafetyPin.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_UnstableFuse.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LeadWeight.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LowGear.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_AdderChip.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_OddAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_EvenAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ForceSpring.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ImpactNail.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_HeavyHammer.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_CastStampAces.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_CastStampChance.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_PairContact.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LeftCoupler.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_RelayMotor.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_FrontLoader.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_EndValveLight.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_PressureGaugeLight.asset");

            return devices.ToArray();
        }

        /// <summary>Stage01 Boss Round에서 사용할 Common + Rare Device 후보 풀을 로드한다.</summary>
        private static SlotPairDeviceDefinitionSO[] LoadBossDevicePool()
        {
            List<SlotPairDeviceDefinitionSO> devices = new List<SlotPairDeviceDefinitionSO>();

            // Common Device
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_SafetyPin.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_UnstableFuse.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LeadWeight.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LowGear.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_AdderChip.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_OddAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_EvenAmplifier.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ForceSpring.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_ImpactNail.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_HeavyHammer.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_CastStampAces.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_CastStampChance.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_PairContact.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_LeftCoupler.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_RelayMotor.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_FrontLoader.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_EndValveLight.asset");
            AddDeviceIfExists(devices, CommonDeviceFolderPath + "/Device_PressureGaugeLight.asset");

            // Rare Device (Boss 전용)
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_MirrorShard.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_IsolatedGear.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_FullHouseBracket.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_StraightRail.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_HighVoltagePin.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_EndValve.asset");
            AddDeviceIfExists(devices, RareDeviceFolderPath + "/Device_StagePressureMeter.asset");

            return devices.ToArray();
        }

        /// <summary>지정 경로의 Device SO가 존재하면 목록에 추가한다. 없으면 Warning을 출력한다.</summary>
        private static void AddDeviceIfExists(List<SlotPairDeviceDefinitionSO> devices, string assetPath)
        {
            SlotPairDeviceDefinitionSO device = AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(assetPath);

            if (device == null)
            {
                Debug.LogWarning($"[Stage01StageRoundSOV44Generator] Device not found (skipped): {assetPath}");
                return;
            }

            devices.Add(device);
        }

        /// <summary>지정 이름의 EnemyIntentProfileSO를 로드한다.</summary>
        private static EnemyIntentProfileSO LoadProfile(string profileName)
        {
            string path = ProfilesFolderPath + "/" + profileName + ".asset";
            EnemyIntentProfileSO profile = AssetDatabase.LoadAssetAtPath<EnemyIntentProfileSO>(path);

            if (profile == null)
                Debug.LogWarning($"[Stage01StageRoundSOV44Generator] Profile not found: {path}");

            return profile;
        }

        /// <summary>지정 이름의 EnemyDiceLoadoutDefinitionSO를 로드한다.</summary>
        private static EnemyDiceLoadoutDefinitionSO LoadLoadout(string loadoutName)
        {
            string path = DiceLoadoutsFolderPath + "/" + loadoutName + ".asset";
            EnemyDiceLoadoutDefinitionSO loadout = AssetDatabase.LoadAssetAtPath<EnemyDiceLoadoutDefinitionSO>(path);

            if (loadout == null)
                Debug.LogWarning($"[Stage01StageRoundSOV44Generator] Loadout not found: {path}");

            return loadout;
        }

        /// <summary>Stage01_TutorialTarget 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundTutorialTarget(
            SlotPairDeviceDefinitionSO[] opponentDevicePool,
            EnemyIntentProfileSO intentProfile,
            EnemyDiceLoadoutDefinitionSO opponentDiceLoadout,
            List<string> createdOrUpdatedAssets)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathTutorialTarget, createdOrUpdatedAssets);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            ApplyRoundCommonFields(
                so,
                roundId: "stage01_tutorial_target",
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
                opponentBaseRollsPerAttempt: 3,
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
                maxOpponentDeviceCount: 0,
                allowDuplicateOpponentDevices: false,
                intentProfile: intentProfile,
                opponentDiceLoadout: opponentDiceLoadout,
                bountyDescription: "첫 튜토리얼 수배지. 상대는 낮은 주사위와 제한된 Roll로 기본 공방을 학습시킨다.",
                intentDescription: "상대가 먼저 굴리지만 Device를 사용하지 않는다.",
                specialRuleDescription: "Tutorial Target에서는 상대 Device가 등장하지 않는다.",
                rewardDescription: "첫 튜토리얼 보상.");

            FinishRoundAssetUpdate(asset, so);
        }

        /// <summary>Stage01_Round01_TutorialNormal 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundRound01(
            SlotPairDeviceDefinitionSO[] opponentDevicePool,
            EnemyIntentProfileSO intentProfile,
            EnemyDiceLoadoutDefinitionSO opponentDiceLoadout,
            List<string> createdOrUpdatedAssets)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathRound01, createdOrUpdatedAssets);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            ApplyRoundCommonFields(
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
                opponentBaseRollsPerAttempt: 3,
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
                allowDuplicateOpponentDevices: false,
                intentProfile: intentProfile,
                opponentDiceLoadout: opponentDiceLoadout,
                bountyDescription: "기본 수배지. 플레이어가 먼저 CastPower를 만들고 상대가 대응한다.",
                intentDescription: "Opening Window 프로필을 사용해 플레이어 선공 흐름을 학습시킨다.",
                specialRuleDescription: "기본 Stage 01 규칙. Broken Cast는 Overcharge와 다음 Attempt Free Roll Token을 제공한다.",
                rewardDescription: "Stage 01 기본 보상.");

            FinishRoundAssetUpdate(asset, so);
        }

        /// <summary>Stage01_Round02_NormalBounty 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundRound02(
            SlotPairDeviceDefinitionSO[] opponentDevicePool,
            EnemyIntentProfileSO intentProfile,
            EnemyDiceLoadoutDefinitionSO opponentDiceLoadout,
            List<string> createdOrUpdatedAssets)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathRound02, createdOrUpdatedAssets);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            ApplyRoundCommonFields(
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
                opponentBaseRollsPerAttempt: 3,
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
                allowDuplicateOpponentDevices: false,
                intentProfile: intentProfile,
                opponentDiceLoadout: opponentDiceLoadout,
                bountyDescription: "Stage 01의 일반 수배지. 상대는 더 공격적인 Loadout과 Intent로 압박한다.",
                intentDescription: "Aggression 프로필을 사용해 상대가 먼저 압박하는 흐름을 만든다.",
                specialRuleDescription: "상대 Device 수가 증가하며 CastPower 경쟁이 강화된다.",
                rewardDescription: "Stage 01 일반 수배지 보상.");

            FinishRoundAssetUpdate(asset, so);
        }

        /// <summary>Stage01_Boss_TheClerk 에셋을 생성/업데이트한다.</summary>
        private static void CreateOrUpdateStageRoundBoss(
            SlotPairDeviceDefinitionSO[] opponentDevicePool,
            EnemyIntentProfileSO intentProfile,
            EnemyDiceLoadoutDefinitionSO opponentDiceLoadout,
            List<string> createdOrUpdatedAssets)
        {
            StageRoundDefinitionSO asset = LoadOrCreateRoundAsset(RoundPathBoss, createdOrUpdatedAssets);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            ApplyRoundCommonFields(
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
                opponentBaseRollsPerAttempt: 4,
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
                allowDuplicateOpponentDevices: false,
                intentProfile: intentProfile,
                opponentDiceLoadout: opponentDiceLoadout,
                bountyDescription: "Stage 01 보스. 장부를 정산하기 전 마지막 공방이다.",
                intentDescription: "Boss 프로필을 사용해 높은 CastPower와 처치 가능성을 우선한다.",
                specialRuleDescription: "Non-Aces CastPower가 감소하고 ImpactCap이 적용된다.",
                rewardDescription: "Stage 01 클리어 보상.");

            FinishRoundAssetUpdate(asset, so);
        }

        /// <summary>Stage_01_BrokenLedger StageDefinitionSO를 생성/수정하고 RoundDefinitions와 WorkshopRules를 연결한다.</summary>
        private static void CreateOrUpdateStageDefinition(
            StageWorkshopRulesSO workshopRules,
            List<string> createdOrUpdatedAssets)
        {
            StageDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StageDefinitionPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageDefinitionSO>();
                AssetDatabase.CreateAsset(asset, StageDefinitionPath);
                createdOrUpdatedAssets.Add(StageDefinitionPath + " (created)");
                Debug.Log($"[Stage01StageRoundSOV44Generator] Created new stage definition asset: {StageDefinitionPath}");
            }
            else
            {
                createdOrUpdatedAssets.Add(StageDefinitionPath + " (updated)");
                Debug.Log($"[Stage01StageRoundSOV44Generator] Updating existing stage definition asset: {StageDefinitionPath}");
            }

            SerializedObject so = new SerializedObject(asset);
            so.Update();

            SetInt(so, "stageNumber", 1);
            SetString(so, "displayName", "Stage 01 - Broken Ledger");
            SetString(so, "descentLabel", "Depth 1");
            SetString(so, "stageDescription", "The Broken Ledger awaits.");

            // RoundDefinitions 배열 설정
            StageRoundDefinitionSO round1 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathTutorialTarget);
            StageRoundDefinitionSO round2 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathRound01);
            StageRoundDefinitionSO round3 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathRound02);
            StageRoundDefinitionSO round4 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathBoss);

            StageRoundDefinitionSO[] roundDefinitions = new StageRoundDefinitionSO[]
            {
                round1,
                round2,
                round3,
                round4
            };

            SerializedProperty roundDefsProp = so.FindProperty("roundDefinitions");
            if (roundDefsProp != null && roundDefsProp.isArray)
            {
                roundDefsProp.ClearArray();
                roundDefsProp.arraySize = roundDefinitions.Length;

                for (int i = 0; i < roundDefinitions.Length; i++)
                    roundDefsProp.GetArrayElementAtIndex(i).objectReferenceValue = roundDefinitions[i];
            }

            // WorkshopRules 연결 (항상 workshopRules 인자를 사용)
            SerializedProperty workshopProp = so.FindProperty("workshopRules");
            if (workshopProp != null)
                workshopProp.objectReferenceValue = workshopRules;

            bool applied = so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

#if UNITY_2021_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#endif

            if (!applied)
                Debug.LogWarning("[Stage01StageRoundSOV44Generator] 변경사항이 없거나 적용되지 않았습니다: " + AssetDatabase.GetAssetPath(asset));
        }

        /// <summary>지정 경로의 Round SO를 로드하거나 새로 생성한다.</summary>
        private static StageRoundDefinitionSO LoadOrCreateRoundAsset(string assetPath, List<string> createdOrUpdatedAssets)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(assetPath);

            if (asset != null)
            {
                createdOrUpdatedAssets.Add(assetPath + " (updated)");
                Debug.Log($"[Stage01StageRoundSOV44Generator] Updating existing round asset: {assetPath}");
                return asset;
            }

            asset = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            createdOrUpdatedAssets.Add(assetPath + " (created)");
            Debug.Log($"[Stage01StageRoundSOV44Generator] Created new round asset: {assetPath}");
            return asset;
        }

        /// <summary>Round SO 변경 내용을 적용하고 즉시 저장 대상으로 표시한다.</summary>
        private static void FinishRoundAssetUpdate(StageRoundDefinitionSO asset, SerializedObject so)
        {
            bool applied = so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);

#if UNITY_2021_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#endif

            if (!applied)
                Debug.LogWarning("[Stage01StageRoundSOV44Generator] 변경사항이 없거나 적용되지 않았습니다: " + AssetDatabase.GetAssetPath(asset));
        }

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
            int opponentBaseRollsPerAttempt,
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
            bool allowDuplicateOpponentDevices,
            EnemyIntentProfileSO intentProfile,
            EnemyDiceLoadoutDefinitionSO opponentDiceLoadout,
            string bountyDescription,
            string intentDescription,
            string specialRuleDescription,
            string rewardDescription)
        {
            SetString(so, "roundId", roundId);
            SetString(so, "displayName", displayName);
            SetEnum(so, "roundType", (int)roundType);
            SetBool(so, "tutorialForcedRound", tutorialForcedRound);
            SetBool(so, "initiallyAvailable", initiallyAvailable);
            SetInt(so, "baseRewardMoney", baseRewardMoney);
            SetInt(so, "bountyRank", bountyRank);
            SetInt(so, "rewardOvercharge", rewardOvercharge);
            SetInt(so, "opponentMaxHP", opponentMaxHP);
            SetInt(so, "enemyStrikeDamage", enemyStrikeDamage);
            SetInt(so, "playerMaxHP", playerMaxHP);
            SetInt(so, "diceCount", diceCount);
            SetInt(so, "maxAttempts", maxAttempts);
            SetInt(so, "opponentBaseRollsPerAttempt", opponentBaseRollsPerAttempt);
            SetInt(so, "impactCap", impactCap);
            SetInt(so, "maxUsesPerCastPerRound", maxUsesPerCastPerRound);
            SetInt(so, "maxBrokenCastUsesPerRound", maxBrokenCastUsesPerRound);
            SetBool(so, "brokenCastGrantsOvercharge", brokenCastGrantsOvercharge);
            SetInt(so, "brokenCastOverchargeAmount", brokenCastOverchargeAmount);
            SetBool(so, "brokenCastGrantsNextAttemptFreeReroll", brokenCastGrantsNextAttemptFreeReroll);
            SetInt(so, "brokenCastFreeRerollTokenAmount", brokenCastFreeRerollTokenAmount);
            SetBool(so, "applyNonAcesCastPowerPenalty", applyNonAcesCastPowerPenalty);
            SetInt(so, "nonAcesCastPowerPercent", nonAcesCastPowerPercent);
            SetBool(so, "disableChance", disableChance);
            SetBool(so, "disableBrokenCastReward", disableBrokenCastReward);
            SetObjectArray(so, "opponentDevicePool", opponentDevicePool);
            SetInt(so, "minOpponentDeviceCount", minOpponentDeviceCount);
            SetInt(so, "maxOpponentDeviceCount", maxOpponentDeviceCount);
            SetBool(so, "allowDuplicateOpponentDevices", allowDuplicateOpponentDevices);

            // intentProfile 연결 (direct openingIntent / intentDeck은 설정하지 않음)
            SetObjectReference(so, "intentProfile", intentProfile);

            // opponentDiceLoadout 연결
            SetObjectReference(so, "opponentDiceLoadout", opponentDiceLoadout);

            // 설명 필드 설정
            SetString(so, "bountyDescription", bountyDescription);
            SetString(so, "intentDescription", intentDescription);
            SetString(so, "specialRuleDescription", specialRuleDescription);
            SetString(so, "rewardDescription", rewardDescription);
        }

        /// <summary>Stage01 WorkshopRules SO를 생성하거나 v4.4 기본 슬롯 규칙으로 업데이트한다.</summary>
        private static StageWorkshopRulesSO CreateOrUpdateWorkshopRules(List<string> createdOrUpdatedAssets)
        {
            StageWorkshopRulesSO asset = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageWorkshopRulesSO>();
                AssetDatabase.CreateAsset(asset, WorkshopRulesPath);
                createdOrUpdatedAssets.Add(WorkshopRulesPath + " (created)");
                Debug.Log($"[Stage01StageRoundSOV44Generator] Created new workshop rules asset: {WorkshopRulesPath}");
            }
            else
            {
                createdOrUpdatedAssets.Add(WorkshopRulesPath + " (updated)");
                Debug.Log($"[Stage01StageRoundSOV44Generator] Updating existing workshop rules asset: {WorkshopRulesPath}");
            }

            SerializedObject so = new SerializedObject(asset);
            so.Update();

            // 기본 Workshop Tier 설정
            SetInt(so, "baseWorkshopTier", 1);
            SetInt(so, "minShopProductTier", 1);
            SetInt(so, "maxShopProductTier", 2);
            SetInt(so, "tierUpgradeOverchargeCost", 1);
            SetInt(so, "maxTierUpgradePerVisit", 1);
            SetInt(so, "tierIncreasePerUpgrade", 1);

            // Shop 슬롯 설정
            SetInt(so, "productSlotCount", 6);
            SetBool(so, "allowDuplicateProducts", false);

            // productSlotRules 배열: 6개 슬롯 생성
            SerializedProperty slotRulesProp = so.FindProperty("productSlotRules");
            if (slotRulesProp != null && slotRulesProp.isArray)
            {
                slotRulesProp.ClearArray();
                slotRulesProp.arraySize = 6;

                // Slot 0: Left Device, AllowedTypes = Device, Tier 1~1
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(0),
                    "Left Device", new ShopProductType[] { ShopProductType.Device }, 1, 1);

                // Slot 1: Right Device, AllowedTypes = Device, Tier 1~2
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(1),
                    "Right Device", new ShopProductType[] { ShopProductType.Device }, 1, 2);

                // Slot 2: Left Dice Type, AllowedTypes = DiceSet, Tier 1~1
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(2),
                    "Left Dice Type", new ShopProductType[] { ShopProductType.DiceSet }, 1, 1);

                // Slot 3: Right Dice Type, AllowedTypes = DiceSet, Tier 1~2
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(3),
                    "Right Dice Type", new ShopProductType[] { ShopProductType.DiceSet }, 1, 2);

                // Slot 4: Left Face Upgrade, AllowedTypes = DiceFaceUpgrade, Tier 1~1
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(4),
                    "Left Face Upgrade", new ShopProductType[] { ShopProductType.DiceFaceUpgrade }, 1, 1);

                // Slot 5: Right Face Upgrade, AllowedTypes = DiceFaceUpgrade, Tier 1~2
                SetupProductSlotRule(slotRulesProp.GetArrayElementAtIndex(5),
                    "Right Face Upgrade", new ShopProductType[] { ShopProductType.DiceFaceUpgrade }, 1, 2);
            }

            bool applied = so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

#if UNITY_2021_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#endif

            if (!applied)
                Debug.LogWarning("[Stage01StageRoundSOV44Generator] WorkshopRules 변경사항이 없거나 적용되지 않았습니다: " + AssetDatabase.GetAssetPath(asset));

            return asset;
        }

        /// <summary>productSlotRules 배열의 단일 요소 SerializedProperty에 displayName/allowedProductTypes/minTierOverride/maxTierOverride를 설정하고 productPool은 비운다.</summary>
        private static void SetupProductSlotRule(
            SerializedProperty slotProp,
            string displayName,
            ShopProductType[] allowedTypes,
            int minTier,
            int maxTier)
        {
            SerializedProperty displayNameProp = slotProp.FindPropertyRelative("displayName");
            if (displayNameProp != null)
                displayNameProp.stringValue = displayName;

            SerializedProperty allowedTypesProp = slotProp.FindPropertyRelative("allowedProductTypes");
            if (allowedTypesProp != null && allowedTypesProp.isArray)
            {
                allowedTypesProp.ClearArray();
                allowedTypesProp.arraySize = allowedTypes.Length;
                for (int i = 0; i < allowedTypes.Length; i++)
                {
                    SerializedProperty elementProp = allowedTypesProp.GetArrayElementAtIndex(i);
                    elementProp.intValue = (int)allowedTypes[i];
                }
            }

            // productPool은 비워둔다 (ShopProduct 생성 전)
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");
            if (productPoolProp != null && productPoolProp.isArray)
            {
                productPoolProp.ClearArray();
                productPoolProp.arraySize = 0;
            }

            SerializedProperty minTierProp = slotProp.FindPropertyRelative("minTierOverride");
            if (minTierProp != null)
                minTierProp.intValue = minTier;

            SerializedProperty maxTierProp = slotProp.FindPropertyRelative("maxTierOverride");
            if (maxTierProp != null)
                maxTierProp.intValue = maxTier;
        }

        /// <summary>생성된 Stage01 Round/Stage/WorkshopRules 연결 상태를 검증한다.</summary>
        private static void ValidateGeneratedAssets()
        {
            // Round 검증
            StageRoundDefinitionSO tutorialTarget = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathTutorialTarget);
            StageRoundDefinitionSO round01 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathRound01);
            StageRoundDefinitionSO round02 = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathRound02);
            StageRoundDefinitionSO boss = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(RoundPathBoss);

            bool allRoundsValid = true;

            // TutorialTarget 검증
            if (tutorialTarget == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage01_TutorialTarget이 null입니다.");
                allRoundsValid = false;
            }
            else
            {
                if (tutorialTarget.RoundId != "stage01_tutorial_target")
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: TutorialTarget.RoundId = '{tutorialTarget.RoundId}', 기대값 'stage01_tutorial_target'");
                    allRoundsValid = false;
                }
                if (tutorialTarget.DisplayName != "Tutorial Target")
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: TutorialTarget.DisplayName = '{tutorialTarget.DisplayName}', 기대값 'Tutorial Target'");
                    allRoundsValid = false;
                }
                if (tutorialTarget.IntentProfile == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: TutorialTarget.IntentProfile이 null입니다.");
                    allRoundsValid = false;
                }
                if (tutorialTarget.OpponentDiceLoadout == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: TutorialTarget.OpponentDiceLoadout이 null입니다.");
                    allRoundsValid = false;
                }
                if (tutorialTarget.OpponentBaseRollsPerAttempt != 3)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: TutorialTarget.OpponentBaseRollsPerAttempt = {tutorialTarget.OpponentBaseRollsPerAttempt}, 기대값 2");
                    allRoundsValid = false;
                }
            }

            // Round01 검증
            if (round01 == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage01_Round01_TutorialNormal이 null입니다.");
                allRoundsValid = false;
            }
            else
            {
                if (round01.IntentProfile == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round01.IntentProfile이 null입니다.");
                    allRoundsValid = false;
                }
                if (round01.OpponentDiceLoadout == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round01.OpponentDiceLoadout이 null입니다.");
                    allRoundsValid = false;
                }
                if (round01.OpponentBaseRollsPerAttempt != 3)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Round01.OpponentBaseRollsPerAttempt = {round01.OpponentBaseRollsPerAttempt}, 기대값 3");
                    allRoundsValid = false;
                }
                if (round01.OpponentDevicePool == null || round01.OpponentDevicePool.Count == 0)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round01.OpponentDevicePool이 null이거나 비어 있습니다.");
                    allRoundsValid = false;
                }
                else
                {
                    for (int i = 0; i < round01.OpponentDevicePool.Count; i++)
                    {
                        if (round01.OpponentDevicePool[i] == null)
                        {
                            Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Round01.OpponentDevicePool[{i}]가 null입니다.");
                            allRoundsValid = false;
                        }
                    }
                }
            }

            // Round02 검증
            if (round02 == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage01_Round02_NormalBounty가 null입니다.");
                allRoundsValid = false;
            }
            else
            {
                if (round02.IntentProfile == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round02.IntentProfile이 null입니다.");
                    allRoundsValid = false;
                }
                if (round02.OpponentDiceLoadout == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round02.OpponentDiceLoadout이 null입니다.");
                    allRoundsValid = false;
                }
                if (round02.OpponentBaseRollsPerAttempt != 3)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Round02.OpponentBaseRollsPerAttempt = {round02.OpponentBaseRollsPerAttempt}, 기대값 3");
                    allRoundsValid = false;
                }
                if (round02.OpponentDevicePool == null || round02.OpponentDevicePool.Count == 0)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Round02.OpponentDevicePool이 null이거나 비어 있습니다.");
                    allRoundsValid = false;
                }
                else
                {
                    for (int i = 0; i < round02.OpponentDevicePool.Count; i++)
                    {
                        if (round02.OpponentDevicePool[i] == null)
                        {
                            Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Round02.OpponentDevicePool[{i}]가 null입니다.");
                            allRoundsValid = false;
                        }
                    }
                }
            }

            // Boss 검증
            if (boss == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage01_Boss_TheClerk가 null입니다.");
                allRoundsValid = false;
            }
            else
            {
                if (boss.IntentProfile == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Boss.IntentProfile이 null입니다.");
                    allRoundsValid = false;
                }
                if (boss.OpponentDiceLoadout == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Boss.OpponentDiceLoadout이 null입니다.");
                    allRoundsValid = false;
                }
                if (boss.OpponentBaseRollsPerAttempt != 4)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Boss.OpponentBaseRollsPerAttempt = {boss.OpponentBaseRollsPerAttempt}, 기대값 4");
                    allRoundsValid = false;
                }
                if (boss.OpponentDevicePool == null || boss.OpponentDevicePool.Count == 0)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Boss.OpponentDevicePool이 null이거나 비어 있습니다.");
                    allRoundsValid = false;
                }
                else
                {
                    for (int i = 0; i < boss.OpponentDevicePool.Count; i++)
                    {
                        if (boss.OpponentDevicePool[i] == null)
                        {
                            Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Boss.OpponentDevicePool[{i}]가 null입니다.");
                            allRoundsValid = false;
                        }
                    }
                }
            }

            if (allRoundsValid)
                Debug.Log("[Stage01StageRoundSOV44Generator] Round 검증 통과");

            // Stage 검증
            StageDefinitionSO stageDef = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StageDefinitionPath);
            bool stageValid = true;

            if (stageDef == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage_01_BrokenLedger가 null입니다.");
                stageValid = false;
            }
            else
            {
                if (stageDef.WorkshopRules == null)
                {
                    Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage_01_BrokenLedger.WorkshopRules가 null입니다.");
                    stageValid = false;
                }
                if (stageDef.RoundDefinitions == null || stageDef.RoundDefinitions.Length != 4)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Stage_01_BrokenLedger.RoundDefinitions count = {(stageDef.RoundDefinitions != null ? stageDef.RoundDefinitions.Length : 0)}, 기대값 4");
                    stageValid = false;
                }
            }

            if (stageValid)
                Debug.Log("[Stage01StageRoundSOV44Generator] Stage 검증 통과");

            // WorkshopRules 검증
            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);
            bool workshopValid = true;

            if (workshopRules == null)
            {
                Debug.LogError("[Stage01StageRoundSOV44Generator] 검증 실패: Stage01_WorkshopRules.asset이 존재하지 않습니다.");
                workshopValid = false;
            }
            else
            {
                ValidateWorkshopRulesSlotTypes(workshopRules, ref workshopValid);
            }

            if (workshopValid)
                Debug.Log("[Stage01StageRoundSOV44Generator] WorkshopRules 검증 통과");
        }

        /// <summary>WorkshopRules의 6개 ProductSlotRule AllowedProductTypes와 productPool을 검증한다.</summary>
        private static void ValidateWorkshopRulesSlotTypes(StageWorkshopRulesSO workshopRules, ref bool workshopValid)
        {
            System.Collections.Generic.IReadOnlyList<ShopProductSlotRule> slotRules = workshopRules.ProductSlotRules;

            if (slotRules == null || slotRules.Count != 6)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: ProductSlotRules count = {(slotRules != null ? slotRules.Count : 0)}, 기대값 6");
                workshopValid = false;
                return;
            }

            // 기대하는 슬롯별 AllowedProductTypes 첫 값
            ShopProductType[] expectedFirstTypes = new ShopProductType[]
            {
                ShopProductType.Device,          // Slot 0: Left Device
                ShopProductType.Device,          // Slot 1: Right Device
                ShopProductType.DiceSet,         // Slot 2: Left Dice Type
                ShopProductType.DiceSet,         // Slot 3: Right Dice Type
                ShopProductType.DiceFaceUpgrade, // Slot 4: Left Face Upgrade
                ShopProductType.DiceFaceUpgrade  // Slot 5: Right Face Upgrade
            };

            string[] expectedNames = new string[]
            {
                "Left Device",
                "Right Device",
                "Left Dice Type",
                "Right Dice Type",
                "Left Face Upgrade",
                "Right Face Upgrade"
            };

            bool allSlotsValid = true;

            for (int i = 0; i < 6; i++)
            {
                ShopProductSlotRule rule = slotRules[i];

                // displayName 검증
                if (rule.DisplayName != expectedNames[i])
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] DisplayName = '{rule.DisplayName}', 기대값 '{expectedNames[i]}'");
                    allSlotsValid = false;
                }

                // AllowedProductTypes 검증
                ShopProductType[] allowedTypes = rule.AllowedProductTypes;
                if (allowedTypes == null || allowedTypes.Length == 0)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] AllowedProductTypes가 null이거나 비어 있습니다.");
                    allSlotsValid = false;
                }
                else
                {
                    if (allowedTypes[0] != expectedFirstTypes[i])
                    {
                        Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] AllowedProductTypes[0] = {allowedTypes[0]}, 기대값 {expectedFirstTypes[i]}");
                        allSlotsValid = false;
                    }
                }

                // productPool 검증 (비어있어야 함)
                ShopProductDefinitionSO[] productPool = rule.ProductPool;
                if (productPool != null && productPool.Length > 0)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] productPool count = {productPool.Length}, 기대값 0");
                    allSlotsValid = false;
                }

                // minTierOverride / maxTierOverride 검증
                int expectedMinTier = (i % 2 == 0) ? 1 : 1; // 모든 slot minTier = 1
                int expectedMaxTier = (i % 2 == 0) ? 1 : 2; // 짝수=1, 홀수=2

                if (rule.MinTierOverride != expectedMinTier)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] MinTierOverride = {rule.MinTierOverride}, 기대값 {expectedMinTier}");
                    allSlotsValid = false;
                }
                if (rule.MaxTierOverride != expectedMaxTier)
                {
                    Debug.LogError($"[Stage01StageRoundSOV44Generator] 검증 실패: Slot[{i}] MaxTierOverride = {rule.MaxTierOverride}, 기대값 {expectedMaxTier}");
                    allSlotsValid = false;
                }
            }

            if (!allSlotsValid)
            {
                workshopValid = false;
            }
            else
            {
                Debug.Log("[Stage01StageRoundSOV44Generator] WorkshopRules 슬롯 타입 검증 통과");
            }
        }

        /// <summary>SerializedObject의 string 필드를 안전하게 설정한다.</summary>
        private static void SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing field: {fieldName} on {so.targetObject.name}");
                return;
            }

            prop.stringValue = value ?? string.Empty;
        }

        /// <summary>SerializedObject의 int 필드를 안전하게 설정한다.</summary>
        private static void SetInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing field: {fieldName} on {so.targetObject.name}");
                return;
            }

            prop.intValue = value;
        }

        /// <summary>SerializedObject의 bool 필드를 안전하게 설정한다.</summary>
        private static void SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing field: {fieldName} on {so.targetObject.name}");
                return;
            }

            prop.boolValue = value;
        }

        /// <summary>SerializedObject의 enum 필드를 실제 enum 정수값 기준으로 설정한다.</summary>
        private static void SetEnum(SerializedObject so, string fieldName, int enumValue)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing field: {fieldName} on {so.targetObject.name}");
                return;
            }

            prop.intValue = enumValue;
        }

        /// <summary>SerializedObject의 ObjectReference 필드를 안전하게 설정한다.</summary>
        private static void SetObjectReference(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)

            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing field: {fieldName} on {so.targetObject.name}");
                return;
            }

            prop.objectReferenceValue = value;
        }

        /// <summary>SerializedObject의 Object 배열 필드를 안전하게 설정한다.</summary>
        private static void SetObjectArray(
            SerializedObject so,
            string fieldName,
            SlotPairDeviceDefinitionSO[] values)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null || !prop.isArray)
            {
                Debug.LogError($"[Stage01StageRoundSOV44Generator] Missing array field: {fieldName} on {so.targetObject.name}");
                return;
            }

            SlotPairDeviceDefinitionSO[] safeValues = values ?? new SlotPairDeviceDefinitionSO[0];

            prop.ClearArray();
            prop.arraySize = safeValues.Length;

            for (int i = 0; i < safeValues.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = safeValues[i];
        }
    }
}
