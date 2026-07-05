using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 DiceFaceUpgrade ShopProduct SO를 생성/수정하고 Stage01_WorkshopRules의 FaceUpgrade 슬롯 productPool을 연결하는 Editor 유틸리티다.</summary>
    public static class ShopProductDiceFaceUpgradeSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";

        private const string DiceFaceUpgradesSourcePath = RootPath + "/DiceFaceUpgrades";
        private const string ShopFaceUpgradesPath = RootPath + "/Shop/Generated/DiceFaceUpgrades";

        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";
        private const string WorkshopRulesPath = StagesFolderPath + "/Stage01_WorkshopRules.asset";

        // ── v4.4 대상 DiceFaceUpgrade SO 이름 목록 ──────────────────────
        private static readonly string[] FaceUpgradeNames = new string[]
        {
            "FaceUpgrade_RedOddRune",
            "FaceUpgrade_BlueEvenRune",
            "FaceUpgrade_HeavySix",
            "FaceUpgrade_IronMark",
            "FaceUpgrade_CoinMark",
            "FaceUpgrade_GuardMark",
            "FaceUpgrade_OverchargeMark",
            "FaceUpgrade_CrackedFace",
            "FaceUpgrade_MirrorFace",
            "FaceUpgrade_BlankFace",
            "FaceUpgrade_WildFace"
        };

        // ── 특수 FaceUpgrade 이름 목록 ─────────────────────────
        // effectType은 None이지만 replacementFaceType 자체가 PatternEvaluator에서 처리되는 상품
        private static readonly HashSet<string> PendingFaceUpgradeNames = new HashSet<string>
        {
            "FaceUpgrade_MirrorFace",
            "FaceUpgrade_BlankFace",
            "FaceUpgrade_WildFace"
        };

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        private static int _skippedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate ShopProduct DiceFaceUpgrade SO v4.4 메뉴 항목이다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate ShopProduct DiceFaceUpgrade SO v4.4")]
        private static void GenerateFromMenu()
        {
            GenerateForPipeline();
        }

        /// <summary>v4.4 통합 생성 파이프라인에서 호출하는 진입점이다.</summary>
        public static void GenerateForPipeline()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _skippedCount = 0;

            // 1. 폴더 생성
            EnsureFolder(RootPath + "/Shop", "Generated");
            EnsureFolder(RootPath + "/Shop/Generated", "DiceFaceUpgrades");

            // 2. DiceFaceUpgrade SO 11종 로드
            DiceFaceUpgradeDefinitionSO[] faceUpgrades = LoadFaceUpgrades();

            if (faceUpgrades.Length == 0)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] 로드된 DiceFaceUpgrade SO가 없습니다. 생성을 중단합니다.");
                return;
            }

            // 3. DiceFaceUpgrade ShopProduct 11종 생성/수정
            List<ShopProductDefinitionSO> createdOrUpdatedProducts = new List<ShopProductDefinitionSO>();
            CreateOrUpdateFaceUpgradeShopProducts(faceUpgrades, createdOrUpdatedProducts);

            // 4. Stage01_WorkshopRules FaceUpgrade productPool 연결
            ConnectFaceUpgradeShopProductsToWorkshopRules(createdOrUpdatedProducts);

            // 5. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. 레거시 ShopProduct 보고
            ReportLegacyCandidates();

            // 7. 검증
            ValidateGeneratedAssets();

            // 8. 결과 출력
            Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] Complete. Created: {_createdCount}, Updated: {_updatedCount}, Skipped: {_skippedCount}");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] Created folder: {path}");
            }
        }

        // ── DiceFaceUpgrade SO 로드 ─────────────────────────────────────

        /// <summary>v4.4 대상 DiceFaceUpgrade SO 11종을 로드한다.</summary>
        private static DiceFaceUpgradeDefinitionSO[] LoadFaceUpgrades()
        {
            List<DiceFaceUpgradeDefinitionSO> upgrades = new List<DiceFaceUpgradeDefinitionSO>();

            for (int i = 0; i < FaceUpgradeNames.Length; i++)
            {
                string name = FaceUpgradeNames[i];
                string assetPath = DiceFaceUpgradesSourcePath + "/" + name + ".asset";
                DiceFaceUpgradeDefinitionSO upgrade = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(assetPath);

                if (upgrade == null)
                {
                    Debug.LogWarning($"[ShopProductDiceFaceUpgradeSOV44Generator] DiceFaceUpgrade not found (skipped): {assetPath}");
                    _skippedCount++;
                    continue;
                }

                upgrades.Add(upgrade);
            }

            return upgrades.ToArray();
        }

        // ── DiceFaceUpgrade ShopProduct 생성/수정 ───────────────────────

        /// <summary>DiceFaceUpgrade 배열에 대해 ShopProduct를 생성/수정하고 목록에 추가한다.</summary>
        private static void CreateOrUpdateFaceUpgradeShopProducts(
            DiceFaceUpgradeDefinitionSO[] faceUpgrades,
            List<ShopProductDefinitionSO> createdOrUpdatedProducts)
        {
            for (int i = 0; i < faceUpgrades.Length; i++)
            {
                DiceFaceUpgradeDefinitionSO upgrade = faceUpgrades[i];

                if (upgrade == null)
                    continue;

                // upgradeId를 SerializedObject로 읽는다
                string upgradeId = ReadUpgradeId(upgrade);

                if (string.IsNullOrWhiteSpace(upgradeId))
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] upgradeId를 읽을 수 없습니다: {AssetDatabase.GetAssetPath(upgrade)}");
                    _skippedCount++;
                    continue;
                }

                // productId 생성: "shop." + upgradeId
                string productId = "shop." + upgradeId;

                // 파일명 생성: FaceUpgrade_XXX.asset → ShopProduct_FaceUpgrade_XXX.asset
                string assetPath = AssetDatabase.GetAssetPath(upgrade);
                string fileName = System.IO.Path.GetFileName(assetPath); // e.g. FaceUpgrade_RedOddRune.asset
                string shopProductFileName = "ShopProduct_" + fileName;
                string shopProductPath = ShopFaceUpgradesPath + "/" + shopProductFileName;

                // ShopProduct 로드 또는 생성
                ShopProductDefinitionSO shopProduct = LoadOrCreateShopProduct(shopProductPath);

                // 필드 설정
                ApplyShopProductFields(shopProduct, productId, ShopProductType.DiceFaceUpgrade, upgrade);

                createdOrUpdatedProducts.Add(shopProduct);
            }
        }

        /// <summary>DiceFaceUpgrade SO의 upgradeId 필드를 SerializedObject로 읽는다.</summary>
        private static string ReadUpgradeId(DiceFaceUpgradeDefinitionSO upgrade)
        {
            SerializedObject so = new SerializedObject(upgrade);
            SerializedProperty upgradeIdProp = so.FindProperty("upgradeId");

            if (upgradeIdProp == null)
                return null;

            return upgradeIdProp.stringValue;
        }

        /// <summary>DiceFaceUpgrade SO의 effectType 필드를 SerializedObject로 읽는다.</summary>
        private static int ReadEffectType(DiceFaceUpgradeDefinitionSO upgrade)
        {
            SerializedObject so = new SerializedObject(upgrade);
            SerializedProperty effectTypeProp = so.FindProperty("effectType");

            if (effectTypeProp == null)
                return 0;

            return effectTypeProp.intValue;
        }

        /// <summary>DiceFaceUpgrade SO의 replacementFaceType 필드를 SerializedObject로 읽는다.</summary>
        private static int ReadReplacementFaceType(DiceFaceUpgradeDefinitionSO upgrade)
        {
            SerializedObject so = new SerializedObject(upgrade);
            SerializedProperty replacementFaceTypeProp = so.FindProperty("replacementFaceType");

            if (replacementFaceTypeProp == null)
                return (int)DiceFaceType.Number;

            return replacementFaceTypeProp.intValue;
        }

        /// <summary>현재 런타임에서 구매 적용 가능한 FaceUpgrade인지 확인한다.</summary>
        private static bool IsImplementedFaceUpgrade(DiceFaceUpgradeDefinitionSO upgrade)
        {
            if (upgrade == null)
                return false;

            return ReadEffectType(upgrade) != 0 || ReadReplacementFaceType(upgrade) != (int)DiceFaceType.Number;
        }

        /// <summary>DiceFaceUpgrade SO의 tier 필드를 SerializedObject로 읽는다.</summary>
        private static int ReadFaceUpgradeTier(DiceFaceUpgradeDefinitionSO upgrade)
        {
            SerializedObject so = new SerializedObject(upgrade);
            SerializedProperty tierProp = so.FindProperty("tier");

            if (tierProp == null)
                return 1;

            return Mathf.Max(1, tierProp.intValue);
        }

        /// <summary>지정 경로에 ShopProductDefinitionSO가 있으면 로드하고, 없으면 새로 생성한다.</summary>
        private static ShopProductDefinitionSO LoadOrCreateShopProduct(string assetPath)
        {
            ShopProductDefinitionSO existing = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(assetPath);

            if (existing != null)
            {
                _updatedCount++;
                Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] Updating existing ShopProduct: {assetPath}");
                return existing;
            }

            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] Created new ShopProduct: {assetPath}");
            return asset;
        }

        /// <summary>SerializedObject를 통해 ShopProduct SO 필드를 설정한다. DiceFaceUpgrade 전용 필드만 세팅한다.</summary>
        private static void ApplyShopProductFields(
            ScriptableObject asset,
            string productId,
            ShopProductType productType,
            DiceFaceUpgradeDefinitionSO diceFaceUpgradeDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            // productId
            SerializedProperty productIdProp = so.FindProperty("productId");
            if (productIdProp != null)
                productIdProp.stringValue = productId;
            else
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] Missing field: productId on " + asset.name);
                allOk = false;
            }

            // productType
            SerializedProperty productTypeProp = so.FindProperty("productType");
            if (productTypeProp != null)
                productTypeProp.intValue = (int)productType;
            else
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] Missing field: productType on " + asset.name);
                allOk = false;
            }

            // diceFaceUpgradeDefinition
            SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
            if (faceUpgradeDefProp != null)
                faceUpgradeDefProp.objectReferenceValue = diceFaceUpgradeDefinition;
            else
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] Missing field: diceFaceUpgradeDefinition on " + asset.name);
                allOk = false;
            }

            // deviceDefinition = null
            SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
            if (deviceDefProp != null)
                deviceDefProp.objectReferenceValue = null;

            // diceTypeDefinition = null
            SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
            if (diceTypeDefProp != null)
                diceTypeDefProp.objectReferenceValue = null;

            // consumableDefinitionPlaceholder = null
            SerializedProperty consumableProp = so.FindProperty("consumableDefinitionPlaceholder");
            if (consumableProp != null)
                consumableProp.objectReferenceValue = null;

            // permanentUpgradeDefinitionPlaceholder = null
            SerializedProperty permanentProp = so.FindProperty("permanentUpgradeDefinitionPlaceholder");
            if (permanentProp != null)
                permanentProp.objectReferenceValue = null;

            // hpRepairDefinitionPlaceholder = null
            SerializedProperty hpRepairProp = so.FindProperty("hpRepairDefinitionPlaceholder");
            if (hpRepairProp != null)
                hpRepairProp.objectReferenceValue = null;

            if (!allOk)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] Failed to set some fields on ShopProduct asset: " + AssetDatabase.GetAssetPath(asset));
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] Applied ShopProduct fields: {AssetDatabase.GetAssetPath(asset)} productId={productId}");
        }

        // ── WorkshopRules 연결 ─────────────────────────────────────────

        /// <summary>생성된 DiceFaceUpgrade ShopProduct를 Stage01_WorkshopRules의 FaceUpgrade 슬롯 productPool에 연결한다.</summary>
        private static void ConnectFaceUpgradeShopProductsToWorkshopRules(List<ShopProductDefinitionSO> allFaceUpgradeProducts)
        {
            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] Stage01_WorkshopRules.asset을 찾을 수 없습니다: {WorkshopRulesPath}");
                return;
            }

            SerializedObject so = new SerializedObject(workshopRules);
            so.Update();

            SerializedProperty slotRulesProp = so.FindProperty("productSlotRules");

            if (slotRulesProp == null || !slotRulesProp.isArray)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] productSlotRules를 찾을 수 없습니다.");
                return;
            }

            int slotCount = slotRulesProp.arraySize;

            if (slotCount < 6)
            {
                Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] productSlotRules count = {slotCount}, 기대값 6");
                return;
            }

            // Tier별로 ShopProduct 분류 (구현된 상품만)
            List<ShopProductDefinitionSO> tier1Products = new List<ShopProductDefinitionSO>();
            List<ShopProductDefinitionSO> tier2Products = new List<ShopProductDefinitionSO>();

            for (int i = 0; i < allFaceUpgradeProducts.Count; i++)
            {
                ShopProductDefinitionSO product = allFaceUpgradeProducts[i];

                if (product == null)
                    continue;

                // 연결된 DiceFaceUpgrade의 effectType과 Tier를 읽는다
                DiceFaceUpgradeDefinitionSO faceUpgrade = GetFaceUpgradeFromShopProduct(product);

                if (faceUpgrade == null)
                {
                    Debug.LogWarning($"[ShopProductDiceFaceUpgradeSOV44Generator] ShopProduct {product.name}의 diceFaceUpgradeDefinition이 null입니다. 건너뜁니다.");
                    continue;
                }

                // 구현되지 않은 상품은 제외
                if (!IsImplementedFaceUpgrade(faceUpgrade))
                {
                    Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] [PendingFaceUpgrade] {faceUpgrade.name}: 런타임 적용 경로 없음");
                    continue;
                }

                int tier = ReadFaceUpgradeTier(faceUpgrade);

                if (tier <= 1)
                    tier1Products.Add(product);
                else
                    tier2Products.Add(product);
            }

            // Slot 0: Left Device - 기존 productPool 유지 (변경하지 않음)
            // Slot 1: Right Device - 기존 productPool 유지 (변경하지 않음)

            // Slot 2: Left Dice Type - 비움 유지
            SerializedProperty slot2 = slotRulesProp.GetArrayElementAtIndex(2);
            ClearProductPool(slot2);

            // Slot 3: Right Dice Type - 비움 유지
            SerializedProperty slot3 = slotRulesProp.GetArrayElementAtIndex(3);
            ClearProductPool(slot3);

            // Slot 4: Left Face Upgrade - Tier 1만
            SerializedProperty slot4 = slotRulesProp.GetArrayElementAtIndex(4);
            SetProductPool(slot4, tier1Products.ToArray());

            // Slot 5: Right Face Upgrade - Tier 1 + Tier 2
            List<ShopProductDefinitionSO> rightFaceUpgradePool = new List<ShopProductDefinitionSO>();
            rightFaceUpgradePool.AddRange(tier1Products);
            rightFaceUpgradePool.AddRange(tier2Products);
            SerializedProperty slot5 = slotRulesProp.GetArrayElementAtIndex(5);
            SetProductPool(slot5, rightFaceUpgradePool.ToArray());

            bool applied = so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(workshopRules);

            Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] WorkshopRules 연결 완료: Left Face Upgrade={tier1Products.Count}개, Right Face Upgrade={rightFaceUpgradePool.Count}개");
        }

        /// <summary>ShopProduct에서 diceFaceUpgradeDefinition 참조를 SerializedObject로 읽는다.</summary>
        private static DiceFaceUpgradeDefinitionSO GetFaceUpgradeFromShopProduct(ShopProductDefinitionSO product)
        {
            SerializedObject so = new SerializedObject(product);
            SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");

            if (faceUpgradeDefProp == null)
                return null;

            return faceUpgradeDefProp.objectReferenceValue as DiceFaceUpgradeDefinitionSO;
        }

        /// <summary>슬롯 SerializedProperty의 productPool 배열을 지정 상품 목록으로 설정한다.</summary>
        private static void SetProductPool(SerializedProperty slotProp, ShopProductDefinitionSO[] products)
        {
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");

            if (productPoolProp == null || !productPoolProp.isArray)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] productPool 필드를 찾을 수 없습니다.");
                return;
            }

            productPoolProp.ClearArray();
            productPoolProp.arraySize = products.Length;

            for (int i = 0; i < products.Length; i++)
            {
                SerializedProperty elementProp = productPoolProp.GetArrayElementAtIndex(i);
                elementProp.objectReferenceValue = products[i];
            }
        }

        /// <summary>슬롯 SerializedProperty의 productPool 배열을 비운다.</summary>
        private static void ClearProductPool(SerializedProperty slotProp)
        {
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");

            if (productPoolProp == null || !productPoolProp.isArray)
                return;

            productPoolProp.ClearArray();
            productPoolProp.arraySize = 0;
        }

        // ── 레거시 보고 ─────────────────────────────────────────────────

        /// <summary>레거시 ShopProduct 후보를 Console에 보고한다.</summary>
        private static void ReportLegacyCandidates()
        {
            // v4.4 대상 DiceFaceUpgrade 이름 목록
            HashSet<string> v44FaceUpgradeNames = new HashSet<string>(FaceUpgradeNames);

            // v4.4 target path의 모든 ShopProduct 검색
            string[] shopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO", new[] { ShopFaceUpgradesPath });

            for (int i = 0; i < shopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shopProductGuids[i]);
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(path);

                if (product == null)
                    continue;

                SerializedObject so = new SerializedObject(product);
                SerializedProperty productTypeProp = so.FindProperty("productType");

                if (productTypeProp == null || productTypeProp.intValue != (int)ShopProductType.DiceFaceUpgrade)
                    continue;

                SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
                DiceFaceUpgradeDefinitionSO faceUpgradeRef = faceUpgradeDefProp?.objectReferenceValue as DiceFaceUpgradeDefinitionSO;

                // diceFaceUpgradeDefinition이 null인 경우
                if (faceUpgradeRef == null)
                {
                    Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] [LegacyCandidate] DiceFaceUpgrade ShopProduct with null diceFaceUpgradeDefinition: {path}");
                    continue;
                }

                string faceUpgradeName = faceUpgradeRef.name;

                // v4.4 대상에 없는 FaceUpgrade인 경우
                if (!v44FaceUpgradeNames.Contains(faceUpgradeName))
                {
                    Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] [LegacyCandidate] DiceFaceUpgrade ShopProduct linked to non-v4.4 upgrade '{faceUpgradeName}': {path}");
                    continue;
                }

                // 미구현 DiceFaceUpgrade가 Stage01_WorkshopRules productPool에 들어가 있는지 확인
                if (!IsImplementedFaceUpgrade(faceUpgradeRef))
                {
                    StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);
                    if (workshopRules != null)
                    {
                        System.Collections.Generic.IReadOnlyList<ShopProductSlotRule> slotRules = workshopRules.ProductSlotRules;
                        if (slotRules != null && slotRules.Count >= 6)
                        {
                            for (int slotIdx = 4; slotIdx <= 5; slotIdx++)
                            {
                                ShopProductDefinitionSO[] pool = slotRules[slotIdx].ProductPool;
                                if (pool != null)
                                {
                                    for (int pIdx = 0; pIdx < pool.Length; pIdx++)
                                    {
                                        if (pool[pIdx] == product)
                                        {
                                            Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] [LegacyCandidate] 미구현 FaceUpgrade '{faceUpgradeName}'가 Stage01_WorkshopRules Slot {slotIdx} productPool에 있습니다: {path}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // v4.4 target path 외부의 ShopProduct_FaceUpgrade_* 검색
            string[] allShopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO");
            for (int i = 0; i < allShopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allShopProductGuids[i]);

                // v4.4 target path에 있으면 스킵
                if (path.StartsWith(ShopFaceUpgradesPath))
                    continue;

                string fileName = System.IO.Path.GetFileName(path);
                if (fileName.StartsWith("ShopProduct_FaceUpgrade_"))
                {
                    Debug.Log($"[ShopProductDiceFaceUpgradeSOV44Generator] [LegacyCandidate] DiceFaceUpgrade ShopProduct outside v4.4 target path: {path}");
                }
            }
        }

        // ── 검증 ────────────────────────────────────────────────────────

        /// <summary>생성된 모든 SO와 WorkshopRules 연결 상태를 검증한다.</summary>
        private static void ValidateGeneratedAssets()
        {
            bool shopProductValid = ValidateShopProducts();
            bool workshopValid = ValidateWorkshopRules();

            if (shopProductValid)
                Debug.Log("[ShopProductDiceFaceUpgradeSOV44Generator] DiceFaceUpgrade ShopProduct 검증 통과");

            if (workshopValid)
                Debug.Log("[ShopProductDiceFaceUpgradeSOV44Generator] WorkshopRules FaceUpgrade productPool 검증 통과");
        }

        /// <summary>DiceFaceUpgrade ShopProduct 11종의 필드를 검증한다.</summary>
        private static bool ValidateShopProducts()
        {
            bool allPassed = true;

            foreach (string faceUpgradeName in FaceUpgradeNames)
            {
                string faceUpgradePath = DiceFaceUpgradesSourcePath + "/" + faceUpgradeName + ".asset";
                DiceFaceUpgradeDefinitionSO faceUpgrade = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(faceUpgradePath);

                if (faceUpgrade == null)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: DiceFaceUpgrade SO를 찾을 수 없습니다: {faceUpgradePath}");
                    allPassed = false;
                    continue;
                }

                // ShopProduct 경로 계산
                string shopProductFileName = "ShopProduct_" + faceUpgradeName + ".asset";
                string shopProductPath = ShopFaceUpgradesPath + "/" + shopProductFileName;

                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(shopProductPath);

                if (shopProduct == null)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: ShopProduct를 찾을 수 없습니다: {shopProductPath}");
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(shopProduct);

                // productType 검증
                SerializedProperty productTypeProp = so.FindProperty("productType");
                if (productTypeProp == null || productTypeProp.intValue != (int)ShopProductType.DiceFaceUpgrade)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: {shopProductPath} productType = {productTypeProp?.intValue}, 기대값 {(int)ShopProductType.DiceFaceUpgrade}");
                    allPassed = false;
                }

                // diceFaceUpgradeDefinition 검증
                SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
                if (faceUpgradeDefProp == null || faceUpgradeDefProp.objectReferenceValue == null)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: {shopProductPath} diceFaceUpgradeDefinition이 null입니다.");
                    allPassed = false;
                }

                // productId 검증
                SerializedProperty productIdProp = so.FindProperty("productId");
                if (productIdProp == null || string.IsNullOrWhiteSpace(productIdProp.stringValue))
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: {shopProductPath} productId가 비어 있습니다.");
                    allPassed = false;
                }

                // deviceDefinition은 null이어야 함
                SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
                if (deviceDefProp != null && deviceDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: {shopProductPath} deviceDefinition이 null이 아닙니다.");
                    allPassed = false;
                }

                // diceTypeDefinition은 null이어야 함
                SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
                if (diceTypeDefProp != null && diceTypeDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: {shopProductPath} diceTypeDefinition이 null이 아닙니다.");
                    allPassed = false;
                }
            }

            return allPassed;
        }

        /// <summary>Stage01_WorkshopRules의 FaceUpgrade productPool 연결 상태를 검증한다.</summary>
        private static bool ValidateWorkshopRules()
        {
            bool allPassed = true;

            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Stage01_WorkshopRules.asset이 존재하지 않습니다: {WorkshopRulesPath}");
                return false;
            }

            System.Collections.Generic.IReadOnlyList<ShopProductSlotRule> slotRules = workshopRules.ProductSlotRules;

            if (slotRules == null || slotRules.Count != 6)
            {
                Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: ProductSlotRules count = {(slotRules != null ? slotRules.Count : 0)}, 기대값 6");
                return false;
            }

            // Slot 0/1: Device productPool은 기존 값 유지 (null이 아니고 비어있지 않으면 통과)
            for (int i = 0; i <= 1; i++)
            {
                ShopProductDefinitionSO[] pool = slotRules[i].ProductPool;
                if (pool == null || pool.Length == 0)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot {i} Device productPool이 비어 있습니다. (기존 값 유지 필요)");
                    allPassed = false;
                }
                else
                {
                    for (int j = 0; j < pool.Length; j++)
                    {
                        if (pool[j] == null)
                        {
                            Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot {i} productPool[{j}]가 null입니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            // Slot 2/3: DiceType productPool count == 0
            for (int i = 2; i <= 3; i++)
            {
                ShopProductDefinitionSO[] pool = slotRules[i].ProductPool;
                int poolCount = (pool != null) ? pool.Length : -1;

                if (poolCount != 0)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot {i} DiceType productPool count = {poolCount}, 기대값 0");
                    allPassed = false;
                }
            }

            // 구현된 DiceFaceUpgrade 목록 수집 (수치 효과 또는 특수 replacementFaceType)
            List<ShopProductDefinitionSO> implementedTier1Products = new List<ShopProductDefinitionSO>();
            List<ShopProductDefinitionSO> implementedTier2Products = new List<ShopProductDefinitionSO>();

            foreach (string faceUpgradeName in FaceUpgradeNames)
            {
                string faceUpgradePath = DiceFaceUpgradesSourcePath + "/" + faceUpgradeName + ".asset";
                DiceFaceUpgradeDefinitionSO faceUpgrade = AssetDatabase.LoadAssetAtPath<DiceFaceUpgradeDefinitionSO>(faceUpgradePath);

                if (faceUpgrade == null)
                    continue;

                if (!IsImplementedFaceUpgrade(faceUpgrade))
                    continue;

                string shopProductFileName = "ShopProduct_" + faceUpgradeName + ".asset";
                string shopProductPath = ShopFaceUpgradesPath + "/" + shopProductFileName;
                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(shopProductPath);

                if (shopProduct == null)
                    continue;

                int tier = ReadFaceUpgradeTier(faceUpgrade);
                if (tier <= 1)
                    implementedTier1Products.Add(shopProduct);
                else
                    implementedTier2Products.Add(shopProduct);
            }

            // Slot 4: Left Face Upgrade - Tier 1 + 구현된 상품만
            ShopProductDefinitionSO[] slot4Pool = slotRules[4].ProductPool;
            int expectedSlot4Count = implementedTier1Products.Count;

            if (slot4Pool == null)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 4 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot4Pool.Length != expectedSlot4Count)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 4 productPool count = {slot4Pool.Length}, 기대값 {expectedSlot4Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot4Pool.Length; i++)
                {
                    if (slot4Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 4 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }

                // 미구현 상품이 Slot 4에 없는지 확인
                for (int i = 0; i < slot4Pool.Length; i++)
                {
                    if (slot4Pool[i] == null)
                        continue;

                    DiceFaceUpgradeDefinitionSO linkedUpgrade = GetFaceUpgradeFromShopProduct(slot4Pool[i]);
                    if (linkedUpgrade != null)
                    {
                        if (!IsImplementedFaceUpgrade(linkedUpgrade))
                        {
                            Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 4 productPool[{i}]에 미구현 상품 '{linkedUpgrade.name}'이 있습니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            // Slot 5: Right Face Upgrade - Tier 1~2 + 구현된 상품만
            ShopProductDefinitionSO[] slot5Pool = slotRules[5].ProductPool;
            int expectedSlot5Count = implementedTier1Products.Count + implementedTier2Products.Count;

            if (slot5Pool == null)
            {
                Debug.LogError("[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 5 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot5Pool.Length != expectedSlot5Count)
                {
                    Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 5 productPool count = {slot5Pool.Length}, 기대값 {expectedSlot5Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot5Pool.Length; i++)
                {
                    if (slot5Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 5 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }

                // 미구현 상품이 Slot 5에 없는지 확인
                for (int i = 0; i < slot5Pool.Length; i++)
                {
                    if (slot5Pool[i] == null)
                        continue;

                    DiceFaceUpgradeDefinitionSO linkedUpgrade = GetFaceUpgradeFromShopProduct(slot5Pool[i]);
                    if (linkedUpgrade != null)
                    {
                        if (!IsImplementedFaceUpgrade(linkedUpgrade))
                        {
                            Debug.LogError($"[ShopProductDiceFaceUpgradeSOV44Generator] 검증 실패: Slot 5 productPool[{i}]에 미구현 상품 '{linkedUpgrade.name}'이 있습니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            return allPassed;
        }
    }
}
