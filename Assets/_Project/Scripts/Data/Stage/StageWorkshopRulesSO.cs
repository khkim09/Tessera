using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Stage별 Workshop/Shop 등급 제한과 Overcharge 업그레이드 규칙을 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "StageWorkshopRules",
        menuName = "Tessera/Stage/Stage Workshop Rules")]
    public class StageWorkshopRulesSO : ScriptableObject
    {
        [Header("Workshop Tier")]
        [SerializeField] private int baseWorkshopTier = 1;
        [SerializeField] private int minShopProductTier = 1;
        [SerializeField] private int maxShopProductTier = 1;

        [Header("Overcharge Upgrade")]
        [SerializeField] private int tierUpgradeOverchargeCost = 1;
        [SerializeField] private int maxTierUpgradePerVisit = 1;
        [SerializeField] private int tierIncreasePerUpgrade = 1;

        [Header("Shop Inventory")]
        [SerializeField] private ShopProductDefinitionSO[] productPool;
        [SerializeField] private int productSlotCount = 3;
        [SerializeField] private bool allowDuplicateProducts;

        /// <summary>Workshop 진입 시 기본 Tier.</summary>
        public int BaseWorkshopTier => Mathf.Max(1, baseWorkshopTier);

        /// <summary>이 Stage에서 Shop에 등장 가능한 최소 상품 Tier.</summary>
        public int MinShopProductTier => Mathf.Max(1, minShopProductTier);

        /// <summary>이 Stage에서 Shop에 등장 가능한 최대 상품 Tier.</summary>
        public int MaxShopProductTier => Mathf.Max(MinShopProductTier, maxShopProductTier);

        /// <summary>Tier 상승 1회에 필요한 Overcharge 비용.</summary>
        public int TierUpgradeOverchargeCost => Mathf.Max(0, tierUpgradeOverchargeCost);

        /// <summary>Workshop 1회 방문 중 가능한 Tier 상승 횟수.</summary>
        public int MaxTierUpgradePerVisit => Mathf.Max(0, maxTierUpgradePerVisit);

        /// <summary>업그레이드 1회당 증가하는 Workshop Tier.</summary>
        public int TierIncreasePerUpgrade => Mathf.Max(1, tierIncreasePerUpgrade);

        /// <summary>이 Stage Workshop에서 등장 가능한 Shop 상품 후보 풀을 반환한다.</summary>
        public IReadOnlyList<ShopProductDefinitionSO> ProductPool => productPool;

        /// <summary>Shop에 표시할 상품 슬롯 수를 반환한다.</summary>
        public int ProductSlotCount => Mathf.Max(1, productSlotCount);

        /// <summary>Shop 상품 중복 등장을 허용하는지 반환한다.</summary>
        public bool AllowDuplicateProducts => allowDuplicateProducts;

        /// <summary>현재 Workshop Tier 기준으로 상품 최대 Tier를 계산한다.</summary>
        public int ResolveAllowedProductMaxTier(int currentWorkshopTier)
        {
            int tier = Mathf.Max(BaseWorkshopTier, currentWorkshopTier);
            return Mathf.Clamp(tier, MinShopProductTier, MaxShopProductTier);
        }
    }
}
