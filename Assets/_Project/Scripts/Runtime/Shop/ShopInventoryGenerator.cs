using System.Collections.Generic;
using Tessera.Data;

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

            List<ShopProductDefinitionSO> candidates = BuildCandidateList(rules, currentWorkshopTier);

            if (candidates.Count == 0)
                return result;

            System.Random random = new System.Random(seed);
            int slotCount = rules.ProductSlotCount;

            for (int i = 0; i < slotCount; i++)
            {
                if (candidates.Count == 0)
                    break;

                int selectedIndex = random.Next(0, candidates.Count);
                ShopProductDefinitionSO selectedProduct = candidates[selectedIndex];

                ShopInventorySlot slot = new ShopInventorySlot(
                    i,
                    selectedProduct,
                    selectedProduct.BaseMoneyPrice,
                    selectedProduct.BaseOverchargePrice);

                result.Add(slot);

                if (!rules.AllowDuplicateProducts)
                    candidates.RemoveAt(selectedIndex);
            }

            return result;
        }

        /// <summary>현재 Workshop Tier에 맞는 상품 후보 목록을 만든다.</summary>
        private static List<ShopProductDefinitionSO> BuildCandidateList(
            StageWorkshopRulesSO rules,
            int currentWorkshopTier)
        {
            List<ShopProductDefinitionSO> candidates = new List<ShopProductDefinitionSO>();
            IReadOnlyList<ShopProductDefinitionSO> productPool = rules.ProductPool;

            if (productPool == null)
                return candidates;

            int minTier = rules.MinShopProductTier;
            int maxTier = rules.ResolveAllowedProductMaxTier(currentWorkshopTier);

            for (int i = 0; i < productPool.Count; i++)
            {
                ShopProductDefinitionSO product = productPool[i];

                if (product == null)
                    continue;

                if (product.Tier < minTier)
                    continue;

                if (product.Tier > maxTier)
                    continue;

                if (!product.IsPurchasableInCurrentBuild())
                    continue;

                candidates.Add(product);
            }

            return candidates;
        }
    }
}
