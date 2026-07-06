using UnityEngine;

namespace Tessera.Data
{
    /// <summary>구매 즉시 소모되어 RunSession에 효과를 적용하는 Shop 상품 정의다.</summary>
    [CreateAssetMenu(
        fileName = "ShopConsumable_",
        menuName = "Tessera/Shop/Consumable Definition")]
    public class ShopConsumableDefinitionSO : ScriptableObject, IShopItemDefinition
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "shop.consumable.none";
        [SerializeField] private string displayName = "Consumable";
        [TextArea]
        [SerializeField] private string description = "Consumed immediately when purchased.";
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private int tier = 1;
        [SerializeField] private int baseMoneyPrice;
        [SerializeField] private int baseOverchargePrice;

        [Header("Effect")]
        [SerializeField] private ShopConsumableEffectType effectType = ShopConsumableEffectType.None;
        [SerializeField] private int intValue;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => Mathf.Max(1, tier);
        public Sprite Icon => icon;
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);
        public ShopConsumableEffectType EffectType => effectType;
        public int IntValue => Mathf.Max(0, intValue);
    }
}
