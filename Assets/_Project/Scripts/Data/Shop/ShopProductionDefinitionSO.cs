using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop에 표시되는 하나의 상품 데이터를 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "ShopProduct_",
        menuName = "Tessera/Shop/Shop Product Definition")]
    public class ShopProductDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string productId = "shop.product.none";
        [SerializeField] private string displayName = "Product";
        [SerializeField] private string description = "No product description.";
        [SerializeField] private Sprite icon;

        [Header("Price")]
        [SerializeField] private int price = 5;

        [Header("Product")]
        [SerializeField] private ShopProductType productType = ShopProductType.SlotPairDevice;
        [SerializeField] private SlotPairDeviceDefinitionSO slotPairDevice;

        /// <summary>상품 고유 ID를 반환한다.</summary>
        public string ProductId => productId;

        /// <summary>상품 표시 이름을 반환한다.</summary>
        public string DisplayName => displayName;

        /// <summary>상품 설명을 반환한다.</summary>
        public string Description => description;

        /// <summary>상품 아이콘을 반환한다.</summary>
        public Sprite Icon => icon;

        /// <summary>상품 가격을 반환한다.</summary>
        public int Price => price;

        /// <summary>상품 타입을 반환한다.</summary>
        public ShopProductType ProductType => productType;

        /// <summary>Device 상품일 때 장착할 Device SO를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO SlotPairDevice => slotPairDevice;

        /// <summary>구매 가능한 최소 유효 상품인지 확인한다.</summary>
        public bool IsValidProduct()
        {
            if (productType == ShopProductType.None)
                return false;

            if (productType == ShopProductType.SlotPairDevice && slotPairDevice == null)
                return false;

            return price >= 0;
        }
    }
}
