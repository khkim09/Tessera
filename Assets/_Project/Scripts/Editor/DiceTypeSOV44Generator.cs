using System;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>DiceType SO v4.4 전용 생성/수정 Editor 유틸리티.
    /// ShopProduct, WorkshopRules, DiceFaceUpgrade, Device, EnemyIntent, Stage/Round는 건드리지 않는다.</summary>
    public static class DiceTypeSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";
        private const string DiceTypesPath = RootPath + "/DiceTypes";
        private const string DiceSynergiesPath = RootPath + "/DiceSynergies";

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        // ── 보류 효과 목록 ──────────────────────────────────────────────
        private static readonly System.Collections.Generic.List<string> _pendingEffects = new System.Collections.Generic.List<string>();

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate DiceType SO v4.4 메뉴 항목.
        /// DiceType SO와 필요한 DiceSynergy SO만 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate DiceType SO v4.4")]
        private static void GenerateDiceTypeSOV44()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _pendingEffects.Clear();

            // 1. 필요한 폴더 생성
            EnsureFolder(RootPath, "DiceTypes");
            EnsureFolder(RootPath, "DiceSynergies");

            // 2. DiceType SO 8종 생성/업데이트
            CreateAllDiceTypesV44();

            // 3. DiceSynergy SO는 DiceTypeDefinitionSO가 enum 기반 DiceSynergyTag만 사용하므로
            //    별도 생성/수정이 필요하지 않음. 기존 SO는 유지.

            // 4. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. 결과 출력
            Debug.Log($"[DiceTypeSOV44Generator] === DiceType SO v4.4 생성/수정 완료 ===");
            Debug.Log($"[DiceTypeSOV44Generator] 생성: {_createdCount}, 수정: {_updatedCount}");

            if (_pendingEffects.Count > 0)
            {
                Debug.Log($"[DiceTypeSOV44Generator] === 보류된 DiceType 효과 목록 ({_pendingEffects.Count}건) ===");
                foreach (string msg in _pendingEffects)
                {
                    Debug.Log($"[DiceTypeSOV44Generator] [보류] {msg}");
                }
            }
            else
            {
                Debug.Log("[DiceTypeSOV44Generator] 보류된 효과 없음.");
            }

            Debug.Log("[DiceTypeSOV44Generator] === 완료. ShopProduct/WorkshopRules는 생성하지 않았음 ===");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[DiceTypeSOV44Generator] Created folder: {path}");
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
                Debug.Log($"[DiceTypeSOV44Generator] Updating existing asset: {assetPath}");
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[DiceTypeSOV44Generator] Created new asset: {assetPath}");
            return asset;
        }

        // ── 헬퍼: SerializedProperty Setter ────────────────────────────

        private static bool SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[DiceTypeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceTypeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceTypeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
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
                Debug.LogError("[DiceTypeSOV44Generator] Missing field: " + fieldName + " on " + so.targetObject.name);
                return false;
            }

            string enumName = value.ToString();
            int index = Array.IndexOf(prop.enumNames, enumName);
            if (index < 0)
            {
                Debug.LogError("[DiceTypeSOV44Generator] Enum value " + enumName + " not found in enumNames for field " + fieldName + " on " + so.targetObject.name);
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

        // ── DiceType SO v4.4 생성 ──────────────────────────────────────

        /// <summary>8종의 DiceType SO를 v4.4 기준으로 생성/업데이트한다.</summary>
        private static void CreateAllDiceTypesV44()
        {
            // 1. DiceType_Standard (신규)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Standard.asset",
                "dice.standard", "Standard Dice",
                "기본 주사위. 별도 보정 없이 안정적으로 사용된다.",
                new Color(0.85f, 0.85f, 0.85f), "standard",
                DiceSynergyTag.None, DiceIntrinsicEffectType.None,
                0, 0f,
                1, 1, 1, 4, 0);

            // 2. DiceType_Red (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Red.asset",
                "dice.red", "Red Dice",
                "홀수 눈금과 공격형 조합에 어울리는 DiceType 후보.",
                Color.red, "red",
                DiceSynergyTag.Red, DiceIntrinsicEffectType.AddScoreIfOdd,
                2, 0f,
                1, 1, 1, 5, 0);

            // 3. DiceType_Blue (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Blue.asset",
                "dice.blue", "Blue Dice",
                "짝수 눈금과 안정형 조합에 어울리는 DiceType 후보.",
                Color.blue, "blue",
                DiceSynergyTag.Blue, DiceIntrinsicEffectType.AddForceIfEven,
                0, 0.2f,
                1, 1, 1, 5, 0);

            // 4. DiceType_Green (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Green.asset",
                "dice.green", "Green Dice",
                "Straight, Chance, 성장형 조합에 어울리는 DiceType 후보.",
                Color.green, "green",
                DiceSynergyTag.Green, DiceIntrinsicEffectType.AddScoreIfValueAtMost,
                2, 0f,
                1, 1, 1, 5, 0);

            // 5. DiceType_Iron (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Iron.asset",
                "dice.iron", "Iron Dice",
                "높은 눈금과 묵직한 CastPower 조합에 어울리는 DiceType 후보.",
                new Color(0.4f, 0.4f, 0.4f), "iron",
                DiceSynergyTag.Iron, DiceIntrinsicEffectType.AddScoreIfValueAtLeast,
                3, 0f,
                1, 1, 1, 5, 0);

            // 6. DiceType_Gold (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Gold.asset",
                "dice.gold", "Gold Dice",
                "Round 승리 보상이나 경제 효과와 연계될 DiceType 후보.",
                Color.yellow, "gold",
                DiceSynergyTag.Gold, DiceIntrinsicEffectType.AddMoneyOnRoundWinIfUsed,
                1, 0f,
                2, 2, 2, 8, 0);

            // 7. DiceType_Lucky (신규)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Lucky.asset",
                "dice.lucky", "Lucky Dice",
                "Roll, 확률, 후보 선택 보조와 연계될 DiceType 후보.",
                new Color(0.5f, 1.0f, 0.7f), "lucky",
                DiceSynergyTag.None, DiceIntrinsicEffectType.None,
                0, 0f,
                2, 2, 2, 8, 0);

            // 8. DiceType_Void (기존 수정)
            CreateDiceTypeV44(
                DiceTypesPath + "/DiceType_Void.asset",
                "dice.void", "Void Dice",
                "Broken Cast, Overcharge, 위험 보상형 조합과 연계될 고급 DiceType 후보.",
                new Color(0.5f, 0.0f, 0.5f), "void",
                DiceSynergyTag.Void, DiceIntrinsicEffectType.ReduceIncomingDamageIfUsed,
                2, 0f,
                3, 3, 4, 10, 1);
        }

        /// <summary>DiceType SO를 v4.4 기준으로 생성하거나 업데이트한다.</summary>
        private static void CreateDiceTypeV44(
            string path,
            string diceTypeId,
            string displayName,
            string description,
            Color visualColor,
            string materialKey,
            DiceSynergyTag synergyTag,
            DiceIntrinsicEffectType intrinsicEffectType,
            int intValue,
            float floatValue,
            int tier,
            int rarity,
            int unlockStage,
            int baseMoneyPrice,
            int baseOverchargePrice)
        {
            DiceTypeDefinitionSO asset = LoadOrCreateAsset<DiceTypeDefinitionSO>(path);
            ApplyDiceTypeFieldsV44(asset, diceTypeId, displayName, description, visualColor,
                materialKey, synergyTag, intrinsicEffectType, intValue, floatValue,
                tier, rarity, unlockStage, baseMoneyPrice, baseOverchargePrice);

            // 보류 효과 기록
            if (intrinsicEffectType == DiceIntrinsicEffectType.None)
            {
                _pendingEffects.Add($"{diceTypeId} ({displayName}): 효과 없음(None). 런타임 계산 연결 후 효과 지정 필요.");
            }
            else
            {
                // 효과가 설정되었지만 "후보" 설명인 경우도 보류로 기록
                if (description.Contains("후보"))
                {
                    _pendingEffects.Add($"{diceTypeId} ({displayName}): 효과={intrinsicEffectType} 지정됨. 런타임 계산 연결 후 검증 필요.");
                }
            }
        }

        /// <summary>SerializedObject를 통해 v4.4 DiceType SO 필드를 설정한다.</summary>
        private static void ApplyDiceTypeFieldsV44(
            ScriptableObject asset,
            string diceTypeId,
            string displayName,
            string description,
            Color visualColor,
            string materialKey,
            DiceSynergyTag synergyTag,
            DiceIntrinsicEffectType intrinsicEffectType,
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

            allOk &= SetString(so, "diceTypeId", diceTypeId);
            allOk &= SetString(so, "displayName", displayName);
            allOk &= SetString(so, "description", description);
            // visualColor
            {
                SerializedProperty prop = so.FindProperty("visualColor");
                if (prop == null)
                {
                    Debug.LogError("[DiceTypeSOV44Generator] Missing field: visualColor on " + so.targetObject.name);
                    allOk = false;
                }
                else
                {
                    prop.colorValue = visualColor;
                }
            }
            allOk &= SetString(so, "materialKey", materialKey);
            allOk &= SetEnum(so, "synergyTag", synergyTag);
            allOk &= SetEnum(so, "intrinsicEffectType", intrinsicEffectType);
            allOk &= SetInt(so, "intValue", intValue);
            allOk &= SetFloat(so, "floatValue", floatValue);
            allOk &= SetInt(so, "tier", tier);
            allOk &= SetInt(so, "rarity", rarity);
            allOk &= SetInt(so, "unlockStage", unlockStage);
            allOk &= SetInt(so, "baseMoneyPrice", baseMoneyPrice);
            allOk &= SetInt(so, "baseOverchargePrice", baseOverchargePrice);

            if (!allOk)
            {
                Debug.LogError("[DiceTypeSOV44Generator] Failed to set some fields on DiceType asset: " + AssetDatabase.GetAssetPath(asset));
            }

            ApplyAndDirty(so, asset);
            Debug.Log($"[DiceTypeSOV44Generator] Applied DiceType v4.4 fields: {AssetDatabase.GetAssetPath(asset)} diceTypeId={diceTypeId}");
        }
    }
}
