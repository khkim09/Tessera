using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// EnemyIntent / EnemyIntentProfile / EnemyDiceLoadout SO v4.4 전용 생성/수정 Editor 유틸리티.
    /// StageRoundDefinitionSO, StageDefinitionSO, WorkshopRules, ShopProduct, Device, DiceType, DiceFaceUpgrade는 건드리지 않는다.
    /// Tools/Tessera/Assets/Generate Enemy Intent Profile Loadout SO v4.4 메뉴에서 실행한다.
    /// </summary>
    public static class EnemyIntentProfileLoadoutSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string EnemiesRootPath = "Assets/_Project/ScriptableObjects/Enemies";
        private const string IntentsFolderPath = EnemiesRootPath + "/Intents";
        private const string ProfilesFolderPath = EnemiesRootPath + "/IntentProfiles";
        private const string DiceLoadoutsFolderPath = EnemiesRootPath + "/DiceLoadouts";

        // ── Intent 에셋 경로 ─────────────────────────────────────────────
        private const string IntentPath_CleanOpeningStrike = IntentsFolderPath + "/Intent_CleanOpeningStrike.asset";
        private const string IntentPath_OpeningWindow = IntentsFolderPath + "/Intent_OpeningWindow.asset";
        private const string IntentPath_OpeningStrike = IntentsFolderPath + "/Intent_OpeningStrike.asset";
        private const string IntentPath_Execution = IntentsFolderPath + "/Intent_Execution.asset";

        // ── Profile 에셋 경로 ─────────────────────────────────────────────
        private const string ProfilePath_TutorialCleanStrike = ProfilesFolderPath + "/Profile_Tutorial_CleanStrike.asset";
        private const string ProfilePath_Stage01PlayerOpening = ProfilesFolderPath + "/Profile_Stage01_PlayerOpening.asset";
        private const string ProfilePath_Stage01Aggression = ProfilesFolderPath + "/Profile_Stage01_Aggression.asset";
        private const string ProfilePath_Stage01Boss = ProfilesFolderPath + "/Profile_Stage01_Boss.asset";

        // ── Dice Loadout 에셋 경로 ─────────────────────────────────────────
        private const string LoadoutPath_TutorialLow = DiceLoadoutsFolderPath + "/Loadout_TutorialLow.asset";
        private const string LoadoutPath_Stage01Balanced = DiceLoadoutsFolderPath + "/Loadout_Stage01Balanced.asset";
        private const string LoadoutPath_Stage01Aggressive = DiceLoadoutsFolderPath + "/Loadout_Stage01Aggressive.asset";
        private const string LoadoutPath_Stage01Boss = DiceLoadoutsFolderPath + "/Loadout_Stage01Boss.asset";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        private static System.Collections.Generic.List<string> _createdOrUpdatedAssets;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>
        /// Tools/Tessera/Assets/Generate Enemy Intent Profile Loadout SO v4.4 메뉴 항목.
        /// EnemyIntent SO, EnemyIntentProfile SO, EnemyDiceLoadout SO만 생성/수정한다.
        /// StageRoundDefinitionSO는 수정하지 않는다.
        /// </summary>
        [MenuItem("Tools/Tessera/Assets/Generate Enemy Intent Profile Loadout SO v4.4")]
        private static void GenerateV44()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _createdOrUpdatedAssets = new System.Collections.Generic.List<string>();

            // 1. 필요한 폴더 생성
            EnsureFolder(EnemiesRootPath, "Intents");
            EnsureFolder(EnemiesRootPath, "IntentProfiles");
            EnsureFolder(EnemiesRootPath, "DiceLoadouts");

            // 2. EnemyIntent SO 4종 생성/수정
            CreateOrUpdateAllIntents();

            // 3. EnemyIntentProfile SO 4종 생성/수정
            CreateOrUpdateAllProfiles();

            // 4. EnemyDiceLoadout SO 4종 생성/수정
            CreateOrUpdateAllLoadouts();

            // 5. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. 결과 출력
            Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] === Enemy Intent Profile Loadout SO v4.4 생성/수정 완료 ===");
            Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] 생성: {_createdCount}, 수정: {_updatedCount}");

            if (_createdOrUpdatedAssets.Count > 0)
            {
                Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] === 생성/수정 목록 ({_createdOrUpdatedAssets.Count}건) ===");
                foreach (string msg in _createdOrUpdatedAssets)
                {
                    Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] {msg}");
                }
            }

            Debug.Log("[EnemyIntentProfileLoadoutSOV44Generator] === 완료. StageRoundDefinitionSO/ShopProduct/WorkshopRules/Device/DiceType/DiceFaceUpgrade는 수정하지 않았음 ===");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] Created folder: {path}");
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
                Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.stringValue = value;
            return true;
        }

        private static bool SetInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.intValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.boolValue = value;
            return true;
        }

        private static bool SetEnum<TEnum>(SerializedObject so, string fieldName, TEnum value) where TEnum : System.Enum
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            string enumName = value.ToString();
            int index = System.Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.enumValueIndex = index;
            return true;
        }

        private static bool SetObjectRef(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.objectReferenceValue = value;
            return true;
        }

        /// <summary>SerializedObject 변경사항을 적용하고 에셋을 저장 대상으로 표시한다.</summary>
        private static void ApplyAndDirty(SerializedObject so, UnityEngine.Object asset)
        {
            bool applied = so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);

#if UNITY_2021_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#endif

            if (!applied)
                Debug.LogWarning("[EnemyIntentProfileLoadoutSOV44Generator] SerializedObject 변경사항이 없거나 적용되지 않았습니다: " + AssetDatabase.GetAssetPath(asset));
        }

        /// <summary>저장된 EnemyIntent SO 값이 기대값으로 반영됐는지 검증한다.</summary>
        private static void ValidateIntentAsset(
            string path,
            string expectedIntentId,
            string expectedDisplayName,
            int expectedTargetPowerToStop,
            int expectedTargetImpactToStop)
        {
            EnemyIntentDefinitionSO asset = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(path);

            if (asset == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Intent 검증 실패. 에셋을 다시 로드할 수 없습니다: " + path);
                return;
            }

            if (asset.IntentId != expectedIntentId)
            {
                Debug.LogError($"[EnemyIntentProfileLoadoutSOV44Generator] IntentId 검증 실패: {path} expected={expectedIntentId}, actual={asset.IntentId}");
                return;
            }

            if (asset.DisplayName != expectedDisplayName)
            {
                Debug.LogError($"[EnemyIntentProfileLoadoutSOV44Generator] DisplayName 검증 실패: {path} expected={expectedDisplayName}, actual={asset.DisplayName}");
                return;
            }

            if (asset.TargetPowerToStop != expectedTargetPowerToStop || asset.TargetImpactToStop != expectedTargetImpactToStop)
            {
                Debug.LogError($"[EnemyIntentProfileLoadoutSOV44Generator] Stop 값 검증 실패: {path} expectedPower={expectedTargetPowerToStop}, actualPower={asset.TargetPowerToStop}, expectedImpact={expectedTargetImpactToStop}, actualImpact={asset.TargetImpactToStop}");
                return;
            }

            Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] Intent 검증 통과: {path}");
        }

        /// <summary>저장된 EnemyIntentProfile SO 참조가 기대값으로 반영됐는지 검증한다.</summary>
        private static void ValidateProfileAsset(
            string path,
            EnemyIntentDefinitionSO expectedOpeningIntent,
            EnemyIntentDefinitionSO[] expectedIntentPool)
        {
            EnemyIntentProfileSO asset = AssetDatabase.LoadAssetAtPath<EnemyIntentProfileSO>(path);

            if (asset == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Profile 검증 실패. 에셋을 다시 로드할 수 없습니다: " + path);
                return;
            }

            if (asset.OpeningIntent != expectedOpeningIntent)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] OpeningIntent 검증 실패: " + path);
                return;
            }

            if (asset.IntentPool == null || expectedIntentPool == null || asset.IntentPool.Count != expectedIntentPool.Length)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] IntentPool 개수 검증 실패: " + path);
                return;
            }

            for (int i = 0; i < expectedIntentPool.Length; i++)
            {
                if (asset.IntentPool[i] != expectedIntentPool[i])
                {
                    Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] IntentPool 참조 검증 실패: " + path + " index=" + i);
                    return;
                }
            }

            Debug.Log($"[EnemyIntentProfileLoadoutSOV44Generator] Profile 검증 통과: {path}");
        }

        // ── EnemyIntent SO v4.4 생성/수정 ──────────────────────────────

        /// <summary>4종의 EnemyIntent SO를 v4.4 기준으로 생성/수정한다.</summary>
        private static void CreateOrUpdateAllIntents()
        {
            // 1. Intent_CleanOpeningStrike
            CreateOrUpdateIntent(
                IntentPath_CleanOpeningStrike,
                "intent.clean_opening_strike",
                "Clean Opening Strike",
                "Opponent rolls first without Device effects.",
                "상대가 먼저 굴리지만 Device를 사용하지 않는 튜토리얼용 Intent.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: false,
                castSelectionPolicy: OpponentCastSelectionPolicy.TargetBandFirst,
                targetPowerToStop: 25,
                targetImpactToStop: 5,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            // 2. Intent_OpeningWindow
            CreateOrUpdateIntent(
                IntentPath_OpeningWindow,
                "intent.opening_window",
                "Opening Window",
                "You act first before the opponent responds.",
                "플레이어가 먼저 CastPower를 만들고 상대가 대응하는 학습/일반용 Intent.",
                EnemyIntentCategoryType.Tactics,
                InitiativeOwnerType.Player,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.TargetBandFirst,
                targetPowerToStop: 40,
                targetImpactToStop: 6,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            // 3. Intent_OpeningStrike
            CreateOrUpdateIntent(
                IntentPath_OpeningStrike,
                "intent.opening_strike",
                "Opening Strike",
                "Opponent rolls first and pressures your Clash.",
                "상대가 먼저 굴려 기본적인 공방 압박을 가하는 일반 공격형 Intent.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.UtilityBest,
                targetPowerToStop: 45,
                targetImpactToStop: 7,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.Balanced);

            // 4. Intent_Execution
            CreateOrUpdateIntent(
                IntentPath_Execution,
                "intent.execution",
                "Execution",
                "Opponent prioritizes strong CastPower and lethal impact.",
                "상대가 높은 CastPower와 처치 가능성을 우선하는 보스/후반용 Intent.",
                EnemyIntentCategoryType.BossUltimate,
                InitiativeOwnerType.Opponent,
                useOpponentDevices: true,
                castSelectionPolicy: OpponentCastSelectionPolicy.UtilityBest,
                targetPowerToStop: 55,
                targetImpactToStop: 9,
                stopIfBeatsPlayerPower: true,
                rollStrategy: OpponentRollStrategyType.GreedyBestDamage);
        }

        /// <summary>EnemyIntent SO를 v4.4 기준으로 생성하거나 업데이트한다.</summary>
        private static void CreateOrUpdateIntent(
            string path,
            string intentId,
            string displayName,
            string shortDescription,
            string bountyCardDescription,
            EnemyIntentCategoryType categoryType,
            InitiativeOwnerType initiativeOwner,
            bool useOpponentDevices,
            OpponentCastSelectionPolicy castSelectionPolicy,
            int targetPowerToStop,
            int targetImpactToStop,
            bool stopIfBeatsPlayerPower,
            OpponentRollStrategyType rollStrategy)
        {
            EnemyIntentDefinitionSO asset = LoadOrCreateAsset<EnemyIntentDefinitionSO>(path);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            bool allOk = true;

            allOk &= SetString(so, "intentId", intentId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "shortDescription", shortDescription);
            allOk &= SetString(so, "bountyCardDescription", bountyCardDescription);
            allOk &= SetEnum(so, "categoryType", categoryType);
            allOk &= SetEnum(so, "initiativeOwner", initiativeOwner);
            allOk &= SetBool(so, "useOpponentDevices", useOpponentDevices);
            allOk &= SetEnum(so, "castSelectionPolicy", castSelectionPolicy);
            allOk &= SetInt(so, "targetPowerToStop", targetPowerToStop);
            allOk &= SetInt(so, "targetImpactToStop", targetImpactToStop);
            allOk &= SetBool(so, "stopIfBeatsPlayerPower", stopIfBeatsPlayerPower);
            allOk &= SetEnum(so, "rollStrategy", rollStrategy);

            if (!allOk)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Failed to set some fields on EnemyIntent asset: " + path);
            }

            ApplyAndDirty(so, asset);
            ValidateIntentAsset(path, intentId, displayName, targetPowerToStop, targetImpactToStop);
            _createdOrUpdatedAssets.Add($"Intent: {path} (id={intentId})");
        }

        // ── EnemyIntentProfile SO v4.4 생성/수정 ───────────────────────

        /// <summary>4종의 EnemyIntentProfile SO를 v4.4 기준으로 생성/수정한다.</summary>
        private static void CreateOrUpdateAllProfiles()
        {
            // Intent 참조를 먼저 로드
            EnemyIntentDefinitionSO intentCleanOpeningStrike = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(IntentPath_CleanOpeningStrike);
            EnemyIntentDefinitionSO intentOpeningWindow = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(IntentPath_OpeningWindow);
            EnemyIntentDefinitionSO intentOpeningStrike = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(IntentPath_OpeningStrike);
            EnemyIntentDefinitionSO intentExecution = AssetDatabase.LoadAssetAtPath<EnemyIntentDefinitionSO>(IntentPath_Execution);

            if (intentCleanOpeningStrike == null || intentOpeningWindow == null ||
                intentOpeningStrike == null || intentExecution == null)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] One or more Intent assets not found. Run Intent generation first.");
                return;
            }

            // 1. Profile_Tutorial_CleanStrike
            CreateOrUpdateProfile(
                ProfilePath_TutorialCleanStrike,
                "Profile_Tutorial_CleanStrike",
                intentCleanOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentCleanOpeningStrike });

            // 2. Profile_Stage01_PlayerOpening
            CreateOrUpdateProfile(
                ProfilePath_Stage01PlayerOpening,
                "Profile_Stage01_PlayerOpening",
                intentOpeningWindow,
                new EnemyIntentDefinitionSO[] { intentOpeningWindow, intentOpeningStrike });

            // 3. Profile_Stage01_Aggression
            CreateOrUpdateProfile(
                ProfilePath_Stage01Aggression,
                "Profile_Stage01_Aggression",
                intentOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentOpeningStrike, intentOpeningWindow });

            // 4. Profile_Stage01_Boss
            CreateOrUpdateProfile(
                ProfilePath_Stage01Boss,
                "Profile_Stage01_Boss",
                intentOpeningStrike,
                new EnemyIntentDefinitionSO[] { intentOpeningStrike, intentExecution, intentOpeningWindow });
        }

        /// <summary>EnemyIntentProfile SO를 v4.4 기준으로 생성하거나 업데이트한다.</summary>
        private static void CreateOrUpdateProfile(
            string path,
            string profileName,
            EnemyIntentDefinitionSO openingIntent,
            EnemyIntentDefinitionSO[] intentPool)
        {
            EnemyIntentProfileSO asset = LoadOrCreateAsset<EnemyIntentProfileSO>(path);
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            bool allOk = true;

            // openingIntent
            allOk &= SetObjectRef(so, "openingIntent", openingIntent);

            // intentPool 배열
            SerializedProperty poolProp = so.FindProperty("intentPool");
            if (poolProp == null || !poolProp.isArray)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing array field: intentPool on " + so.targetObject.name);
                allOk = false;
            }
            else
            {
                poolProp.ClearArray();
                poolProp.arraySize = intentPool.Length;
                for (int i = 0; i < intentPool.Length; i++)
                {
                    poolProp.GetArrayElementAtIndex(i).objectReferenceValue = intentPool[i];
                }
            }

            if (!allOk)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Failed to set some fields on EnemyIntentProfile asset: " + path);
            }

            ApplyAndDirty(so, asset);
            ValidateProfileAsset(path, openingIntent, intentPool);
            _createdOrUpdatedAssets.Add($"Profile: {path} (name={profileName})");
        }

        // ── EnemyDiceLoadout SO v4.4 생성/수정 ─────────────────────────

        /// <summary>4종의 EnemyDiceLoadout SO를 v4.4 기준으로 생성/수정한다.</summary>
        private static void CreateOrUpdateAllLoadouts()
        {
            // 1. Loadout_TutorialLow
            // 각 dice number faces: [1, 1, 2, 2, 3, 4]
            CreateOrUpdateLoadout(
                LoadoutPath_TutorialLow,
                "loadout.tutorial_low",
                "Tutorial Low",
                new int[][]
                {
                    new int[] { 1, 1, 2, 2, 3, 4 },
                    new int[] { 1, 1, 2, 2, 3, 4 },
                    new int[] { 1, 1, 2, 2, 3, 4 },
                    new int[] { 1, 1, 2, 2, 3, 4 },
                    new int[] { 1, 1, 2, 2, 3, 4 }
                });

            // 2. Loadout_Stage01Balanced
            // 각 dice number faces: [1, 2, 3, 4, 5, 6]
            CreateOrUpdateLoadout(
                LoadoutPath_Stage01Balanced,
                "loadout.stage01_balanced",
                "Stage01 Balanced",
                new int[][]
                {
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 },
                    new int[] { 1, 2, 3, 4, 5, 6 }
                });

            // 3. Loadout_Stage01Aggressive
            // 각 dice number faces: [2, 3, 4, 4, 5, 6]
            CreateOrUpdateLoadout(
                LoadoutPath_Stage01Aggressive,
                "loadout.stage01_aggressive",
                "Stage01 Aggressive",
                new int[][]
                {
                    new int[] { 2, 3, 4, 4, 5, 6 },
                    new int[] { 2, 3, 4, 4, 5, 6 },
                    new int[] { 2, 3, 4, 4, 5, 6 },
                    new int[] { 2, 3, 4, 4, 5, 6 },
                    new int[] { 2, 3, 4, 4, 5, 6 }
                });

            // 4. Loadout_Stage01Boss
            // 각 dice number faces: [2, 3, 4, 5, 5, 6]
            CreateOrUpdateLoadout(
                LoadoutPath_Stage01Boss,
                "loadout.stage01_boss",
                "Stage01 Boss",
                new int[][]
                {
                    new int[] { 2, 3, 4, 5, 5, 6 },
                    new int[] { 2, 3, 4, 5, 5, 6 },
                    new int[] { 2, 3, 4, 5, 5, 6 },
                    new int[] { 2, 3, 4, 5, 5, 6 },
                    new int[] { 2, 3, 4, 5, 5, 6 }
                });
        }

        /// <summary>EnemyDiceLoadout SO를 v4.4 기준으로 생성하거나 업데이트한다.</summary>
        private static void CreateOrUpdateLoadout(
            string path,
            string loadoutId,
            string displayName,
            int[][] diceFaceSets)
        {
            EnemyDiceLoadoutDefinitionSO asset = LoadOrCreateAsset<EnemyDiceLoadoutDefinitionSO>(path);
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "loadoutId", loadoutId);
            allOk &= SetString(so, "displayName", displayName);

            // diceDefinitions 배열 설정
            SerializedProperty diceDefsProp = so.FindProperty("diceDefinitions");
            if (diceDefsProp == null || !diceDefsProp.isArray)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing array field: diceDefinitions on " + so.targetObject.name);
                allOk = false;
            }
            else
            {
                diceDefsProp.ClearArray();
                diceDefsProp.arraySize = diceFaceSets.Length;

                for (int i = 0; i < diceFaceSets.Length; i++)
                {
                    SerializedProperty elementProp = diceDefsProp.GetArrayElementAtIndex(i);
                    int[] faceValues = diceFaceSets[i];

                    // numberFaces 배열 설정
                    SerializedProperty numberFacesProp = elementProp.FindPropertyRelative("numberFaces");
                    if (numberFacesProp == null || !numberFacesProp.isArray)
                    {
                        Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Missing array field: numberFaces on diceDefinitions[" + i + "]");
                        allOk = false;
                    }
                    else
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

            if (!allOk)
            {
                Debug.LogError("[EnemyIntentProfileLoadoutSOV44Generator] Failed to set some fields on EnemyDiceLoadout asset: " + path);
            }

            ApplyAndDirty(so, asset);
            _createdOrUpdatedAssets.Add($"Loadout: {path} (id={loadoutId})");
        }
    }
}
