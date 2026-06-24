using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

public static class Stage01AssetGenerator
{
    private const string FolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

    [MenuItem("Tools/Generate Stage 01 Assets")]
    public static void Generate()
    {
        // Ensure folder exists
        System.IO.Directory.CreateDirectory(FolderPath);

        // 기존 에셋 제거 (있으면 덮어쓰기 위해)
        string[] existingPaths = new string[]
        {
            $"{FolderPath}/Stage01_Round01_TutorialNormal.asset",
            $"{FolderPath}/Stage01_Round02_NormalBounty.asset",
            $"{FolderPath}/Stage01_Boss_TheClerk.asset",
            $"{FolderPath}/Stage01_WorkshopRules.asset",
            $"{FolderPath}/Stage_01_BrokenLedger.asset",
        };

        foreach (string path in existingPaths)
        {
            string assetPath = path.Replace("\\", "/");
            string existing = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(existing))
                AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- Create Round 1: Tutorial Normal ---
        StageRoundDefinitionSO round1 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round1.name = "Stage01_Round01_TutorialNormal";
        SetRoundFields(round1,
            roundId: "stage01_tutorial_normal",
            displayName: "Tutorial Target",
            roundType: StageRoundType.Normal,
            tutorialForcedRound: true,
            initiallyAvailable: true,
            rewardMoney: 10,
            rewardOvercharge: 0,
            rewardDescription: "First tutorial bounty reward.",
            opponentMaxHP: 40,
            enemyStrikeDamage: 2,
            playerMaxHP: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            impactCap: 8,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesCastPowerPenalty: false,
            nonAcesCastPowerPercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round1, $"{FolderPath}/Stage01_Round01_TutorialNormal.asset");

        // --- Create Round 2: Normal Bounty ---
        StageRoundDefinitionSO round2 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round2.name = "Stage01_Round02_NormalBounty";
        SetRoundFields(round2,
            roundId: "stage01_normal_bounty",
            displayName: "Second Bounty",
            roundType: StageRoundType.Normal,
            tutorialForcedRound: false,
            initiallyAvailable: true,
            rewardMoney: 20,
            rewardOvercharge: 0,
            rewardDescription: "Chain choice test reward.",
            opponentMaxHP: 70,
            enemyStrikeDamage: 3,
            playerMaxHP: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            impactCap: 9,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesCastPowerPenalty: false,
            nonAcesCastPowerPercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round2, $"{FolderPath}/Stage01_Round02_NormalBounty.asset");

        // --- Create Round 3: Boss ---
        StageRoundDefinitionSO round3 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round3.name = "Stage01_Boss_TheClerk";
        SetRoundFields(round3,
            roundId: "stage01_boss_clerk",
            displayName: "Boss - The Clerk",
            roundType: StageRoundType.Boss,
            tutorialForcedRound: false,
            initiallyAvailable: true,
            rewardMoney: 50,
            rewardOvercharge: 0,
            rewardDescription: "Stage clear reward.",
            opponentMaxHP: 90,
            enemyStrikeDamage: 4,
            playerMaxHP: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            impactCap: 11,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesCastPowerPenalty: true,
            nonAcesCastPowerPercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round3, $"{FolderPath}/Stage01_Boss_TheClerk.asset");

        // --- Create Workshop Rules for Stage 01 ---
        StageWorkshopRulesSO workshopRules = ScriptableObject.CreateInstance<StageWorkshopRulesSO>();
        workshopRules.name = "Stage01_WorkshopRules";
        SerializedObject workshopRulesSo = new SerializedObject(workshopRules);
        workshopRulesSo.FindProperty("baseWorkshopTier").intValue = 1;
        workshopRulesSo.FindProperty("minShopProductTier").intValue = 1;
        workshopRulesSo.FindProperty("maxShopProductTier").intValue = 2;
        workshopRulesSo.FindProperty("tierUpgradeOverchargeCost").intValue = 1;
        workshopRulesSo.FindProperty("maxTierUpgradePerVisit").intValue = 1;
        workshopRulesSo.FindProperty("tierIncreasePerUpgrade").intValue = 1;
        workshopRulesSo.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(workshopRules, $"{FolderPath}/Stage01_WorkshopRules.asset");

        // 중간 저장: CreateAsset으로 만든 에셋들을 디스크에 기록하여 참조 가능하게 함
        AssetDatabase.SaveAssets();

        // --- Create Stage Definition (다른 CreateAsset 에셋을 참조하므로 중간 저장 후 생성) ---
        StageDefinitionSO stage = ScriptableObject.CreateInstance<StageDefinitionSO>();
        stage.name = "Stage_01_BrokenLedger";
        SetStageFields(stage,
            stageNumber: 1,
            displayName: "Stage 1 - Broken Ledger",
            descentLabel: "Depth 01",
            stageDescription: "Tutorial bounty board test stage.",
            tutorialStage: true,
            shopEntryRequiresOverchargeAfterStageClear: false,
            keepChainAfterStageClear: true,
            rounds: new StageRoundDefinitionSO[] { round1, round2, round3 },
            workshopRules: workshopRules);
        AssetDatabase.CreateAsset(stage, $"{FolderPath}/Stage_01_BrokenLedger.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Stage 01 assets generated successfully!");
    }

    private static void SetStageFields(StageDefinitionSO stage,
        int stageNumber,
        string displayName,
        string descentLabel,
        string stageDescription,
        bool tutorialStage,
        bool shopEntryRequiresOverchargeAfterStageClear,
        bool keepChainAfterStageClear,
        StageRoundDefinitionSO[] rounds,
        StageWorkshopRulesSO workshopRules = null)
    {
        var so = new SerializedObject(stage);
        so.FindProperty("stageNumber").intValue = stageNumber;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("descentLabel").stringValue = descentLabel;
        so.FindProperty("stageDescription").stringValue = stageDescription;
        so.FindProperty("tutorialStage").boolValue = tutorialStage;
        so.FindProperty("shopEntryRequiresOverchargeAfterStageClear").boolValue = shopEntryRequiresOverchargeAfterStageClear;
        so.FindProperty("keepChainAfterStageClear").boolValue = keepChainAfterStageClear;

        var roundsProp = so.FindProperty("roundDefinitions");
        roundsProp.ClearArray();
        roundsProp.arraySize = rounds.Length;
        for (int i = 0; i < rounds.Length; i++)
        {
            roundsProp.GetArrayElementAtIndex(i).objectReferenceValue = rounds[i];
        }

        so.FindProperty("workshopRules").objectReferenceValue = workshopRules;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRoundFields(StageRoundDefinitionSO round,
        string roundId,
        string displayName,
        StageRoundType roundType,
        bool tutorialForcedRound,
        bool initiallyAvailable,
        int rewardMoney,
        int rewardOvercharge,
        string rewardDescription,
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
        bool disableBrokenCastReward)
    {
        var so = new SerializedObject(round);
        so.FindProperty("roundId").stringValue = roundId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("roundType").enumValueIndex = (int)roundType;
        so.FindProperty("tutorialForcedRound").boolValue = tutorialForcedRound;
        so.FindProperty("initiallyAvailable").boolValue = initiallyAvailable;
        so.FindProperty("baseRewardMoney").intValue = rewardMoney;
        so.FindProperty("rewardOvercharge").intValue = rewardOvercharge;
        so.FindProperty("rewardDescription").stringValue = rewardDescription;
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
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
