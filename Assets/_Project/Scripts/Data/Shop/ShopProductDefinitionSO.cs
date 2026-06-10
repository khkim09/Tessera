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
        [TextArea]
        [SerializeField] private string description = "Shop product description.";
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private ShopProductType productType = ShopProductType.None;
        [SerializeField] private int tier = 1;
        [SerializeField] private int baseMoneyPrice = 1;
        [SerializeField] private int baseOverchargePrice;

        [Header("Device")]
        [SerializeField] private SlotPairDeviceDefinitionSO deviceDefinition;

        [Header("Dice")]
        [SerializeField] private DiceTypeDefinitionSO diceTypeDefinition;
        [SerializeField] private DiceFaceUpgradeDefinitionSO diceFaceUpgradeDefinition;

        [Header("Future Placeholders")]
        [SerializeField] private ScriptableObject consumableDefinitionPlaceholder;
        [SerializeField] private ScriptableObject permanentUpgradeDefinitionPlaceholder;
        [SerializeField] private ScriptableObject hpRepairDefinitionPlaceholder;

        public string ProductId => productId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public ShopProductType ProductType => productType;
        public int Tier => Mathf.Max(1, tier);
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);

        public SlotPairDeviceDefinitionSO DeviceDefinition => deviceDefinition;
        public DiceTypeDefinitionSO DiceTypeDefinition => diceTypeDefinition;
        public DiceFaceUpgradeDefinitionSO DiceFaceUpgradeDefinition => diceFaceUpgradeDefinition;

        public ScriptableObject ConsumableDefinitionPlaceholder => consumableDefinitionPlaceholder;
        public ScriptableObject PermanentUpgradeDefinitionPlaceholder => permanentUpgradeDefinitionPlaceholder;
        public ScriptableObject HpRepairDefinitionPlaceholder => hpRepairDefinitionPlaceholder;

        // Legacy property names. 기존 Shop UI/Spawner 코드 호환용.
        public ScriptableObject DiceDefinitionPlaceholder => diceTypeDefinition;
        public ScriptableObject ItemDefinitionPlaceholder => consumableDefinitionPlaceholder;
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

                default:
                    return false;
            }
        }
    }
}
