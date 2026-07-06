using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop 카드로 구매하는 HP 회복 상품 정의다.</summary>
    [CreateAssetMenu(
        fileName = "ShopHpRepair_",
        menuName = "Tessera/Shop/HP Repair Definition")]
    public class ShopHpRepairDefinitionSO : ScriptableObject, IShopItemDefinition
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "shop.hp_repair.none";
        [SerializeField] private string displayName = "HP Repair";
        [TextArea]
        [SerializeField] private string description = "Repairs player HP immediately when purchased.";
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private int tier = 1;
        [SerializeField] private int baseMoneyPrice = 8;
        [SerializeField] private int baseOverchargePrice;

        [Header("Effect")]
        [SerializeField] private int healAmount = 10;
        [SerializeField] private bool restoreFullHP;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => Mathf.Max(1, tier);
        public Sprite Icon => icon;
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);
        public int HealAmount => Mathf.Max(0, healAmount);
        public bool RestoreFullHP => restoreFullHP;
    }
}
