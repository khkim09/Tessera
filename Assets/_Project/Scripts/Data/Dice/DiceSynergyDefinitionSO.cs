using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>동일 DiceSynergyTag 개수에 따라 활성화되는 Dice 시너지 정의다.</summary>
    [CreateAssetMenu(
        fileName = "DiceSynergy_",
        menuName = "Tessera/Dice/Dice Synergy Definition")]
    public class DiceSynergyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string synergyId = "synergy.none";
        [SerializeField] private string displayName = "No Synergy";
        [TextArea]
        [SerializeField] private string description = "No synergy effect.";
        [SerializeField] private Sprite icon;

        [Header("Requirement")]
        [SerializeField] private DiceSynergyTag requiredTag = DiceSynergyTag.None;
        [SerializeField] private int requiredCount = 2;

        [Header("Effect")]
        [SerializeField] private DiceSynergyEffectType effectType = DiceSynergyEffectType.None;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        public string SynergyId => synergyId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public DiceSynergyTag RequiredTag => requiredTag;
        public int RequiredCount => Mathf.Max(1, requiredCount);

        public DiceSynergyEffectType EffectType => effectType;
        public int IntValue => intValue;
        public float FloatValue => floatValue;

        /// <summary>Core 런타임 계산용 DiceSynergy 규칙으로 변환한다.</summary>
        public DiceSynergyRuleData ToCoreRuleData()
        {
            return new DiceSynergyRuleData(
                DisplayName,
                (int)RequiredTag,
                RequiredCount,
                (Tessera.Core.DiceSynergyEffectType)(int)EffectType,
                IntValue,
                FloatValue);
        }
    }
}
