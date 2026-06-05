using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop에 등장 가능한 상품 정의 데이터다.</summary>
    [CreateAssetMenu(
        fileName = "ShopProduct_",
        menuName = "Tessera/Shop/Shop Product Definition")]
    public class ShopProductDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string productId = "shop.product.none";
        [SerializeField] private string displayName = "Shop Product";
        [SerializeField] private string description = "Shop product description.";
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private ShopProductType productType = ShopProductType.None;
        [SerializeField] private int tier = 1;
        [SerializeField] private int baseMoneyPrice = 1;
        [SerializeField] private int baseOverchargePrice;

        [Header("Device")]
        [SerializeField] private SlotPairDeviceDefinitionSO deviceDefinition;

        [Header("Future Placeholders")]
        [SerializeField] private ScriptableObject diceDefinitionPlaceholder;
        [SerializeField] private ScriptableObject itemDefinitionPlaceholder;
        [SerializeField] private ScriptableObject relicDefinitionPlaceholder;

        /// <summary>상품 고유 ID를 반환한다.</summary>
        public string ProductId => productId;

        /// <summary>상품 표시 이름을 반환한다.</summary>
        public string DisplayName => displayName;

        /// <summary>상품 설명을 반환한다.</summary>
        public string Description => description;

        /// <summary>상품 아이콘을 반환한다.</summary>
        public Sprite Icon => icon;

        /// <summary>상품 타입을 반환한다.</summary>
        public ShopProductType ProductType => productType;

        /// <summary>상품 Tier를 반환한다.</summary>
        public int Tier => Mathf.Max(1, tier);

        /// <summary>Money 가격을 반환한다.</summary>
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);

        /// <summary>Overcharge 가격을 반환한다.</summary>
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);

        /// <summary>Device 상품에 연결된 Device 정의를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO DeviceDefinition => deviceDefinition;

        /// <summary>추후 Dice 상품에 연결할 placeholder 정의를 반환한다.</summary>
        public ScriptableObject DiceDefinitionPlaceholder => diceDefinitionPlaceholder;

        /// <summary>추후 Item 상품에 연결할 placeholder 정의를 반환한다.</summary>
        public ScriptableObject ItemDefinitionPlaceholder => itemDefinitionPlaceholder;

        /// <summary>추후 Relic 상품에 연결할 placeholder 정의를 반환한다.</summary>
        public ScriptableObject RelicDefinitionPlaceholder => relicDefinitionPlaceholder;

        /// <summary>현재 빌드에서 실제 구매 적용 가능한 상품인지 확인한다.</summary>
        public bool IsPurchasableInCurrentBuild()
        {
            if (productType == ShopProductType.Device)
                return deviceDefinition != null;

            return false;
        }
    }
}
