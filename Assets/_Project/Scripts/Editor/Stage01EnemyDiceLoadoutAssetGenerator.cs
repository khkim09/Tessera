using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// Stage 1 (Broken Cast) 테스트용 EnemyDiceLoadoutDefinitionSO 에셋을 생성/업데이트하고,
    /// Stage 1 Round SO에 opponentDiceLoadout을 자동 연결하는 Editor 유틸리티.
    /// Tools/Tessera/Generate Stage 1 Enemy Dice Loadout Assets 메뉴에서 실행한다.
    /// </summary>
    public static class Stage01EnemyDiceLoadoutAssetGenerator
    {
        private const string DiceLoadoutsFolderPath = "Assets/_Project/ScriptableObjects/Enemies/DiceLoadouts";
        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";

        // ── Dice Loadout 에셋 경로 ─────────────────────────────────────────
        private const string LoadoutPath_StandardD6 = DiceLoadoutsFolderPath + "/Loadout_StandardD6.asset";
        private const string LoadoutPath_HighDamage = DiceLoadoutsFolderPath + "/Loadout_HighDamage.asset";
        private const string LoadoutPath_Balanced = DiceLoadoutsFolderPath + "/Loadout_Balanced.asset";
        private const string LoadoutPath_BossRigged = DiceLoadoutsFolderPath + "/Loadout_BossRigged.asset";

        // ── Stage Round 에셋 경로 ─────────────────────────────────────────
        private const string RoundPath_TutorialTarget = StagesFolderPath + "/Stage01_TutorialTarget.asset";
        private const string RoundPath_Round01 = StagesFolderPath + "/Stage01_Round01_TutorialNormal.asset";
        private const string RoundPath_Round02 = StagesFolderPath + "/Stage01_Round02_NormalBounty.asset";
        private const string RoundPath_Boss = StagesFolderPath + "/Stage01_Boss_TheClerk.asset";

        /// <summary>Tools/Tessera/Generate Stage 1 Enemy Dice Loadout Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 Enemy Dice Loadout Assets")]
        private static void Generate()
        {
            EnsureFolderExists(DiceLoadoutsFolderPath, "Assets/_Project/ScriptableObjects/Enemies", "DiceLoadouts");

            // 1. EnemyDiceLoadoutDefinitionSO 에셋 4개 생성/업데이트
            EnemyDiceLoadoutDefinitionSO loadoutStandardD6 = CreateOrUpdateLoadout(
                LoadoutPath_StandardD6,
                "loadout_standard_d6",
                "Standard D6",
                new int[][]
                {
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 }
                });

            EnemyDiceLoadoutDefinitionSO loadoutHighDamage = CreateOrUpdateLoadout(
                LoadoutPath_HighDamage,
                "loadout_high_damage",
                "High Damage",
                new int[][]
                {
                    new int[] { 6, 6, 6, 5, 5, 4 },
                    new int[] { 6, 6, 6, 5, 5, 4 },
                    new int[] { 6, 6, 6, 5, 5, 4 },
                    new int[] { 6, 6, 6, 5, 5, 4 },
                    new int[] { 6, 6, 6, 5, 5, 4 }
                });

            EnemyDiceLoadoutDefinitionSO loadoutBalanced = CreateOrUpdateLoadout(
                LoadoutPath_Balanced,
                "loadout_balanced",
                "Balanced",
                new int[][]
                {
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 2, 3, 4, 5, 6, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 2, 3, 4, 5, 6, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 }
                });

            EnemyDiceLoadoutDefinitionSO loadoutBossRigged = CreateOrUpdateLoadout(
                LoadoutPath_BossRigged,
                "loadout_boss_rigged",
                "Boss Rigged",
                new int[][]
                {
                    new int[] { 6, 6, 6, 6, 6, 6 },
                    new int[] { 6, 6, 6, 6, 6, 6 },
                    new int[] { 6, 6, 6, 6, 6, 6 },
                    new int[] { 6, 6, 6, 6, 6, 6 },
                    new int[] { 6, 6, 6, 6, 6, 6 }
                });

            AssetDatabase.SaveAssets();

            // 2. Stage Round SO에 opponentDiceLoadout 연결
            ConnectStageRound_TutorialTarget(loadoutStandardD6);
            ConnectStageRound_Round01(loadoutBalanced);
            ConnectStageRound_Round02(loadoutHighDamage);
            ConnectStageRound_Boss(loadoutBossRigged);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Generation complete.\n" +
                      $"  Loadouts: {LoadoutPath_StandardD6}, {LoadoutPath_HighDamage}, {LoadoutPath_Balanced}, {LoadoutPath_BossRigged}\n" +
                      $"  Rounds: {RoundPath_TutorialTarget}, {RoundPath_Round01}, {RoundPath_Round02}, {RoundPath_Boss}");
        }

        // ── 폴더 생성 ─────────────────────────────────────────────────────

        /// <summary>지정 경로에 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists(string fullPath, string parentPath, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
                Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Created folder: {fullPath}");
            }
        }

        // ── Loadout 생성/업데이트 ──────────────────────────────────────────

        /// <summary>
        /// 지정 경로에 EnemyDiceLoadoutDefinitionSO 에셋이 없으면 새로 생성하고,
        /// 이미 존재하면 기존 에셋을 로드하여 필드 값을 업데이트한다.
        /// </summary>
        private static EnemyDiceLoadoutDefinitionSO CreateOrUpdateLoadout(
            string assetPath,
            string loadoutId,
            string displayName,
            int[][] diceFaceSets)
        {
            EnemyDiceLoadoutDefinitionSO asset = AssetDatabase.LoadAssetAtPath<EnemyDiceLoadoutDefinitionSO>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyDiceLoadoutDefinitionSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Created new loadout asset: {assetPath}");
            }
            else
            {
                Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Updating existing loadout asset: {assetPath}");
            }

            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("loadoutId").stringValue = loadoutId;
            so.FindProperty("displayName").stringValue = displayName;

            // diceDefinitions 배열 설정
            SerializedProperty diceDefsProp = so.FindProperty("diceDefinitions");
            if (diceDefsProp != null && diceDefsProp.isArray)
            {
                diceDefsProp.ClearArray();
                diceDefsProp.arraySize = diceFaceSets.Length;

                for (int i = 0; i < diceFaceSets.Length; i++)
                {
                    SerializedProperty elementProp = diceDefsProp.GetArrayElementAtIndex(i);
                    int[] faceValues = diceFaceSets[i];

                    // numberFaces 배열 설정
                    SerializedProperty numberFacesProp = elementProp.FindPropertyRelative("numberFaces");
                    if (numberFacesProp != null && numberFacesProp.isArray)
                    {
                        numberFacesProp.ClearArray();
                        numberFacesProp.arraySize = faceValues.Length;

                        for (int j = 0; j < faceValues.Length; j++)
                        {
                            numberFacesProp.GetArrayElementAtIndex(j).intValue = Mathf.Clamp(faceValues[j], 1, 6);
                        }
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        // ── Stage Round 연결 ──────────────────────────────────────────────

        /// <summary>Stage01_TutorialTarget에 opponentDiceLoadout을 연결한다.</summary>
        private static void ConnectStageRound_TutorialTarget(EnemyDiceLoadoutDefinitionSO loadout)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_TutorialTarget, "Stage01_TutorialTarget");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("opponentDiceLoadout").objectReferenceValue = loadout;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Connected loadout to {RoundPath_TutorialTarget}");
        }

        /// <summary>Stage01_Round01_TutorialNormal에 opponentDiceLoadout을 연결한다.</summary>
        private static void ConnectStageRound_Round01(EnemyDiceLoadoutDefinitionSO loadout)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round01, "Stage01_Round01_TutorialNormal");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("opponentDiceLoadout").objectReferenceValue = loadout;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Connected loadout to {RoundPath_Round01}");
        }

        /// <summary>Stage01_Round02_NormalBounty에 opponentDiceLoadout을 연결한다.</summary>
        private static void ConnectStageRound_Round02(EnemyDiceLoadoutDefinitionSO loadout)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Round02, "Stage01_Round02_NormalBounty");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("opponentDiceLoadout").objectReferenceValue = loadout;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Connected loadout to {RoundPath_Round02}");
        }

        /// <summary>Stage01_Boss_TheClerk에 opponentDiceLoadout을 연결한다.</summary>
        private static void ConnectStageRound_Boss(EnemyDiceLoadoutDefinitionSO loadout)
        {
            StageRoundDefinitionSO asset = FindStageRound(RoundPath_Boss, "Stage01_Boss_TheClerk");
            if (asset == null)
                return;

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("opponentDiceLoadout").objectReferenceValue = loadout;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[Stage01EnemyDiceLoadoutAssetGenerator] Connected loadout to {RoundPath_Boss}");
        }

        // ── Helper ────────────────────────────────────────────────────────

        /// <summary>지정 경로에서 StageRoundDefinitionSO를 로드한다. 없으면 경고를 출력하고 null을 반환한다.</summary>
        private static StageRoundDefinitionSO FindStageRound(string assetPath, string assetName)
        {
            StageRoundDefinitionSO asset = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(assetPath);

            if (asset == null)
            {
                Debug.LogWarning($"[Stage01EnemyDiceLoadoutAssetGenerator] Could not find StageRoundDefinitionSO at path: {assetPath} (asset name: {assetName})");
                return null;
            }

            return asset;
        }
    }
}
