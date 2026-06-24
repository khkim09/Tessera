using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// Stage 1 (Broken Cast) 테스트용 EnemyIntentDefinitionSO 에셋을 생성/업데이트하고,
    /// Stage 1 Round SO에 openingIntent / intentDeck을 자동 연결하는 Editor 유틸리티.
    /// Tools/Tessera/Generate Stage 1 Enemy Intent Assets 메뉴에서 실행한다.
    /// </summary>
    public static class Stage01EnemyIntentAssetGenerator
    {
        private const string IntentsFolderPath = "Assets/_Project/ScriptableObjects/Enemies/Intents";
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        // ── Intent 에셋 경로 ──────────────────────────────────────────────
        private const string IntentPath_OpponentOpeningStrike = IntentsFolderPath + "/Intent_OpponentOpeningStrike.asset";
        private const string IntentPath_PlayerOpeningWindow = IntentsFolderPath + "/Intent_PlayerOpeningWindow.asset";
        private const string IntentPath_OpponentNoDeviceStrike = IntentsFolderPath + "/Intent_OpponentNoDeviceStrike.asset";

        // ── Stage Round 에셋 경로 ─────────────────────────────────────────
        private const string RoundPath_TutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";
        private const string RoundPath_Round01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";
        private const string RoundPath_Round02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";
        private const string RoundPath_Boss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Tools/Tessera/Generate Stage 1 Enemy Intent Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 Enemy Intent Assets")]
        private static void Generate()
        {
            EnsureFolderExists(IntentsFolderPath, "Assets/_Project/ScriptableObjects/Enemies", "Intents");

            // 1. EnemyIntentDefinitionSO 에셋 3개 생성/업데이트
            EnemyIntentDefinitionSO intentOpponentOpeningStrike = CreateOrUpdateIntent(
                IntentPath_OpponentOpeningStrike,
                "intent_opponent_opening_strike",
                "Opening Strike",
                "Opponent rolls first and sets the CastPower target.",
                "Opponent acts first. Beat its Power or use Broken Cast.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: true,
                chooseBestAvailableCast: true,
                opponentRollCount: 3,
                targetImpactToStop: 7,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            EnemyIntentDefinitionSO intentPlayerOpeningWindow = CreateOrUpdateIntent(
                IntentPath_PlayerOpeningWindow,
                "intent_player_opening_window",
                "Opening Window",
                "You act first before the opponent responds.",
                "You act first. Set Power before the opponent rolls.",
                EnemyIntentCategoryType.Tactics,
                InitiativeOwnerType.Player,
                useOpponentDevices: true,
                chooseBestAvailableCast: true,
                opponentRollCount: 3,
                targetImpactToStop: 6,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            EnemyIntentDefinitionSO intentOpponentNoDeviceStrike = CreateOrUpdateIntent(
                IntentPath_OpponentNoDeviceStrike,
                "intent_opponent_no_device_strike",
                "Clean Opening Strike",
                "Opponent rolls first without Device effects.",
                "Opponent acts first, but does not use Devices.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: false,
                chooseBestAvailableCast: true,
                opponentRollCount: 2,
                targetImpactToStop: 5,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            AssetDatabase.SaveAssets();

            // 2. Stage Round SO에 openingIntent / intentDeck 연결
            ConnectStageRound_TutorialTarget(intentOpponentNoDeviceStrike);
            ConnectStageRound_Round01(intentPlayerOpeningWindow, intentOpponentOpeningStrike);
            ConnectStageRound_Round02(intentOpponentOpeningStrike, intentPlayerOpeningWindow);
            ConnectStageRound_Boss(intentOpponentOpeningStrike);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Generation complete.\n" +
                    $"  Intents: {IntentPath_OpponentOpeningStrike}, {IntentPath_PlayerOpeningWindow}, {IntentPath_OpponentNoDeviceStrike}\n" +
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
            bool chooseBestAvailableCast,
            int opponentRollCount,
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
            SetSerializedFieldEnum(so, "castSelectionPolicy", chooseBestAvailableCast
                ? (int)OpponentCastSelectionPolicy.UtilityBest
                : (int)OpponentCastSelectionPolicy.DebugFirstValid);
            SetSerializedFieldInt(so, "opponentRollCount", opponentRollCount);
            SetSerializedFieldInt(so, "targetImpactToStop", targetImpactToStop);
            SetSerializedFieldBool(so, "stopIfBeatsPlayerPower", stopIfBeatsPlayerPower);
            SetSerializedFieldEnum(so, "rollStrategy", (int)rollStrategy);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        // ── Stage Round 연결 ──────────────────────────────────────────────

        /// <summary>Stage01_TutorialTarget에 openingIntent / intentDeck을 연결한다.</summary>
        private static void ConnectStageRound_TutorialTarget(EnemyIntentDefinitionSO intentNoDeviceStrike)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_TutorialTarget, "Stage01_TutorialTarget");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);

            SetSerializedFieldObject(so, "openingIntent", intentNoDeviceStrike);
            SetIntentDeck(so, "intentDeck", new EnemyIntentDefinitionSO[]
            {
                intentNoDeviceStrike
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected intents to {RoundPath_TutorialTarget}");
        }

        /// <summary>Stage01_Round01_TutorialNormal에 openingIntent / intentDeck을 연결한다.</summary>
        private static void ConnectStageRound_Round01(
            EnemyIntentDefinitionSO intentPlayerWindow,
            EnemyIntentDefinitionSO intentOpponentStrike)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round01, "Stage01_Round01_TutorialNormal");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);

            SetSerializedFieldObject(so, "openingIntent", intentPlayerWindow);
            SetIntentDeck(so, "intentDeck", new EnemyIntentDefinitionSO[]
            {
                intentPlayerWindow,
                intentOpponentStrike
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected intents to {RoundPath_Round01}");
        }

        /// <summary>Stage01_Round02_NormalBounty에 openingIntent / intentDeck을 연결한다.</summary>
        private static void ConnectStageRound_Round02(
            EnemyIntentDefinitionSO intentOpponentStrike,
            EnemyIntentDefinitionSO intentPlayerWindow)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round02, "Stage01_Round02_NormalBounty");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);

            SetSerializedFieldObject(so, "openingIntent", intentOpponentStrike);
            SetIntentDeck(so, "intentDeck", new EnemyIntentDefinitionSO[]
            {
                intentOpponentStrike,
                intentPlayerWindow
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected intents to {RoundPath_Round02}");
        }

        /// <summary>Stage01_Boss_TheClerk에 openingIntent / intentDeck을 연결한다.</summary>
        private static void ConnectStageRound_Boss(EnemyIntentDefinitionSO intentOpponentStrike)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Boss, "Stage01_Boss_TheClerk");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);

            SetSerializedFieldObject(so, "openingIntent", intentOpponentStrike);
            SetIntentDeck(so, "intentDeck", new EnemyIntentDefinitionSO[]
            {
                intentOpponentStrike
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyIntentAssetGenerator] Connected intents to {RoundPath_Boss}");
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

        /// <summary>SerializedObject에서 intentDeck 배열을 안전하게 설정한다.</summary>
        private static void SetIntentDeck(SerializedObject so, string fieldName, EnemyIntentDefinitionSO[] deck)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is null. Check field name.");
                return;
            }

            if (!prop.isArray)
            {
                Debug.LogError($"[Stage01EnemyIntentAssetGenerator] SerializedProperty '{fieldName}' is not an array.");
                return;
            }

            prop.ClearArray();
            prop.arraySize = deck.Length;

            for (int i = 0; i < deck.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = deck[i];
            }
        }
    }
}
