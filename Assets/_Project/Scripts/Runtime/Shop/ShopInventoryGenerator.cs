using System.Collections.Generic;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>Stage Workshop 규칙과 현재 Workshop Tier를 기준으로 Shop 상품 목록을 생성한다.</summary>
    public static class ShopInventoryGenerator
    {
        /// <summary>Shop 상품 슬롯 목록을 생성한다.</summary>
        public static List<ShopInventorySlot> Generate(
            StageWorkshopRulesSO rules,
            int currentWorkshopTier,
            int seed)
        {
            List<ShopInventorySlot> result = new List<ShopInventorySlot>();

            if (rules == null)
                return result;

            System.Random random = new System.Random(seed);

            if (HasSlotRules(rules))
                GenerateBySlotRules(rules, currentWorkshopTier, random, result);
            else
                GenerateFromSharedPool(rules, currentWorkshopTier, random, result);

            return result;
        }

        /// <summary>슬롯별 Shop 상품 추출 규칙이 있는지 확인한다.</summary>
        private static bool HasSlotRules(StageWorkshopRulesSO rules)
        {
            if (rules == null || rules.ProductSlotRules == null)
                return false;

            return rules.ProductSlotRules.Count > 0;
        }

        /// <summary>슬롯별 규칙을 사용해 Shop 상품을 생성한다.</summary>
        private static void GenerateBySlotRules(
            StageWorkshopRulesSO rules,
            int currentWorkshopTier,
            System.Random random,
            List<ShopInventorySlot> result)
        {
            IReadOnlyList<ShopProductSlotRule> slotRules = rules.ProductSlotRules;
            List<ShopProductDefinitionSO> alreadySelectedProducts = new List<ShopProductDefinitionSO>();

            for (int slotIndex = 0; slotIndex < slotRules.Count; slotIndex++)
            {
                ShopProductSlotRule slotRule = slotRules[slotIndex];
                List<ShopProductDefinitionSO> candidates = BuildCandidateListForSlot(
                    rules,
                    slotRule,
                    currentWorkshopTier,
                    alreadySelectedProducts);

                if (candidates.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Tessera][ShopInventory] No candidates for slot {slotIndex} / rule '{slotRule?.DisplayName}'.");
                    continue;
                }

                int selectedIndex = random.Next(0, candidates.Count);
                ShopProductDefinitionSO selectedProduct = candidates[selectedIndex];

                result.Add(
                    new ShopInventorySlot(
                        slotIndex,
                        selectedProduct,
                        selectedProduct.BaseMoneyPrice,
                        selectedProduct.BaseOverchargePrice));

                if (!rules.AllowDuplicateProducts)
                    alreadySelectedProducts.Add(selectedProduct);
            }
        }

        /// <summary>기존 공통 ProductPool 방식으로 Shop 상품을 생성한다.</summary>
        private static void GenerateFromSharedPool(
            StageWorkshopRulesSO rules,
            int currentWorkshopTier,
            System.Random random,
            List<ShopInventorySlot> result)
        {
            List<ShopProductDefinitionSO> candidates = BuildCandidateListFromPool(
                rules.ProductPool,
                rules.MinShopProductTier,
                rules.ResolveAllowedProductMaxTier(currentWorkshopTier),
                null,
                null);

            int slotCount = rules.ProductSlotCount;

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (candidates.Count == 0)
                    break;

                int selectedIndex = random.Next(0, candidates.Count);
                ShopProductDefinitionSO selectedProduct = candidates[selectedIndex];

                result.Add(
                    new ShopInventorySlot(
                        slotIndex,
                        selectedProduct,
                        selectedProduct.BaseMoneyPrice,
                        selectedProduct.BaseOverchargePrice));

                if (!rules.AllowDuplicateProducts)
                    candidates.RemoveAt(selectedIndex);
            }
        }

        /// <summary>특정 SlotRule 기준 후보 상품 목록을 만든다.</summary>
        private static List<ShopProductDefinitionSO> BuildCandidateListForSlot(
            StageWorkshopRulesSO rules,
            ShopProductSlotRule slotRule,
            int currentWorkshopTier,
            List<ShopProductDefinitionSO> alreadySelectedProducts)
        {
            IReadOnlyList<ShopProductDefinitionSO> sourcePool =
                slotRule != null && slotRule.ProductPool != null && slotRule.ProductPool.Length > 0
                    ? slotRule.ProductPool
                    : rules.ProductPool;

            int minTier = slotRule != null && slotRule.MinTierOverride > 0
                ? slotRule.MinTierOverride
                : rules.MinShopProductTier;

            int maxTier = slotRule != null && slotRule.MaxTierOverride > 0
                ? slotRule.MaxTierOverride
                : rules.ResolveAllowedProductMaxTier(currentWorkshopTier);

            return BuildCandidateListFromPool(
                sourcePool,
                minTier,
                maxTier,
                slotRule,
                alreadySelectedProducts);
        }

        private static List<ShopProductDefinitionSO> BuildCandidateListFromPool(
            IReadOnlyList<ShopProductDefinitionSO> productPool,
            int minTier,
            int maxTier,
            ShopProductSlotRule slotRule,
            List<ShopProductDefinitionSO> alreadySelectedProducts)
        {
            List<ShopProductDefinitionSO> candidates = new List<ShopProductDefinitionSO>();

            if (productPool == null)
                return candidates;

            for (int i = 0; i < productPool.Count; i++)
            {
                ShopProductDefinitionSO product = productPool[i];

                if (product == null)
                    continue;

                if (alreadySelectedProducts != null && alreadySelectedProducts.Contains(product))
                    continue;

                if (product.Tier < minTier)
                    continue;

                if (product.Tier > maxTier)
                    continue;

                if (slotRule != null && !slotRule.AllowsProductType(product.ProductType))
                    continue;

                candidates.Add(product);
            }

            return candidates;
        }
    }
}
