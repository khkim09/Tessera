using System;

namespace Tessera.Core
{
    /// <summary>SlotPair 계산에 사용할 단일 Device 효과 값을 정의한다.</summary>
    public class SlotPairDeviceDefinition
    {
        public SlotPairDeviceType DeviceType { get; }
        public int IntValue { get; }
        public float FloatValue { get; }
        public float ForceThreshold { get; }

        public RollPatternType RequiredPatternType { get; }
        public RollPatternType SecondaryPatternType { get; }

        public DiceValueParity RequiredParity { get; }
        public int RequiredMinDiceValue { get; }
        public int RequiredMaxDiceValue { get; }

        /// <summary>0-based SlotPair index. -1이면 슬롯 조건 없음.</summary>
        public int RequiredSlotIndex { get; }

        public int RequiredStageThreatLevel { get; }
        public int TrueDamageValue { get; }

        public string Description { get; }

        public SlotPairDeviceDefinition(
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            RollPatternType secondaryPatternType,
            DiceValueParity requiredParity,
            int requiredMinDiceValue,
            int requiredMaxDiceValue,
            int requiredSlotIndex,
            int requiredStageThreatLevel,
            int trueDamageValue,
            string description)
        {
            DeviceType = deviceType;
            IntValue = intValue;
            FloatValue = floatValue;
            ForceThreshold = forceThreshold;
            RequiredPatternType = requiredPatternType;
            SecondaryPatternType = secondaryPatternType;
            RequiredParity = requiredParity;
            RequiredMinDiceValue = requiredMinDiceValue;
            RequiredMaxDiceValue = requiredMaxDiceValue;
            RequiredSlotIndex = requiredSlotIndex;
            RequiredStageThreatLevel = requiredStageThreatLevel;
            TrueDamageValue = trueDamageValue;
            Description = description ?? string.Empty;
        }

        public static SlotPairDeviceDefinition None()
        {
            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.None,
                0,
                1f,
                0f,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                "No device.");
        }

        public static SlotPairDeviceDefinition Create(
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            RollPatternType secondaryPatternType,
            DiceValueParity requiredParity,
            int requiredMinDiceValue,
            int requiredMaxDiceValue,
            int requiredSlotIndex,
            int requiredStageThreatLevel,
            int trueDamageValue,
            string description)
        {
            return new SlotPairDeviceDefinition(
                deviceType,
                Math.Max(0, intValue),
                floatValue <= 0f ? 1f : floatValue,
                Math.Max(0f, forceThreshold),
                requiredPatternType,
                secondaryPatternType,
                requiredParity,
                ClampDiceValue(requiredMinDiceValue),
                ClampDiceValue(requiredMaxDiceValue),
                requiredSlotIndex,
                Math.Max(0, requiredStageThreatLevel),
                Math.Max(0, trueDamageValue),
                description);
        }

        public static SlotPairDeviceDefinition AddScoreByDiceValue(int multiplier)
        {
            return Create(
                SlotPairDeviceType.AddScoreByDiceValue,
                multiplier,
                1f,
                0f,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                $"Add score by dice value x{multiplier}.");
        }

        public static SlotPairDeviceDefinition AddForceIfDiceIncluded(int forceAmount)
        {
            return Create(
                SlotPairDeviceType.AddForceIfDiceIncluded,
                forceAmount,
                1f,
                0f,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                $"Add force +{forceAmount} if dice is included.");
        }

        public static SlotPairDeviceDefinition AddForceIfSameAsPrevious(int forceAmount)
        {
            return Create(
                SlotPairDeviceType.AddForceIfSameAsPrevious,
                forceAmount,
                1f,
                0f,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                $"Add force +{forceAmount} if same as previous.");
        }

        public static SlotPairDeviceDefinition MultiplyForceIfCurrentForceAtLeast(float threshold, float multiplier)
        {
            return Create(
                SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast,
                0,
                multiplier,
                threshold,
                RollPatternType.None,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                $"Multiply force x{multiplier:0.##} if force >= {threshold:0.##}.");
        }

        public static SlotPairDeviceDefinition AddScoreIfCastType(RollPatternType patternType, int scoreAmount)
        {
            return Create(
                SlotPairDeviceType.AddScoreIfCastType,
                scoreAmount,
                1f,
                0f,
                patternType,
                RollPatternType.None,
                DiceValueParity.Any,
                1,
                6,
                -1,
                0,
                0,
                $"Add score +{scoreAmount} if cast is {patternType}.");
        }

        private static int ClampDiceValue(int value)
        {
            if (value < 1)
                return 1;

            if (value > 6)
                return 6;

            return value;
        }
    }
}
