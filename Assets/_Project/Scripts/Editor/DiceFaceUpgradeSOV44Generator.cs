using System;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 DiceFaceUpgrade SO 전용 생성/수정 Editor 유틸리티.
    /// ShopProduct, WorkshopRules, Device, DiceType, DiceSynergy, EnemyIntent, Stage/Round는 건드리지 않는다.</summary>
    public static class DiceFaceUpgradeSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";
        private const string DiceFaceUpgradesPath = RootPath + "/DiceFaceUpgrades";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate DiceFaceUpgrade SO v4.4 메뉴 항목.
        /// DiceFaceUpgrade SO 11종만 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate DiceFaceUpgrade SO v4.4")]
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
            EnsureFolder(RootPath, "DiceFaceUpgrades");

            // 2. DiceFaceUpgrade SO 11종 생성/업데이트
            CreateAllDiceFaceUpgrades();

            // 3. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 결과 출력
            int created = _createdCount;
            int updated = _updatedCount;
            Debug.Log($"[DiceFaceUpgradeSOV44Generator] v4.4 DiceFaceUpgrade SO Complete. Created: {created}, Updated: {updated}");

            // 5. 특수 Face 항목 안내
            Debug.Log("[DiceFaceUpgradeSOV44Generator] [특수 Face] FaceUpgrade_MirrorFace: 왼쪽 평가값 복제.");
            Debug.Log("[DiceFaceUpgradeSOV44Generator] [특수 Face] FaceUpgrade_BlankFace: Pattern 평가 기여 제외.");
            Debug.Log("[DiceFaceUpgradeSOV44Generator] [특수 Face] FaceUpgrade_WildFace: Pattern별 최선 값 선택.");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[DiceFaceUpgradeSOV44Generator] Created folder: {path}");
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
                Debug.Log($"[DiceFaceUpgradeSOV44Generator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[DiceFaceUpgradeSOV44Generator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        /// <summary>SerializedObject에서 string 필드를 설정한다.</summary>
        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.floatValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 bool 필드를 설정한다.</summary>
        private static bool SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.boolValue = value;
            return true;
        }

        /// <summary>SerializedObject에서 enum 필드를 안전하게 설정한다.</summary>
        private static bool SetEnum<TEnum>(SerializedObject so, string fieldName, TEnum value) where TEnum : Enum
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            string enumName = value.ToString();
            int index = Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
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

        // ── v4.4 DiceFaceUpgrade SO 생성 ───────────────────────────────

        /// <summary>11종의 DiceFaceUpgrade SO를 생성/업데이트한다. (v4.4 기준)</summary>
        private static void CreateAllDiceFaceUpgrades()
        {
            // 1. FaceUpgrade_RedOddRune: 홀수 Face 연계 공격형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_RedOddRune.asset",
                "face.red_odd_rune", "붉은 홀수 룬",
                "홀수 Face와 연계되는 공격형 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f,
                1, 1, 1, 5, 0);

            // 2. FaceUpgrade_BlueEvenRune: 짝수 Face 연계 안정형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_BlueEvenRune.asset",
                "face.blue_even_rune", "푸른 짝수 룬",
                "짝수 Face와 연계되는 안정형 Face Upgrade 후보.",
                false, 2, DiceFaceType.Number, 2,
                DiceFaceUpgradeEffectType.AddForceWhenRolled, 0, 0.2f,
                1, 1, 1, 5, 0);

            // 3. FaceUpgrade_HeavySix: 6 Face 강화 고점형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_HeavySix.asset",
                "face.heavy_six", "무거운 6",
                "6 Face를 강화하는 고점형 Face Upgrade 후보.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 2, 0f,
                1, 1, 1, 5, 0);

            // 4. FaceUpgrade_IronMark: 높은 눈금 연계 기본형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_IronMark.asset",
                "face.iron_mark", "철 표식",
                "높은 눈금과 연계되는 기본 Face Upgrade 후보.",
                false, 5, DiceFaceType.Number, 5,
                DiceFaceUpgradeEffectType.AddScoreWhenRolled, 3, 0f,
                1, 1, 1, 5, 0);

            // 5. FaceUpgrade_CoinMark: 보상/경제 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_CoinMark.asset",
                "face.coin_mark", "동전 표식",
                "보상/경제 효과와 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddMoneyOnRoundWinWhenUsed, 1, 0f,
                1, 1, 1, 5, 0);

            // 6. FaceUpgrade_GuardMark: 피해 방어 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_GuardMark.asset",
                "face.guard_mark", "가드 표식",
                "피해 방어 효과와 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.ReduceIncomingDamageWhenUsed, 5, 0f,
                2, 2, 2, 7, 0);

            // 7. FaceUpgrade_OverchargeMark: Overcharge 획득 연계형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_OverchargeMark.asset",
                "face.overcharge_mark", "과충전 표식",
                "Overcharge 획득과 연계될 Face Upgrade 후보.",
                false, 1, DiceFaceType.Number, 1,
                DiceFaceUpgradeEffectType.AddOverchargeWhenUsed, 1, 0f,
                2, 2, 2, 7, 0);

            // 8. FaceUpgrade_CrackedFace: 위험 보상형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_CrackedFace.asset",
                "face.cracked_face", "금 간 Face",
                "강한 보정과 패널티를 함께 갖는 위험 보상형 Face Upgrade 후보.",
                false, 6, DiceFaceType.Number, 6,
                DiceFaceUpgradeEffectType.IncreaseIncomingDamageWhenLose, 10, 0f,
                2, 2, 2, 7, 0);

            // 9. FaceUpgrade_MirrorFace: 왼쪽 평가값 복제형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_MirrorFace.asset",
                "face.mirror_face", "거울 Face",
                "왼쪽 Dice의 평가값을 복제하는 특수 Face Upgrade.",
                false, 1, DiceFaceType.Mirror, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                2, 2, 3, 8, 0);

            // 10. FaceUpgrade_BlankFace: 패턴 기여 제외형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_BlankFace.asset",
                "face.blank_face", "빈 Face",
                "Pattern 평가에서 기여하지 않는 특수 Face Upgrade.",
                false, 1, DiceFaceType.Blank, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                2, 2, 3, 8, 0);

            // 11. FaceUpgrade_WildFace: 유리한 값 선택형
            CreateDiceFaceUpgradeV44(DiceFaceUpgradesPath + "/FaceUpgrade_WildFace.asset",
                "face.wild_face", "와일드 Face",
                "Pattern 평가에서 가장 유리한 1~6 값으로 취급되는 고급 Face Upgrade.",
                false, 1, DiceFaceType.Wild, 1,
                DiceFaceUpgradeEffectType.None, 0, 0f,
                3, 3, 4, 10, 1);
        }

        /// <summary>v4.4 DiceFaceUpgrade SO를 생성하거나 업데이트한다. (tier/unlockStage/baseOverchargePrice 포함)</summary>
        private static void CreateDiceFaceUpgradeV44(
            string path,
            string upgradeId,
            string displayName,
            string description,
            bool requiresSpecificNumber,
            int targetNumber,
            DiceFaceType replacementFaceType,
            int replacementNumberValue,
            DiceFaceUpgradeEffectType effectType,
            int intValue,
            float floatValue,
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
        {
            DiceFaceUpgradeDefinitionSO asset = LoadOrCreateAsset<DiceFaceUpgradeDefinitionSO>(path);
            ApplyDiceFaceUpgradeFieldsV44(asset, upgradeId, displayName, description,
                requiresSpecificNumber, targetNumber, replacementFaceType, replacementNumberValue,
                effectType, intValue, floatValue, tier, rarity, unlockStage, baseMoneyPrice, baseOverchargePrice);
        }

        /// <summary>SerializedObject를 통해 v4.4 DiceFaceUpgrade SO 필드를 설정한다.</summary>
        private static void ApplyDiceFaceUpgradeFieldsV44(
            ScriptableObject asset,
            string upgradeId,
            string displayName,
            string description,
            bool requiresSpecificNumber,
            int targetNumber,
            DiceFaceType replacementFaceType,
            int replacementNumberValue,
            DiceFaceUpgradeEffectType effectType,
            int intValue,
            float floatValue,
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "upgradeId", upgradeId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetBool(so, "requiresSpecificNumber", requiresSpecificNumber);
            allOk &= SetInt(so, "targetNumber", targetNumber);
            allOk &= SetEnum(so, "replacementFaceType", replacementFaceType);
            allOk &= SetInt(so, "replacementNumberValue", replacementNumberValue);
            allOk &= SetEnum(so, "effectType", effectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);
            allOk &= SetInt(so, "tier", tier);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "unlockStage", unlockStage);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);
            allOk &= SetInt(so, "baseOverchargePrice", baseOverchargePrice);

            if (!allOk)
            {
                Debug.LogError("[DiceFaceUpgradeSOV44Generator] Failed to set some fields on DiceFaceUpgrade asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[DiceFaceUpgradeSOV44Generator] Applied DiceFaceUpgrade v4.4 fields: {AssetDatabase.GetAssetPath(asset)} upgradeId={upgradeId}");
        }
    }
}
