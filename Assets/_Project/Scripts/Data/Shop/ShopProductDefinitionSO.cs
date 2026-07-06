using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop에 등장 가능한 상품 wrapper 정의다. 표시/가격 원본은 연결된 아이템 SO에서 읽는다.</summary>
    [CreateAssetMenu(
        fileName = "ShopProduct_",
        menuName = "Tessera/Shop/Shop Product Definition")]
    public class ShopProductDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Shop 상품 wrapper 고유 ID다.</summary>
        [SerializeField] private string productId = "shop.product.none";

        [Header("Shop")]
        /// <summary>Shop 상품 타입이다.</summary>
        [SerializeField] private ShopProductType productType = ShopProductType.None;
        /// <summary>Shop 카드 배경으로 사용할 선택 Sprite다. 비어 있으면 prefab 기본 배경을 유지한다.</summary>
        [SerializeField] private Sprite cardBackgroundSprite;

        [Header("Linked Item")]
        /// <summary>Device 상품일 때 연결되는 원본 Device 정의다.</summary>
        [SerializeField] private SlotPairDeviceDefinitionSO deviceDefinition;
        /// <summary>DiceType 계열 상품일 때 연결되는 원본 DiceType 정의다.</summary>
        [SerializeField] private DiceTypeDefinitionSO diceTypeDefinition;
        /// <summary>DiceFaceUpgrade 상품일 때 연결되는 원본 FaceUpgrade 정의다.</summary>
        [SerializeField] private DiceFaceUpgradeDefinitionSO diceFaceUpgradeDefinition;

        [Header("Future Placeholders")]
        /// <summary>Consumable 상품일 때 연결되는 원본 Consumable 정의다.</summary>
        [SerializeField] private ShopConsumableDefinitionSO consumableDefinition;
        /// <summary>PermanentUpgrade 상품일 때 연결되는 원본 PermanentUpgrade 정의다.</summary>
        [SerializeField] private ShopPermanentUpgradeDefinitionSO permanentUpgradeDefinition;
        /// <summary>HPRepair 상품일 때 연결되는 원본 HPRepair 정의다.</summary>
        [SerializeField] private ShopHpRepairDefinitionSO hpRepairDefinition;
        /// <summary>추후 Consumable 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        [SerializeField] private ScriptableObject consumableDefinitionPlaceholder;
        /// <summary>추후 PermanentUpgrade 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        [SerializeField] private ScriptableObject permanentUpgradeDefinitionPlaceholder;
        /// <summary>추후 HPRepair 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        [SerializeField] private ScriptableObject hpRepairDefinitionPlaceholder;

        /// <summary>Shop 상품 wrapper 고유 ID다.</summary>
        public string ProductId => string.IsNullOrWhiteSpace(productId) ? ResolveFallbackProductId() : productId;

        /// <summary>현재 상품 타입에 연결된 원본 Shop 아이템 정의다.</summary>
        public IShopItemDefinition ItemDefinition => ResolveItemDefinition();

        /// <summary>Shop 카드와 Tooltip에 표시할 이름이다.</summary>
        public string DisplayName => ItemDefinition != null ? ItemDefinition.DisplayName : name;

        /// <summary>Shop 카드와 Tooltip에 표시할 설명이다.</summary>
        public string Description => ItemDefinition != null ? ItemDefinition.Description : string.Empty;

        /// <summary>Shop 카드에 표시할 아이콘이다.</summary>
        public Sprite Icon => ItemDefinition != null ? ItemDefinition.Icon : null;

        /// <summary>Shop 상품 타입이다.</summary>
        public ShopProductType ProductType => productType;

        /// <summary>Shop 카드 배경으로 사용할 선택 Sprite다. 비어 있으면 prefab 기본 배경을 유지한다.</summary>
        public Sprite CardBackgroundSprite => cardBackgroundSprite;

        /// <summary>Shop 등장/필터링에 사용할 Tier다.</summary>
        public int Tier => ItemDefinition != null ? Mathf.Max(1, ItemDefinition.Tier) : 1;

        /// <summary>구매 시 기본 Money 가격이다.</summary>
        public int BaseMoneyPrice => ItemDefinition != null ? Mathf.Max(0, ItemDefinition.BaseMoneyPrice) : 0;

        /// <summary>구매 시 기본 Overcharge 가격이다.</summary>
        public int BaseOverchargePrice => ItemDefinition != null ? Mathf.Max(0, ItemDefinition.BaseOverchargePrice) : 0;

        /// <summary>Device 상품 원본 정의다.</summary>
        public SlotPairDeviceDefinitionSO DeviceDefinition => deviceDefinition;

        /// <summary>DiceType 계열 상품 원본 정의다.</summary>
        public DiceTypeDefinitionSO DiceTypeDefinition => diceTypeDefinition;

        /// <summary>DiceFaceUpgrade 상품 원본 정의다.</summary>
        public DiceFaceUpgradeDefinitionSO DiceFaceUpgradeDefinition => diceFaceUpgradeDefinition;

        /// <summary>Consumable 상품 원본 정의다.</summary>
        public ShopConsumableDefinitionSO ConsumableDefinition => consumableDefinition;

        /// <summary>PermanentUpgrade 상품 원본 정의다.</summary>
        public ShopPermanentUpgradeDefinitionSO PermanentUpgradeDefinition => permanentUpgradeDefinition;

        /// <summary>HPRepair 상품 원본 정의다.</summary>
        public ShopHpRepairDefinitionSO HpRepairDefinition => hpRepairDefinition;

        /// <summary>추후 Consumable 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        public ScriptableObject ConsumableDefinitionPlaceholder => consumableDefinitionPlaceholder;

        /// <summary>추후 PermanentUpgrade 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        public ScriptableObject PermanentUpgradeDefinitionPlaceholder => permanentUpgradeDefinitionPlaceholder;

        /// <summary>추후 HPRepair 상품 원본 정의가 추가되기 전까지 사용하는 임시 참조다.</summary>
        public ScriptableObject HpRepairDefinitionPlaceholder => hpRepairDefinitionPlaceholder;

        /// <summary>기존 Dice placeholder 호환 프로퍼티다.</summary>
        public ScriptableObject DiceDefinitionPlaceholder => diceTypeDefinition;

        /// <summary>기존 Item placeholder 호환 프로퍼티다.</summary>
        public ScriptableObject ItemDefinitionPlaceholder => consumableDefinitionPlaceholder;

        /// <summary>기존 Relic placeholder 호환 프로퍼티다.</summary>
        public ScriptableObject RelicDefinitionPlaceholder => permanentUpgradeDefinitionPlaceholder;

        /// <summary>현재 빌드에서 실제 구매 적용 가능한 상품인지 확인한다.</summary>
        public bool IsPurchasableInCurrentBuild()
        {
            switch (productType)
            {
                case ShopProductType.Device:
                    return deviceDefinition != null;

                case ShopProductType.DiceSet:
                case ShopProductType.SingleDice:
                case ShopProductType.DiceTypeUpgrade:
                    return diceTypeDefinition != null;

                case ShopProductType.DiceFaceUpgrade:
                    return diceFaceUpgradeDefinition != null;

                case ShopProductType.Consumable:
                    return consumableDefinition != null;

                case ShopProductType.PermanentUpgrade:
                    return permanentUpgradeDefinition != null;

                case ShopProductType.HpRepair:
                    return hpRepairDefinition != null;

                default:
                    return false;
            }
        }

        /// <summary>상품 타입 기준으로 연결된 원본 아이템 정의를 반환한다.</summary>
        private IShopItemDefinition ResolveItemDefinition()
        {
            switch (productType)
            {
                case ShopProductType.Device:
                    return deviceDefinition;

                case ShopProductType.DiceSet:
                case ShopProductType.SingleDice:
                case ShopProductType.DiceTypeUpgrade:
                    return diceTypeDefinition;

                case ShopProductType.DiceFaceUpgrade:
                    return diceFaceUpgradeDefinition;

                case ShopProductType.Consumable:
                    return consumableDefinition;

                case ShopProductType.PermanentUpgrade:
                    return permanentUpgradeDefinition;

                case ShopProductType.HpRepair:
                    return hpRepairDefinition;

                default:
                    return null;
            }
        }

        /// <summary>원본 아이템 ID를 기반으로 wrapper fallback ID를 생성한다.</summary>
        private string ResolveFallbackProductId()
        {
            IShopItemDefinition itemDefinition = ResolveItemDefinition();

            if (itemDefinition != null && !string.IsNullOrWhiteSpace(itemDefinition.ItemId))
                return $"shop.{itemDefinition.ItemId}";

            return name;
        }
    }
}
