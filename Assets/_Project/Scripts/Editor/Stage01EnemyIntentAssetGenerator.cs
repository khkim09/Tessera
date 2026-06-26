using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// Stage 1 (Broken Cast) 테스트용 EnemyIntentDefinitionSO 및 EnemyIntentProfileSO 에셋을 생성/업데이트하고,
    /// Stage 1 Round SO에 intentProfile을 자동 연결하는 Editor 유틸리티.
    /// Tools/Tessera/Generate Stage 1 Enemy Intent Assets 메뉴에서 실행한다.
    /// </summary>
    public static class Stage01EnemyIntentAssetGenerator
    {
        private const string IntentsFolderPath = "Assets/_Project/ScriptableObjects/Enemies/Intents";
        private const string ProfilesFolderPath = "Assets/_Project/ScriptableObjects/Enemies/IntentProfiles";
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        // ── Intent 에셋 경로 ──────────────────────────────────────────────
        private const string IntentPath_CleanOpeningStrike = IntentsFolderPath + "/Intent_CleanOpeningStrike.asset";
        private const string IntentPath_OpeningStrike = IntentsFolderPath + "/Intent_OpeningStrike.asset";
        private const string IntentPath_OpeningWindow = IntentsFolderPath + "/Intent_OpeningWindow.asset";
        private const string IntentPath_Execution = IntentsFolderPath + "/Intent_Execution.asset";

        // ── Profile 에셋 경로 ─────────────────────────────────────────────
        private const string ProfilePath_TutorialCleanStrike = ProfilesFolderPath + "/Profile_Tutorial_CleanStrike.asset";
        private const string ProfilePath_Stage01PlayerOpening = ProfilesFolderPath + "/Profile_Stage01_PlayerOpening.asset";
        private const string ProfilePath_Stage01Aggression = ProfilesFolderPath + "/Profile_Stage01_Aggression.asset";
        private const string ProfilePath_Stage01Boss = ProfilesFolderPath + "/Profile_Stage01_Boss.asset";

        // ── Stage Round 에셋 경로 ─────────────────────────────────────────
        private const string RoundPath_TutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";
        private const string RoundPath_Round01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";
        private const string RoundPath_Round02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";
        private const string RoundPath_Boss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Tools/Tessera/Generate Stage 1 Enemy Intent Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 Enemy Intent Assets")]
        private static void Generate()
        {
            InvokeGenerate();
        }

        /// <summary>Stage01AssetGenerator에서 호출하는 public 진입점이다.</summary>
        public static void InvokeGenerate()
        {
            EnsureFolderExists(IntentsFolderPath, "Assets/_Project/ScriptableObjects/Enemies", "Intents");
            EnsureFolderExists(ProfilesFolderPath, "Assets/_Project/ScriptableObjects/Enemies", "IntentProfiles");

            // 1. EnemyIntentDefinitionSO 에셋 4개 생성/업데이트
            EnemyIntentDefinitionSO intentCleanOpeningStrike = CreateOrUpdateIntent(
                IntentPath_CleanOpeningStrike,
                "intent_clean_opening_strike",
                "Clean Opening Strike",
                "Opponent rolls first without Device effects.",
                "Opponent acts first, but does not use Devices.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: false,
                castSelectionPolicy: OpponentCastSelectionPolicy.TargetBandFirst,
                opponentRollCount: 2,
                targetPowerToStop: 25,
                targetImpactToStop: 5,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            EnemyIntentDefinitionSO intentOpeningStrike = CreateOrUpdateIntent(
                IntentPath_OpeningStrike,
                "intent_opening_strike",
                "Opening Strike",
                "Opponent rolls first and sets the CastPower target.",
                "Opponent acts first. Beat its Power or use Broken Cast.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.UtilityBest,
                opponentRollCount: 3,
                targetPowerToStop: 45,
                targetImpactToStop: 7,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            EnemyIntentDefinitionSO intentOpeningWindow = CreateOrUpdateIntent(
                IntentPath_OpeningWindow,
                "intent_opening_window",
                "Opening Window",
                "You act first before the opponent responds.",
                "You act first. Set Power before the opponent rolls.",
                EnemyIntentCategoryType.Tactics,
                InitiativeOwnerType.Player,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.UtilityBest,
                opponentRollCount: 3,
                targetPowerToStop: 40,
                targetImpactToStop: 6,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            EnemyIntentDefinitionSO intentExecution = CreateOrUpdateIntent(
                IntentPath_Execution,
                "intent_execution",
                "Execution",
                "Opponent executes a heavy strike with high Power.",
                "Opponent executes a devastating strike.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.UtilityBest,
                opponentRollCount: 3,
                targetPowerToStop: 55,
                targetImpactToStop: 9,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.GreedyBestDamage);

            AssetDatabase.SaveAssets();

            // 2. EnemyIntentProfileSO 에셋 4개 생성/업데이트
            EnemyIntentProfileSO profileTutorialCleanStrike = CreateOrUpdateProfile(
                ProfilePath_TutorialCleanStrike,
                "Profile_Tutorial_CleanStrike",
                intentCleanOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentCleanOpeningStrike });

            EnemyIntentProfileSO profileStage01PlayerOpening = CreateOrUpdateProfile(
                ProfilePath_Stage01PlayerOpening,
                "Profile_Stage01_PlayerOpening",
                intentOpeningWindow,
                new EnemyIntentDefinitionSO[] { intentOpeningWindow, intentOpeningStrike });

            EnemyIntentProfileSO profileStage01Aggression = CreateOrUpdateProfile(
                ProfilePath_Stage01Aggression,
                "Profile_Stage01_Aggression",
                intentOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentOpeningStrike, intentOpeningWindow });

            EnemyIntentProfileSO profileStage01Boss = CreateOrUpdateProfile(
                ProfilePath_Stage01Boss,
                "Profile_Stage01_Boss",
                intentOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentOpeningStrike, intentExecution, intentOpeningWindow });

            AssetDatabase.SaveAssets();

            // 3. Stage Round SO에 intentProfile 연결 (direct openingIntent / intentDeck은 fallback으로 유지)
            ConnectStageRound_TutorialTarget(profileTutorialCleanStrike);
            ConnectStageRound_Round01(profileStage01PlayerOpening);
            ConnectStageRound_Round02(profileStage01Aggression);
            ConnectStageRound_Boss(profileStage01Boss);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Generation complete.\n" +
                      $"  Intents: {IntentPath_CleanOpeningStrike}, {IntentPath_OpeningStrike}, {IntentPath_OpeningWindow}, {IntentPath_Execution}\n" +
                      $"  Profiles: {ProfilePath_TutorialCleanStrike}, {ProfilePath_Stage01PlayerOpening}, {ProfilePath_Stage01Aggression}, {ProfilePath_Stage01Boss}\n" +
                      $"  Rounds: {RoundPath_TutorialTarget}, {RoundPath_Round01}, {RoundPath_Round02}, {RoundPath_Boss}");
        }

        // ── 폴더 생성 ─────────────────────────────────────────────────────

        /// <summary>지정 경로에 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
                Debug.Log($"[Stage01EnemyIntentAssetGenerator] Created folder: {fullPath}");
            }
        }

        // ── Intent 생성/업데이트 ──────────────────────────────────────────

        /// <summary>
        /// 지정 경로에 EnemyIntentDefinitionSO 에셋이 없으면 새로 생성하고,
        /// 이미 존재하면 기존 에셋을 로드하여 필드 값을 업데이트한다.
        /// </summary>
        private static EnemyIntentDefinitionSO CreateOrUpdateIntent(
            string assetPath,
            string intentId,
            string displayName,
            string shortDescription,
            string bountyCardDescription,
            EnemyIntentCategoryType categoryType,
            InitiativeOwnerType initiativeOwner,
            bool useOpponentDevices,
            OpponentCastSelectionPolicy castSelectionPolicy,
            int opponentRollCount,
            int targetPowerToStop,
            int targetImpactToStop,
            bool stopIfBeatsPlayerPower,
            OpponentRollStrategyType rollStrategy)
        {
            EnemyIntentDefinitionSO asset = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyIntentDefinitionSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"[Stage01EnemyIntentAssetGenerator] Created new intent asset: {assetPath}");
            }
            else
            {
                Debug.Log($"[Stage01EnemyIntentAssetGenerator] Updating existing intent asset: {assetPath}");
            }

            SerializedObject so = new SerializedObject(asset);

            SetSerializedFieldString(so, "intentId", intentId);
            SetSerializedFieldString(so, "displayName", displayName);
            SetSerializedFieldString(so, "shortDescription", shortDescription);
            SetSerializedFieldString(so, "bountyCardDescription", bountyCardDescription);
            SetSerializedFieldEnum(so, "categoryType", (int)categoryType);
            SetSerializedFieldEnum(so, "initiativeOwner", (int)initiativeOwner);
            SetSerializedFieldBool(so, "useOpponentDevices", useOpponentDevices);
            SetSerializedFieldEnum(so, "castSelectionPolicy", (int)castSelectionPolicy);
            SetSerializedFieldInt(so, "opponentRollCount", opponentRollCount);
            SetSerializedFieldInt(so, "targetPowerToStop", targetPowerToStop);
            SetSerializedFieldInt(so, "targetImpactToStop", targetImpactToStop);
            SetSerializedFieldBool(so, "stopIfBeatsPlayerPower", stopIfBeatsPlayerPower);
            SetSerializedFieldEnum(so, "rollStrategy", (int)rollStrategy);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        // ── Profile 생성/업데이트 ─────────────────────────────────────────

        /// <summary>
        /// 지정 경로에 EnemyIntentProfileSO 에셋이 없으면 새로 생성하고,
        /// 이미 존재하면 기존 에셋을 로드하여 필드 값을 업데이트한다.
        /// </summary>
        private static EnemyIntentProfileSO CreateOrUpdateProfile(
            string assetPath,
            string profileName,
            EnemyIntentDefinitionSO openingIntent,
            EnemyIntentDefinitionSO[] intentPool)
        {
            EnemyIntentProfileSO asset = AssetDatabase.LoadAssetAtPath<EnemyIntentProfileSO>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyIntentProfileSO>();
                asset.name = profileName;
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"[Stage01EnemyIntentAssetGenerator] Created new profile asset: {assetPath}");
            }
            else
            {
                Debug.Log($"[Stage01EnemyIntentAssetGenerator] Updating existing profile asset: {assetPath}");
            }

            SerializedObject so = new SerializedObject(asset);
            SetSerializedFieldObject(so, "openingIntent", openingIntent);

            SerializedProperty poolProp = so.FindProperty("intentPool");
            if (poolProp != null && poolProp.isArray)
            {
                poolProp.ClearArray();
                poolProp.arraySize = intentPool.Length;
                for (int i = 0; i < intentPool.Length; i++)
                {
                    poolProp.GetArrayElementAtIndex(i).objectReferenceValue = intentPool[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        // ── Stage Round 연결 ──────────────────────────────────────────────

        /// <summary>Stage01_TutorialTarget에 intentProfile을 연결한다.</summary>
        private static void ConnectStageRound_TutorialTarget(EnemyIntentProfileSO profile)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_TutorialTarget, "Stage01_TutorialTarget");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            SetSerializedFieldObject(so, "intentProfile", profile);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected profile to {RoundPath_TutorialTarget}");
        }

        /// <summary>Stage01_Round01_TutorialNormal에 intentProfile을 연결한다.</summary>
        private static void ConnectStageRound_Round01(EnemyIntentProfileSO profile)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round01, "Stage01_Round01_TutorialNormal");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            SetSerializedFieldObject(so, "intentProfile", profile);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected profile to {RoundPath_Round01}");
        }

        /// <summary>Stage01_Round02_NormalBounty에 intentProfile을 연결한다.</summary>
        private static void ConnectStageRound_Round02(EnemyIntentProfileSO profile)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round02, "Stage01_Round02_NormalBounty");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            SetSerializedFieldObject(so, "intentProfile", profile);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected profile to {RoundPath_Round02}");
        }

        /// <summary>Stage01_Boss_TheClerk에 intentProfile을 연결한다.</summary>
        private static void ConnectStageRound_Boss(EnemyIntentProfileSO profile)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Boss, "Stage01_Boss_TheClerk");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            SetSerializedFieldObject(so, "intentProfile", profile);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected profile to {RoundPath_Boss}");
        }

        // ── Helper ────────────────────────────────────────────────────────

        /// <summary>지정 경로에서 StageRoundDefinitionSO를 로드한다. 없으면 경고를 출력하고 null을 반환한다.</summary>
        private static StageRoundDefinitionSO FindStageRound(string assetPath, string assetName)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(assetPath);

            if (asset == null)
            {
                Debug.LogWarning($"[Stage01EnemyIntentAssetGenerator] Could not find StageRoundDefinitionSO at path: {assetPath} (asset name: {assetName})");
                return null;
            }

            return asset;
        }

        /// <summary>SerializedObject에서 string 필드를 안전하게 설정한다.</summary>
        private static void SetSerializedFieldString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }
            prop.stringValue = value;
        }

        /// <summary>SerializedObject에서 bool 필드를 안전하게 설정한다.</summary>
        private static void SetSerializedFieldBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }
            prop.boolValue = value;
        }

        /// <summary>SerializedObject에서 int 필드를 안전하게 설정한다.</summary>
        private static void SetSerializedFieldInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }

            prop.intValue = value;
        }

        /// <summary>SerializedObject에서 enum 필드를 안전하게 설정한다.</summary>
        private static void SetSerializedFieldEnum(SerializedObject so, string fieldName, int enumValueIndex)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }
            prop.enumValueIndex = enumValueIndex;
        }

        /// <summary>SerializedObject에서 ObjectReference 필드를 안전하게 설정한다.</summary>
        private static void SetSerializedFieldObject(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
