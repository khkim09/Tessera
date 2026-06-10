using System;

namespace Tessera.Data
{
    /// <summary>Shop/Workshop에서 취급하는 상품 타입을 정의한다.</summary>
    public enum ShopProductType
    {
        None = 0,

        Device = 10,

        DiceSet = 20,
        SingleDice = 21,
        DiceTypeUpgrade = 22,
        DiceFaceUpgrade = 23,

        Consumable = 30,
        PermanentUpgrade = 40,
        HpRepair = 50,

        WorkshopReroll = 100,
        WorkshopTierUpgrade = 101,
        RareSlotUnlock = 102,

        // Legacy aliases. 기존 Shop 코드 참조가 남아 있어도 즉시 컴파일이 깨지지 않도록 유지한다.
        [Obsolete("Use DiceSet, SingleDice, DiceTypeUpgrade, or DiceFaceUpgrade instead.")]
        Dice = DiceSet,

        [Obsolete("Use Consumable instead.")]
        Item = Consumable,

        [Obsolete("Use PermanentUpgrade instead.")]
        Relic = PermanentUpgrade,

        [Obsolete("Use WorkshopReroll instead.")]
        Reroll = WorkshopReroll,

        [Obsolete("Use WorkshopTierUpgrade instead.")]
        Upgrade = WorkshopTierUpgrade
    }
}
