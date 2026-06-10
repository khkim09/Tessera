using UnityEngine;

namespace Tessera.Data
{
    /// <summary>주사위 자체의 타입, 색/계열, 고유 효과, Shop 가격을 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "DiceType_",
        menuName = "Tessera/Dice/Dice Type Definition")]
    public class DiceTypeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string diceTypeId = "dice.standard";
        [SerializeField] private string displayName = "Standard Dice";
        [TextArea]
        [SerializeField] private string description = "Standard six-sided dice.";
        [SerializeField] private Sprite icon;

        [Header("Visual")]
        [SerializeField] private Color visualColor = Color.white;
        [SerializeField] private string materialKey = "dice.standard";

        [Header("Synergy")]
        [SerializeField] private DiceSynergyTag synergyTag = DiceSynergyTag.None;

        [Header("Intrinsic Effect")]
        [SerializeField] private DiceIntrinsicEffectType intrinsicEffectType = DiceIntrinsicEffectType.None;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        [Header("Shop")]
        [SerializeField] private int rarity = 1;
        [SerializeField] private int unlockStage = 1;
        [SerializeField] private int baseMoneyPrice = 4;

        public string DiceTypeId => diceTypeId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public Color VisualColor => visualColor;
        public string MaterialKey => materialKey;

        public DiceSynergyTag SynergyTag => synergyTag;
        public DiceIntrinsicEffectType IntrinsicEffectType => intrinsicEffectType;
        public int IntValue => intValue;
        public float FloatValue => floatValue;

        public int Rarity => Mathf.Max(1, rarity);
        public int UnlockStage => Mathf.Max(1, unlockStage);
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
    }
}
