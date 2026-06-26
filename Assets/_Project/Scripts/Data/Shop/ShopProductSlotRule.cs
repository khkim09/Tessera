using System;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop의 특정 표시 슬롯이 어떤 상품 풀에서 상품을 뽑을지 정의한다.</summary>
    [Serializable]
    public class ShopProductSlotRule
    {
        [SerializeField] private string displayName = "Shop Slot";
        [SerializeField] private ShopProductType[] allowedProductTypes; // 슬롯에 허용되는 상품 타입 목록
        [SerializeField] private ShopProductDefinitionSO[] productPool; // 슬롯 전용 상품 풀. 비어있으면 이 슬롯은 후보를 생성하지 않는다.

        [SerializeField] private int minTierOverride;
        [SerializeField] private int maxTierOverride;

        public string DisplayName => displayName;
        public ShopProductType[] AllowedProductTypes => allowedProductTypes;
        public ShopProductDefinitionSO[] ProductPool => productPool;
        public int MinTierOverride => minTierOverride;
        public int MaxTierOverride => maxTierOverride;

        /// <summary>이 슬롯 규칙이 슬롯별 추출에 사용 가능한지 확인한다.</summary>
        public bool HasAnyRule()
        {
            bool hasTypes = allowedProductTypes != null && allowedProductTypes.Length > 0;
            bool hasPool = productPool != null && productPool.Length > 0;

            return hasTypes || hasPool || minTierOverride > 0 || maxTierOverride > 0;
        }

        /// <summary>지정 상품 타입이 이 슬롯에서 허용되는지 확인한다.</summary>
        public bool AllowsProductType(ShopProductType productType)
        {
            if (allowedProductTypes == null || allowedProductTypes.Length == 0)
                return true;

            for (int i = 0; i < allowedProductTypes.Length; i++)
            {
                if (allowedProductTypes[i] == productType)
                    return true;
            }

            return false;
        }
    }
}
