using System;

namespace Tessera.Core
{
    /// <summary>Core 계산용 DiceSynergy 규칙 데이터다.</summary>
    public readonly struct DiceSynergyRuleData
    {
        public static DiceSynergyRuleData Empty => new DiceSynergyRuleData(string.Empty, 0, 0, DiceSynergyEffectType.None, 0, 0f);

        public string DisplayName { get; }
        public int RequiredTagValue { get; }
        public int RequiredCount { get; }
        public DiceSynergyEffectType EffectType { get; }
        public int IntValue { get; }
        public float FloatValue { get; }
        public bool IsValid => EffectType != DiceSynergyEffectType.None && RequiredTagValue != 0 && RequiredCount > 0;

        public DiceSynergyRuleData(string displayName, int requiredTagValue, int requiredCount, DiceSynergyEffectType effectType, int intValue, float floatValue)
        {
            DisplayName = displayName ?? string.Empty;
            RequiredTagValue = requiredTagValue;
            RequiredCount = Math.Max(1, requiredCount);
            EffectType = effectType;
            IntValue = Math.Max(0, intValue);
            FloatValue = floatValue;
        }
    }
}
