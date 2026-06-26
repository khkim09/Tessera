using System.Collections.Generic;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 DiceType ShopProduct SO를 생성/수정하고 Stage01_WorkshopRules의 DiceType 슬롯 productPool을 연결하는 Editor 유틸리티다.</summary>
    public static class ShopProductDiceTypeSOV44Generator
    {
        // ── 폴더 경로 상수 ──────────────────────────────────────────────
        private const string RootPath = "Assets/_Project/ScriptableObjects";

        private const string DiceTypesSourcePath = RootPath + "/DiceTypes";
        private const string ShopDiceTypesPath = RootPath + "/Shop/Generated/DiceTypes";

        private const string StagesFolderPath = "Assets/_Project/ScriptableObjects/Stages/Stage01";
        private const string WorkshopRulesPath = StagesFolderPath + "/Stage01_WorkshopRules.asset";

        // ── v4.4 대상 DiceType SO 이름 목록 ─────────────────────────────
        private static readonly string[] DiceTypeNames = new string[]
        {
            "DiceType_Standard",
            "DiceType_Red",
            "DiceType_Blue",
            "DiceType_Green",
            "DiceType_Iron",
            "DiceType_Gold",
            "DiceType_Lucky",
            "DiceType_Void"
        };

        // ── 카운터 ──────────────────────────────────────────────────────
        private static int _createdCount;
        private static int _updatedCount;
        private static int _skippedCount;

        // ── 메뉴 엔트리 포인트 ──────────────────────────────────────────

        /// <summary>Tools/Tessera/Assets/Generate ShopProduct DiceType SO v4.4 메뉴 항목이다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate ShopProduct DiceType SO v4.4")]
        private static void Generate()
        {
            _createdCount = 0;
            _updatedCount = 0;
            _skippedCount = 0;

            // 1. 폴더 생성
            EnsureFolder(RootPath + "/Shop", "Generated");
            EnsureFolder(RootPath + "/Shop/Generated", "DiceTypes");

            // 2. DiceType SO 8종 로드
            DiceTypeDefinitionSO[] diceTypes = LoadDiceTypes();

            if (diceTypes.Length == 0)
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] 로드된 DiceTypeDefinitionSO가 없습니다. 생성을 중단합니다.");
                return;
            }

            // 3. DiceType ShopProduct 8종 생성/수정
            List<ShopProductDefinitionSO> createdOrUpdatedProducts = new List<ShopProductDefinitionSO>();
            CreateOrUpdateDiceTypeShopProducts(diceTypes, createdOrUpdatedProducts);

            // 4. Stage01_WorkshopRules DiceType productPool 연결
            ConnectDiceTypeShopProductsToWorkshopRules(createdOrUpdatedProducts);

            // 5. 저장 및 리프레시
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. 레거시 ShopProduct 보고
            ReportLegacyCandidates();

            // 7. 검증
            ValidateGeneratedAssets();

            // 8. 결과 출력
            Debug.Log($"[ShopProductDiceTypeSOV44Generator] Complete. Created: {_createdCount}, Updated: {_updatedCount}, Skipped: {_skippedCount}");
        }

        // ── 폴더 생성 ──────────────────────────────────────────────────

        /// <summary>부모 폴더 아래에 새 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[ShopProductDiceTypeSOV44Generator] Created folder: {path}");
            }
        }

        // ── DiceType SO 로드 ───────────────────────────────────────────

        /// <summary>v4.4 대상 DiceTypeDefinitionSO 8종을 로드한다.</summary>
        private static DiceTypeDefinitionSO[] LoadDiceTypes()
        {
            List<DiceTypeDefinitionSO> diceTypes = new List<DiceTypeDefinitionSO>();

            for (int i = 0; i < DiceTypeNames.Length; i++)
            {
                string name = DiceTypeNames[i];
                string assetPath = DiceTypesSourcePath + "/" + name + ".asset";
                DiceTypeDefinitionSO diceType = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(assetPath);

                if (diceType == null)
                {
                    Debug.LogWarning($"[ShopProductDiceTypeSOV44Generator] DiceTypeDefinitionSO not found (skipped): {assetPath}");
                    _skippedCount++;
                    continue;
                }

                diceTypes.Add(diceType);
            }

            return diceTypes.ToArray();
        }

        // ── DiceType ShopProduct 생성/수정 ─────────────────────────────

        /// <summary>DiceType 배열에 대해 ShopProduct를 생성/수정하고 목록에 추가한다.</summary>
        private static void CreateOrUpdateDiceTypeShopProducts(
            DiceTypeDefinitionSO[] diceTypes,
            List<ShopProductDefinitionSO> createdOrUpdatedProducts)
        {
            for (int i = 0; i < diceTypes.Length; i++)
            {
                DiceTypeDefinitionSO diceType = diceTypes[i];

                if (diceType == null)
                    continue;

                // diceTypeId를 SerializedObject로 읽는다
                string diceTypeId = ReadDiceTypeId(diceType);

                if (string.IsNullOrWhiteSpace(diceTypeId))
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] diceTypeId를 읽을 수 없습니다: {AssetDatabase.GetAssetPath(diceType)}");
                    _skippedCount++;
                    continue;
                }

                // productId 생성: "shop." + diceTypeId
                string productId = "shop." + diceTypeId;

                // 파일명 생성: DiceType_XXX.asset → ShopProduct_DiceType_XXX.asset
                string assetPath = AssetDatabase.GetAssetPath(diceType);
                string fileName = System.IO.Path.GetFileName(assetPath); // e.g. DiceType_Red.asset
                string shopProductFileName = "ShopProduct_" + fileName;
                string shopProductPath = ShopDiceTypesPath + "/" + shopProductFileName;

                // ShopProduct 로드 또는 생성
                ShopProductDefinitionSO shopProduct = LoadOrCreateShopProduct(shopProductPath);

                // 필드 설정
                ApplyShopProductFields(shopProduct, productId, ShopProductType.DiceSet, diceType);

                createdOrUpdatedProducts.Add(shopProduct);
            }
        }

        /// <summary>DiceType SO의 diceTypeId 필드를 SerializedObject로 읽는다.</summary>
        private static string ReadDiceTypeId(DiceTypeDefinitionSO diceType)
        {
            SerializedObject so = new SerializedObject(diceType);
            SerializedProperty diceTypeIdProp = so.FindProperty("diceTypeId");

            if (diceTypeIdProp == null)
                return null;

            return diceTypeIdProp.stringValue;
        }

        /// <summary>DiceType SO의 intrinsicEffectType 필드를 SerializedObject로 읽는다.</summary>
        private static int ReadIntrinsicEffectType(DiceTypeDefinitionSO diceType)
        {
            SerializedObject so = new SerializedObject(diceType);
            SerializedProperty effectTypeProp = so.FindProperty("intrinsicEffectType");

            if (effectTypeProp == null)
                return 0;

            return effectTypeProp.intValue;
        }

        /// <summary>지정 경로에 ShopProductDefinitionSO가 있으면 로드하고, 없으면 새로 생성한다.</summary>
        private static ShopProductDefinitionSO LoadOrCreateShopProduct(string assetPath)
        {
            ShopProductDefinitionSO existing = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(assetPath);

            if (existing != null)
            {
                _updatedCount++;
                Debug.Log($"[ShopProductDiceTypeSOV44Generator] Updating existing ShopProduct: {assetPath}");
                return existing;
            }

            ShopProductDefinitionSO asset = ScriptableObject.CreateInstance<ShopProductDefinitionSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            _createdCount++;
            Debug.Log($"[ShopProductDiceTypeSOV44Generator] Created new ShopProduct: {assetPath}");
            return asset;
        }

        /// <summary>SerializedObject를 통해 ShopProduct SO 필드를 설정한다. DiceType 전용 필드만 세팅한다.</summary>
        private static void ApplyShopProductFields(
            ScriptableObject asset,
            string productId,
            ShopProductType productType,
            DiceTypeDefinitionSO diceTypeDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            bool allOk = true;

            // productId
            SerializedProperty productIdProp = so.FindProperty("productId");
            if (productIdProp != null)
                productIdProp.stringValue = productId;
            else
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] Missing field: productId on " + asset.name);
                allOk = false;
            }

            // productType
            SerializedProperty productTypeProp = so.FindProperty("productType");
            if (productTypeProp != null)
                productTypeProp.intValue = (int)productType;
            else
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] Missing field: productType on " + asset.name);
                allOk = false;
            }

            // diceTypeDefinition
            SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
            if (diceTypeDefProp != null)
                diceTypeDefProp.objectReferenceValue = diceTypeDefinition;
            else
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] Missing field: diceTypeDefinition on " + asset.name);
                allOk = false;
            }

            // deviceDefinition = null
            SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
            if (deviceDefProp != null)
                deviceDefProp.objectReferenceValue = null;

            // diceFaceUpgradeDefinition = null
            SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
            if (faceUpgradeDefProp != null)
                faceUpgradeDefProp.objectReferenceValue = null;

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
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] Failed to set some fields on ShopProduct asset: " + AssetDatabase.GetAssetPath(asset));
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[ShopProductDiceTypeSOV44Generator] Applied ShopProduct fields: {AssetDatabase.GetAssetPath(asset)} productId={productId}");
        }

        // ── WorkshopRules 연결 ─────────────────────────────────────────

        /// <summary>생성된 DiceType ShopProduct를 Stage01_WorkshopRules의 DiceType 슬롯 productPool에 연결한다.</summary>
        private static void ConnectDiceTypeShopProductsToWorkshopRules(List<ShopProductDefinitionSO> allDiceTypeProducts)
        {
            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDiceTypeSOV44Generator] Stage01_WorkshopRules.asset을 찾을 수 없습니다: {WorkshopRulesPath}");
                return;
            }

            SerializedObject so = new SerializedObject(workshopRules);
            so.Update();

            SerializedProperty slotRulesProp = so.FindProperty("productSlotRules");

            if (slotRulesProp == null || !slotRulesProp.isArray)
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] productSlotRules를 찾을 수 없습니다.");
                return;
            }

            int slotCount = slotRulesProp.arraySize;

            if (slotCount < 6)
            {
                Debug.LogError($"[ShopProductDiceTypeSOV44Generator] productSlotRules count = {slotCount}, 기대값 6");
                return;
            }

            // DiceType별로 effectType 확인하여 구현 가능 상품과 보류 상품 분류
            List<ShopProductDefinitionSO> implementedProducts = new List<ShopProductDefinitionSO>();
            List<ShopProductDefinitionSO> pendingProducts = new List<ShopProductDefinitionSO>();

            for (int i = 0; i < allDiceTypeProducts.Count; i++)
            {
                ShopProductDefinitionSO product = allDiceTypeProducts[i];

                if (product == null)
                    continue;

                // 연결된 DiceTypeDefinition의 effectType을 읽는다
                DiceTypeDefinitionSO diceType = GetDiceTypeFromShopProduct(product);

                if (diceType == null)
                {
                    Debug.LogWarning($"[ShopProductDiceTypeSOV44Generator] ShopProduct {product.name}의 diceTypeDefinition이 null입니다. 건너뜁니다.");
                    continue;
                }

                // effectType None 상품은 productPool에서 제외
                int effectType = ReadIntrinsicEffectType(diceType);
                if (effectType == 0)
                {
                    Debug.Log($"[ShopProductDiceTypeSOV44Generator] [PendingDiceType] {diceType.name} effectType=None, productPool 제외");
                    pendingProducts.Add(product);
                    continue;
                }

                // effectType이 None이 아니지만 런타임 효과 적용이 아직 완전히 연결되지 않은 경우 경고
                Debug.Log($"[ShopProductDiceTypeSOV44Generator] [PendingDiceTypeRuntime] {diceType.name} effectType={(DiceIntrinsicEffectType)effectType}, 런타임 계산 연결 후 검증 필요");

                implementedProducts.Add(product);
            }

            // Slot 0: Left Device - 기존 productPool 유지 (변경하지 않음)
            // Slot 1: Right Device - 기존 productPool 유지 (변경하지 않음)

            // Slot 2: Left Dice Type - 구현 가능 DiceType만
            SerializedProperty slot2 = slotRulesProp.GetArrayElementAtIndex(2);
            SetProductPool(slot2, implementedProducts.ToArray());

            // Slot 3: Right Dice Type - 구현 가능 DiceType만
            SerializedProperty slot3 = slotRulesProp.GetArrayElementAtIndex(3);
            SetProductPool(slot3, implementedProducts.ToArray());

            // Slot 4: Left Face Upgrade - 기존 productPool 유지 (변경하지 않음)
            // Slot 5: Right Face Upgrade - 기존 productPool 유지 (변경하지 않음)

            bool applied = so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(workshopRules);

            Debug.Log($"[ShopProductDiceTypeSOV44Generator] WorkshopRules 연결 완료: Left Dice Type={implementedProducts.Count}개, Right Dice Type={implementedProducts.Count}개");
        }

        /// <summary>ShopProduct에서 diceTypeDefinition 참조를 SerializedObject로 읽는다.</summary>
        private static DiceTypeDefinitionSO GetDiceTypeFromShopProduct(ShopProductDefinitionSO product)
        {
            SerializedObject so = new SerializedObject(product);
            SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");

            if (diceTypeDefProp == null)
                return null;

            return diceTypeDefProp.objectReferenceValue as DiceTypeDefinitionSO;
        }

        /// <summary>슬롯 SerializedProperty의 productPool 배열을 지정 상품 목록으로 설정한다.</summary>
        private static void SetProductPool(SerializedProperty slotProp, ShopProductDefinitionSO[] products)
        {
            SerializedProperty productPoolProp = slotProp.FindPropertyRelative("productPool");

            if (productPoolProp == null || !productPoolProp.isArray)
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] productPool 필드를 찾을 수 없습니다.");
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

        // ── 레거시 보고 ─────────────────────────────────────────────────

        /// <summary>레거시 ShopProduct 후보를 Console에 보고한다.</summary>
        private static void ReportLegacyCandidates()
        {
            // v4.4 대상 DiceType 이름 목록
            HashSet<string> v44DiceTypeNames = new HashSet<string>(DiceTypeNames);

            // v4.4 target path의 모든 ShopProduct 검색
            string[] shopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO", new[] { ShopDiceTypesPath });

            for (int i = 0; i < shopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shopProductGuids[i]);
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(path);

                if (product == null)
                    continue;

                SerializedObject so = new SerializedObject(product);
                SerializedProperty productTypeProp = so.FindProperty("productType");

                if (productTypeProp == null)
                    continue;

                // DiceSet / SingleDice / DiceTypeUpgrade 계열만 확인
                int productTypeValue = productTypeProp.intValue;
                if (productTypeValue != (int)ShopProductType.DiceSet &&
                    productTypeValue != (int)ShopProductType.SingleDice &&
                    productTypeValue != (int)ShopProductType.DiceTypeUpgrade)
                {
                    continue;
                }

                SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
                DiceTypeDefinitionSO diceTypeRef = diceTypeDefProp?.objectReferenceValue as DiceTypeDefinitionSO;

                // diceTypeDefinition이 null인 경우
                if (diceTypeRef == null)
                {
                    Debug.Log($"[ShopProductDiceTypeSOV44Generator] [LegacyCandidate] DiceType ShopProduct with null diceTypeDefinition: {path}");
                    continue;
                }

                string diceTypeName = diceTypeRef.name;

                // v4.4 대상에 없는 DiceType인 경우
                if (!v44DiceTypeNames.Contains(diceTypeName))
                {
                    Debug.Log($"[ShopProductDiceTypeSOV44Generator] [LegacyCandidate] DiceType ShopProduct linked to non-v4.4 dice type '{diceTypeName}': {path}");
                    continue;
                }
            }

            // v4.4 target path 외부의 ShopProduct_DiceType_* 검색
            string[] allShopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO");
            for (int i = 0; i < allShopProductGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allShopProductGuids[i]);

                // v4.4 target path에 있으면 스킵
                if (path.StartsWith(ShopDiceTypesPath))
                    continue;

                string fileName = System.IO.Path.GetFileName(path);
                if (fileName.StartsWith("ShopProduct_DiceType_"))
                {
                    Debug.Log($"[ShopProductDiceTypeSOV44Generator] [LegacyCandidate] DiceType ShopProduct outside v4.4 target path: {path}");
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
                Debug.Log("[ShopProductDiceTypeSOV44Generator] DiceType ShopProduct 검증 통과");

            if (workshopValid)
                Debug.Log("[ShopProductDiceTypeSOV44Generator] WorkshopRules DiceType productPool 검증 통과");
        }

        /// <summary>DiceType ShopProduct 8종의 필드를 검증한다.</summary>
        private static bool ValidateShopProducts()
        {
            bool allPassed = true;

            foreach (string diceTypeName in DiceTypeNames)
            {
                string diceTypePath = DiceTypesSourcePath + "/" + diceTypeName + ".asset";
                DiceTypeDefinitionSO diceType = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(diceTypePath);

                if (diceType == null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: DiceTypeDefinitionSO를 찾을 수 없습니다: {diceTypePath}");
                    allPassed = false;
                    continue;
                }

                // ShopProduct 경로 계산
                string shopProductFileName = "ShopProduct_" + diceTypeName + ".asset";
                string shopProductPath = ShopDiceTypesPath + "/" + shopProductFileName;

                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(shopProductPath);

                if (shopProduct == null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: ShopProduct를 찾을 수 없습니다: {shopProductPath}");
                    allPassed = false;
                    continue;
                }

                SerializedObject so = new SerializedObject(shopProduct);

                // productType 검증 (DiceSet 계열이어야 함)
                SerializedProperty productTypeProp = so.FindProperty("productType");
                if (productTypeProp == null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} productType 필드 없음");
                    allPassed = false;
                }
                else
                {
                    int productTypeValue = productTypeProp.intValue;
                    if (productTypeValue != (int)ShopProductType.DiceSet &&
                        productTypeValue != (int)ShopProductType.SingleDice &&
                        productTypeValue != (int)ShopProductType.DiceTypeUpgrade)
                    {
                        Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} productType = {productTypeValue}, 기대값 DiceSet 계열");
                        allPassed = false;
                    }
                }

                // diceTypeDefinition 검증
                SerializedProperty diceTypeDefProp = so.FindProperty("diceTypeDefinition");
                if (diceTypeDefProp == null || diceTypeDefProp.objectReferenceValue == null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} diceTypeDefinition이 null입니다.");
                    allPassed = false;
                }

                // productId 검증
                SerializedProperty productIdProp = so.FindProperty("productId");
                if (productIdProp == null || string.IsNullOrWhiteSpace(productIdProp.stringValue))
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} productId가 비어 있습니다.");
                    allPassed = false;
                }

                // deviceDefinition은 null이어야 함
                SerializedProperty deviceDefProp = so.FindProperty("deviceDefinition");
                if (deviceDefProp != null && deviceDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} deviceDefinition이 null이 아닙니다.");
                    allPassed = false;
                }

                // diceFaceUpgradeDefinition은 null이어야 함
                SerializedProperty faceUpgradeDefProp = so.FindProperty("diceFaceUpgradeDefinition");
                if (faceUpgradeDefProp != null && faceUpgradeDefProp.objectReferenceValue != null)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: {shopProductPath} diceFaceUpgradeDefinition이 null이 아닙니다.");
                    allPassed = false;
                }
            }

            return allPassed;
        }

        /// <summary>Stage01_WorkshopRules의 DiceType productPool 연결 상태를 검증한다.</summary>
        private static bool ValidateWorkshopRules()
        {
            bool allPassed = true;

            StageWorkshopRulesSO workshopRules = AssetDatabase.LoadAssetAtPath<StageWorkshopRulesSO>(WorkshopRulesPath);

            if (workshopRules == null)
            {
                Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Stage01_WorkshopRules.asset이 존재하지 않습니다: {WorkshopRulesPath}");
                return false;
            }

            System.Collections.Generic.IReadOnlyList<ShopProductSlotRule> slotRules = workshopRules.ProductSlotRules;

            if (slotRules == null || slotRules.Count != 6)
            {
                Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: ProductSlotRules count = {(slotRules != null ? slotRules.Count : 0)}, 기대값 6");
                return false;
            }

            // Slot 0/1: Device productPool은 기존 값 유지 (null이 아니고 비어있지 않으면 통과)
            for (int i = 0; i <= 1; i++)
            {
                ShopProductDefinitionSO[] pool = slotRules[i].ProductPool;
                if (pool == null || pool.Length == 0)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot {i} Device productPool이 비어 있습니다. (기존 값 유지 필요)");
                    allPassed = false;
                }
                else
                {
                    for (int j = 0; j < pool.Length; j++)
                    {
                        if (pool[j] == null)
                        {
                            Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot {i} productPool[{j}]가 null입니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            // 구현 가능 DiceType 목록 수집 (effectType != None)
            List<ShopProductDefinitionSO> implementedProducts = new List<ShopProductDefinitionSO>();

            foreach (string diceTypeName in DiceTypeNames)
            {
                string diceTypePath = DiceTypesSourcePath + "/" + diceTypeName + ".asset";
                DiceTypeDefinitionSO diceType = AssetDatabase.LoadAssetAtPath<DiceTypeDefinitionSO>(diceTypePath);

                if (diceType == null)
                    continue;

                int effectType = ReadIntrinsicEffectType(diceType);
                if (effectType == 0)
                    continue;

                string shopProductFileName = "ShopProduct_" + diceTypeName + ".asset";
                string shopProductPath = ShopDiceTypesPath + "/" + shopProductFileName;
                ShopProductDefinitionSO shopProduct = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(shopProductPath);

                if (shopProduct == null)
                    continue;

                implementedProducts.Add(shopProduct);
            }

            // Slot 2: Left Dice Type - 구현 가능 DiceType만
            ShopProductDefinitionSO[] slot2Pool = slotRules[2].ProductPool;
            int expectedSlot2Count = implementedProducts.Count;

            if (slot2Pool == null)
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 2 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot2Pool.Length != expectedSlot2Count)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 2 productPool count = {slot2Pool.Length}, 기대값 {expectedSlot2Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot2Pool.Length; i++)
                {
                    if (slot2Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 2 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }

                // effectType None 상품이 Slot 2에 없는지 확인
                for (int i = 0; i < slot2Pool.Length; i++)
                {
                    if (slot2Pool[i] == null)
                        continue;

                    DiceTypeDefinitionSO linkedDiceType = GetDiceTypeFromShopProduct(slot2Pool[i]);
                    if (linkedDiceType != null)
                    {
                        int effectType = ReadIntrinsicEffectType(linkedDiceType);
                        if (effectType == 0)
                        {
                            Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 2 productPool[{i}]에 effectType None 상품 '{linkedDiceType.name}'이 있습니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            // Slot 3: Right Dice Type - 구현 가능 DiceType만
            ShopProductDefinitionSO[] slot3Pool = slotRules[3].ProductPool;
            int expectedSlot3Count = implementedProducts.Count;

            if (slot3Pool == null)
            {
                Debug.LogError("[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 3 productPool이 null입니다.");
                allPassed = false;
            }
            else
            {
                if (slot3Pool.Length != expectedSlot3Count)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 3 productPool count = {slot3Pool.Length}, 기대값 {expectedSlot3Count}");
                    allPassed = false;
                }

                for (int i = 0; i < slot3Pool.Length; i++)
                {
                    if (slot3Pool[i] == null)
                    {
                        Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 3 productPool[{i}]가 null입니다.");
                        allPassed = false;
                    }
                }

                // effectType None 상품이 Slot 3에 없는지 확인
                for (int i = 0; i < slot3Pool.Length; i++)
                {
                    if (slot3Pool[i] == null)
                        continue;

                    DiceTypeDefinitionSO linkedDiceType = GetDiceTypeFromShopProduct(slot3Pool[i]);
                    if (linkedDiceType != null)
                    {
                        int effectType = ReadIntrinsicEffectType(linkedDiceType);
                        if (effectType == 0)
                        {
                            Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot 3 productPool[{i}]에 effectType None 상품 '{linkedDiceType.name}'이 있습니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            // Slot 4/5: FaceUpgrade productPool은 기존 값 유지 (null이 아니고 비어있지 않으면 통과)
            for (int i = 4; i <= 5; i++)
            {
                ShopProductDefinitionSO[] pool = slotRules[i].ProductPool;
                if (pool == null || pool.Length == 0)
                {
                    Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot {i} FaceUpgrade productPool이 비어 있습니다. (기존 값 유지 필요)");
                    allPassed = false;
                }
                else
                {
                    for (int j = 0; j < pool.Length; j++)
                    {
                        if (pool[j] == null)
                        {
                            Debug.LogError($"[ShopProductDiceTypeSOV44Generator] 검증 실패: Slot {i} productPool[{j}]가 null입니다.");
                            allPassed = false;
                        }
                    }
                }
            }

            return allPassed;
        }
    }
}
