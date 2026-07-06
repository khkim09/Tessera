using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Run 동안 지속되는 영구 강화 Shop 상품 정의다.</summary>
    [CreateAssetMenu(
        fileName = "ShopPermanentUpgrade_",
        menuName = "Tessera/Shop/Permanent Upgrade Definition")]
    public class ShopPermanentUpgradeDefinitionSO : ScriptableObject, IShopItemDefinition
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "shop.permanent.none";
        [SerializeField] private string displayName = "Permanent Upgrade";
        [TextArea]
        [SerializeField] private string description = "Applied permanently for the current run when purchased.";
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private int tier = 1;
        [SerializeField] private int baseMoneyPrice;
        [SerializeField] private int baseOverchargePrice;

        [Header("Effect")]
        [SerializeField] private ShopPermanentUpgradeEffectType effectType = ShopPermanentUpgradeEffectType.None;
        [SerializeField] private int intValue;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => Mathf.Max(1, tier);
        public Sprite Icon => icon;
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);
        public ShopPermanentUpgradeEffectType EffectType => effectType;
        public int IntValue => Mathf.Max(0, intValue);
    }
}
