using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>특정 Dice의 특정 Face 하나를 교체/각인하는 성장 상품 정의다.</summary>
    [CreateAssetMenu(
        fileName = "DiceFaceUpgrade_",
        menuName = "Tessera/Dice/Dice Face Upgrade Definition")]
    public class DiceFaceUpgradeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string upgradeId = "face.upgrade.none";
        [SerializeField] private string displayName = "Face Upgrade";
        [TextArea]
        [SerializeField] private string description = "Dice face upgrade.";
        [SerializeField] private Sprite icon;

        [Header("Target")]
        [SerializeField] private bool requiresSpecificNumber;
        [SerializeField] private int targetNumber = 6;

        [Header("Replacement")]
        [SerializeField] private DiceFaceType replacementFaceType = DiceFaceType.Number;
        [SerializeField] private int replacementNumberValue = 6;

        [Header("Effect")]
        [SerializeField] private DiceFaceUpgradeEffectType effectType = DiceFaceUpgradeEffectType.None;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        [Header("Shop")]
        [SerializeField] private int rarity = 1;
        [SerializeField] private int baseMoneyPrice = 3;

        public string UpgradeId => upgradeId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public bool RequiresSpecificNumber => requiresSpecificNumber;
        public int TargetNumber => Mathf.Clamp(targetNumber, 1, 6);

        public DiceFaceType ReplacementFaceType => replacementFaceType;
        public int ReplacementNumberValue => Mathf.Clamp(replacementNumberValue, 1, 6);

        public DiceFaceUpgradeEffectType EffectType => effectType;
        public int IntValue => intValue;
        public float FloatValue => floatValue;

        public int Rarity => Mathf.Max(1, rarity);
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);

        public DiceFace CreateReplacementFace()
        {
            if (replacementFaceType == DiceFaceType.Number)
                return DiceFace.Number(ReplacementNumberValue);

            return new DiceFace(replacementFaceType, ReplacementNumberValue);
        }
    }
}
