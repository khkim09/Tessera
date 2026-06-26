using System;
using System.Collections.Generic;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>DiceSynergy SO v4.4 전용 생성/수정 Editor 유틸리티.
    /// ShopProduct, WorkshopRules, Device, DiceType, DiceFaceUpgrade, EnemyIntent, Stage/Round는 건드리지 않는다.</summary>
    public static class DiceSynergySOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";
        private const string DiceSynergiesPath = RootPath + "/DiceSynergies";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        private static readonly List<string> _pendingEffects = new List<string>();
        private static readonly List<string> _createdOrUpdatedList = new List<string>();

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate DiceSynergy SO v4.4 메뉴 항목.
        /// DiceSynergy SO만 생성/수정한다. ShopProduct/WorkshopRules/StageRound는 건드리지 않는다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate DiceSynergy SO v4.4")]
        private static void GenerateFromMenu()
        {
            GenerateForPipeline();
        }

        /// <summary>v4.4 통합 생성 파이프라인에서 호출하는 진입점이다.</summary>
        public static void GenerateForPipeline()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _pendingEffects.Clear();
            _createdOrUpdatedList.Clear();

            // 1. 필요한 폴더 생성
            EnsureFolder(RootPath, "DiceSynergies");

            // 2. DiceSynergy SO 11종 생성/업데이트
            CreateAllDiceSynergiesV44();

            // 3. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 결과 출력
            Debug.Log($"[DiceSynergySOV44Generator] === DiceSynergy SO v4.4 생성/수정 완료 ===");
            Debug.Log($"[DiceSynergySOV44Generator] 생성: {_createdCount}, 수정: {_updatedCount}");

            Debug.Log($"[DiceSynergySOV44Generator] === 생성/수정한 DiceSynergy SO 목록 ({_createdOrUpdatedList.Count}건) ===");
            foreach (string name in _createdOrUpdatedList)
            {
                Debug.Log($"[DiceSynergySOV44Generator]   - {name}");
            }

            if (_pendingEffects.Count > 0)
            {
                Debug.Log($"[DiceSynergySOV44Generator] === 보류된 DiceSynergy 효과 목록 ({_pendingEffects.Count}건) ===");
                foreach (string msg in _pendingEffects)
                {
                    Debug.Log($"[DiceSynergySOV44Generator] [보류] {msg}");
                }
            }
            else
            {
                Debug.Log("[DiceSynergySOV44Generator] 보류된 효과 없음.");
            }

            Debug.Log("[DiceSynergySOV44Generator] === 완료. ShopProduct/WorkshopRules/StageRound는 생성하지 않았음 ===");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[DiceSynergySOV44Generator] Created folder: {path}");
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
                Debug.Log($"[DiceSynergySOV44Generator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[DiceSynergySOV44Generator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceSynergySOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceSynergySOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.intValue = value;
            return true;
        }

        private static bool SetFloat(SerializedObject so, string fieldName, float value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceSynergySOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.floatValue = value;
            return true;
        }

        private static bool SetEnum<TEnum>(SerializedObject so, string fieldName, TEnum value) where TEnum : Enum
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceSynergySOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            string enumName = value.ToString();
            int index = Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[DiceSynergySOV44Generator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
                return false;
            }
            prop.enumValueIndex = index;
            return true;
        }

        private static void ApplyAndDirty(SerializedObject so, UnityEngine.Object asset)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        // ── DiceSynergy SO v4.4 생성 ──────────────────────────────────

        /// <summary>11종의 DiceSynergy SO를 v4.4 기준으로 생성/업데이트한다.
        /// DiceSynergy_Broken_2, DiceSynergy_Broken_4는 생성하지 않는다.</summary>
        private static void CreateAllDiceSynergiesV44()
        {
            // 1. DiceSynergy_Red_2 (기존 수정)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Red_2.asset",
                "synergy.red.2", "Red 2 Set",
                "Red Dice가 2개 이상이면 홀수/공격형 조합 보너스 후보.",
                DiceSynergyTag.Red, 2,
                DiceSynergyEffectType.AddScoreForOddDice, 2, 0f);

            // 2. DiceSynergy_Red_4 (기존 수정)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Red_4.asset",
                "synergy.red.4", "Red 4 Set",
                "Red Dice가 4개 이상이면 강한 홀수/공격형 조합 보너스 후보.",
                DiceSynergyTag.Red, 4,
                DiceSynergyEffectType.AddForceIfOddDiceCountAtLeast, 1, 0f);

            // 3. DiceSynergy_Blue_2 (기존 수정)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Blue_2.asset",
                "synergy.blue.2", "Blue 2 Set",
                "Blue Dice가 2개 이상이면 짝수/안정형 조합 보너스 후보.",
                DiceSynergyTag.Blue, 2,
                DiceSynergyEffectType.AddScoreForEvenDice, 2, 0f);

            // 4. DiceSynergy_Blue_4 (기존 수정)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Blue_4.asset",
                "synergy.blue.4", "Blue 4 Set",
                "Blue Dice가 4개 이상이면 강한 짝수/방어형 조합 보너스 후보.",
                DiceSynergyTag.Blue, 4,
                DiceSynergyEffectType.AddForceIfEvenDiceCountAtLeast, 1, 0f);

            // 5. DiceSynergy_Green_2 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Green_2.asset",
                "synergy.green.2", "Green 2 Set",
                "Green Dice가 2개 이상이면 Straight/Chance/성장형 조합 보너스 후보.",
                DiceSynergyTag.Green, 2,
                DiceSynergyEffectType.None, 0, 0f);

            // 6. DiceSynergy_Green_4 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Green_4.asset",
                "synergy.green.4", "Green 4 Set",
                "Green Dice가 4개 이상이면 강한 성장형 조합 보너스 후보.",
                DiceSynergyTag.Green, 4,
                DiceSynergyEffectType.None, 0, 0f);

            // 7. DiceSynergy_Iron_3 (기존 수정)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Iron_3.asset",
                "synergy.iron.3", "Iron 3 Set",
                "Iron Dice가 3개 이상이면 높은 눈금/묵직한 CastPower 조합 보너스 후보.",
                DiceSynergyTag.Iron, 3,
                DiceSynergyEffectType.AddScoreForHighDice, 3, 0f);

            // 8. DiceSynergy_Gold_2 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Gold_2.asset",
                "synergy.gold.2", "Gold 2 Set",
                "Gold Dice가 2개 이상이면 경제 보상 조합 보너스 후보.",
                DiceSynergyTag.Gold, 2,
                DiceSynergyEffectType.AddMoneyOnRoundWin, 1, 0f);

            // 9. DiceSynergy_Lucky_2 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Lucky_2.asset",
                "synergy.lucky.2", "Lucky 2 Set",
                "Lucky Dice가 2개 이상이면 Roll/확률/유틸리티 조합 보너스 후보.",
                DiceSynergyTag.None, 2,
                DiceSynergyEffectType.None, 0, 0f);

            // 10. DiceSynergy_Void_2 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Void_2.asset",
                "synergy.void.2", "Void 2 Set",
                "Void Dice가 2개 이상이면 Broken Cast/Overcharge/위험 보상 조합 보너스 후보.",
                DiceSynergyTag.Void, 2,
                DiceSynergyEffectType.None, 0, 0f);

            // 11. DiceSynergy_Void_4 (신규)
            CreateDiceSynergyV44(
                DiceSynergiesPath + "/DiceSynergy_Void_4.asset",
                "synergy.void.4", "Void 4 Set",
                "Void Dice가 4개 이상이면 강한 위험 보상형 조합 보너스 후보.",
                DiceSynergyTag.Void, 4,
                DiceSynergyEffectType.None, 0, 0f);
        }

        /// <summary>DiceSynergy SO를 v4.4 기준으로 생성하거나 업데이트한다.</summary>
        private static void CreateDiceSynergyV44(
            string path,
            string synergyId,
            string displayName,
            string description,
            DiceSynergyTag requiredTag,
            int requiredCount,
            DiceSynergyEffectType effectType,
            int intValue,
            float floatValue)
        {
            DiceSynergyDefinitionSO asset = LoadOrCreateAsset<DiceSynergyDefinitionSO>(path);
            ApplyDiceSynergyFieldsV44(asset, synergyId, displayName, description,
                requiredTag, requiredCount, effectType, intValue, floatValue);

            // 기록
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            _createdOrUpdatedList.Add(fileName);

            // 보류 효과 기록
            if (effectType == DiceSynergyEffectType.None)
            {
                _pendingEffects.Add($"{synergyId} ({displayName}): 효과=None. DiceSynergyEffectType에 적합한 효과 enum이 없어 보류.");
            }
            else
            {
                if (description.Contains("후보"))
                {
                    _pendingEffects.Add($"{synergyId} ({displayName}): 효과={effectType} 지정됨. 런타임 계산 연결 후 검증 필요.");
                }
            }
        }

        /// <summary>SerializedObject를 통해 v4.4 DiceSynergy SO 필드를 설정한다.</summary>
        private static void ApplyDiceSynergyFieldsV44(
            ScriptableObject asset,
            string synergyId,
            string displayName,
            string description,
            DiceSynergyTag requiredTag,
            int requiredCount,
            DiceSynergyEffectType effectType,
            int intValue,
            float floatValue)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            allOk &= SetString(so, "synergyId", synergyId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            allOk &= SetEnum(so, "requiredTag", requiredTag);
            allOk &= SetInt(so, "requiredCount", requiredCount);
            allOk &= SetEnum(so, "effectType", effectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);

            if (!allOk)
            {
                Debug.LogError("[DiceSynergySOV44Generator] Failed to set some fields on DiceSynergy asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[DiceSynergySOV44Generator] Applied DiceSynergy v4.4 fields: {AssetDatabase.GetAssetPath(asset)} synergyId={synergyId}");
        }
    }
}
