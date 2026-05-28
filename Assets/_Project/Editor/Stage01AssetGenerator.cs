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

        // --- Create Round 1: Tutorial Normal ---
        var round1 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round1.name = "Stage01_Round01_TutorialNormal";
        SetRoundFields(round1,
            roundId: "stage01_tutorial_normal",
            displayName: "Tutorial Target",
            roundType: StageRoundType.Normal,
            tutorialForcedRound: true,
            initiallyAvailable: true,
            rewardParts: 10,
            rewardOvercharge: 0,
            rewardDescription: "First tutorial bounty reward.",
            opponentMaxHp: 40,
            enemyStrikeDamage: 2,
            playerMaxHp: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesDamagePenalty: false,
            nonAcesDamagePercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round1, $"{FolderPath}/Stage01_Round01_TutorialNormal.asset");

        // --- Create Round 2: Normal Bounty ---
        var round2 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round2.name = "Stage01_Round02_NormalBounty";
        SetRoundFields(round2,
            roundId: "stage01_normal_bounty",
            displayName: "Second Bounty",
            roundType: StageRoundType.Normal,
            tutorialForcedRound: false,
            initiallyAvailable: true,
            rewardParts: 20,
            rewardOvercharge: 0,
            rewardDescription: "Chain choice test reward.",
            opponentMaxHp: 70,
            enemyStrikeDamage: 3,
            playerMaxHp: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesDamagePenalty: false,
            nonAcesDamagePercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round2, $"{FolderPath}/Stage01_Round02_NormalBounty.asset");

        // --- Create Round 3: Boss ---
        var round3 = ScriptableObject.CreateInstance<StageRoundDefinitionSO>();
        round3.name = "Stage01_Boss_TheClerk";
        SetRoundFields(round3,
            roundId: "stage01_boss_clerk",
            displayName: "Boss - The Clerk",
            roundType: StageRoundType.Boss,
            tutorialForcedRound: false,
            initiallyAvailable: true,
            rewardParts: 50,
            rewardOvercharge: 0,
            rewardDescription: "Stage clear reward.",
            opponentMaxHp: 90,
            enemyStrikeDamage: 4,
            playerMaxHp: 100,
            diceCount: 5,
            maxAttempts: 3,
            roundRollPool: 8,
            maxUsesPerCastPerRound: 1,
            maxBrokenCastUsesPerRound: 3,
            brokenCastGrantsOvercharge: true,
            brokenCastOverchargeAmount: 1,
            brokenCastGrantsNextAttemptFreeReroll: true,
            brokenCastFreeRerollTokenAmount: 1,
            applyNonAcesDamagePenalty: true,
            nonAcesDamagePercent: 50,
            disableChance: false,
            disableBrokenCastReward: false);
        AssetDatabase.CreateAsset(round3, $"{FolderPath}/Stage01_Boss_TheClerk.asset");

        // --- Create Stage Definition ---
        var stage = ScriptableObject.CreateInstance<StageDefinitionSO>();
        stage.name = "Stage_01_BrokenLedger";
        SetStageFields(stage,
            stageNumber: 1,
            displayName: "Stage 1 - Broken Ledger",
            descentLabel: "Depth 01",
            stageDescription: "Tutorial bounty board test stage.",
            tutorialStage: true,
            shopEntryRequiresOverchargeAfterStageClear: false,
            keepChainAfterStageClear: true,
            rounds: new[] { round1, round2, round3 });
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
        StageRoundDefinitionSO[] rounds)
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

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRoundFields(StageRoundDefinitionSO round,
        string roundId,
        string displayName,
        StageRoundType roundType,
        bool tutorialForcedRound,
        bool initiallyAvailable,
        int rewardParts,
        int rewardOvercharge,
        string rewardDescription,
        int opponentMaxHp,
        int enemyStrikeDamage,
        int playerMaxHp,
        int diceCount,
        int maxAttempts,
        int roundRollPool,
        int maxUsesPerCastPerRound,
        int maxBrokenCastUsesPerRound,
        bool brokenCastGrantsOvercharge,
        int brokenCastOverchargeAmount,
        bool brokenCastGrantsNextAttemptFreeReroll,
        int brokenCastFreeRerollTokenAmount,
        bool applyNonAcesDamagePenalty,
        int nonAcesDamagePercent,
        bool disableChance,
        bool disableBrokenCastReward)
    {
        var so = new SerializedObject(round);
        so.FindProperty("roundId").stringValue = roundId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("roundType").enumValueIndex = (int)roundType;
        so.FindProperty("tutorialForcedRound").boolValue = tutorialForcedRound;
        so.FindProperty("initiallyAvailable").boolValue = initiallyAvailable;
        so.FindProperty("rewardParts").intValue = rewardParts;
        so.FindProperty("rewardOvercharge").intValue = rewardOvercharge;
        so.FindProperty("rewardDescription").stringValue = rewardDescription;
        so.FindProperty("opponentMaxHp").intValue = opponentMaxHp;
        so.FindProperty("enemyStrikeDamage").intValue = enemyStrikeDamage;
        so.FindProperty("playerMaxHp").intValue = playerMaxHp;
        so.FindProperty("diceCount").intValue = diceCount;
        so.FindProperty("maxAttempts").intValue = maxAttempts;
        so.FindProperty("roundRollPool").intValue = roundRollPool;
        so.FindProperty("maxUsesPerCastPerRound").intValue = maxUsesPerCastPerRound;
        so.FindProperty("maxBrokenCastUsesPerRound").intValue = maxBrokenCastUsesPerRound;
        so.FindProperty("brokenCastGrantsOvercharge").boolValue = brokenCastGrantsOvercharge;
        so.FindProperty("brokenCastOverchargeAmount").intValue = brokenCastOverchargeAmount;
        so.FindProperty("brokenCastGrantsNextAttemptFreeReroll").boolValue = brokenCastGrantsNextAttemptFreeReroll;
        so.FindProperty("brokenCastFreeRerollTokenAmount").intValue = brokenCastFreeRerollTokenAmount;
        so.FindProperty("applyNonAcesDamagePenalty").boolValue = applyNonAcesDamagePenalty;
        so.FindProperty("nonAcesDamagePercent").intValue = nonAcesDamagePercent;
        so.FindProperty("disableChance").boolValue = disableChance;
        so.FindProperty("disableBrokenCastReward").boolValue = disableBrokenCastReward;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
