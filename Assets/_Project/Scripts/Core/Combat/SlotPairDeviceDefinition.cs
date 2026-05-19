using System;

namespace Tessera.Core
{
    /// <summary>SlotPair 계산에 사용할 단일 Device 효과 값을 정의한다.</summary>
    public class SlotPairDeviceDefinition
    {
        /// <summary>Device 효과 종류.</summary>
        public SlotPairDeviceType DeviceType { get; }

        /// <summary>정수 보정값.</summary>
        public int IntValue { get; }

        /// <summary>Force 곱연산 값.</summary>
        public float FloatValue { get; }

        /// <summary>조건 비교용 Force 기준값.</summary>
        public float ForceThreshold { get; }

        /// <summary>조건 비교용 Cast 카테고리.</summary>
        public RollPatternType RequiredPatternType { get; }

        /// <summary>디버그 및 추후 UI 표시용 설명.</summary>
        public string Description { get; }

        /// <summary>SlotPair Device 정의를 생성한다.</summary>
        public SlotPairDeviceDefinition(
            SlotPairDeviceType deviceType,
            int intValue,
            float floatValue,
            float forceThreshold,
            RollPatternType requiredPatternType,
            string description)
        {
            DeviceType = deviceType;
            IntValue = intValue;
            FloatValue = floatValue;
            ForceThreshold = forceThreshold;
            RequiredPatternType = requiredPatternType;
            Description = description ?? string.Empty;
        }

        /// <summary>효과가 없는 빈 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition None()
        {
            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.None,
                0,
                1f,
                0f,
                RollPatternType.None,
                "No device.");
        }

        /// <summary>현재 슬롯 주사위 값에 비례해 Score를 더하는 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition AddScoreByDiceValue(int multiplier)
        {
            if (multiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Score 배율은 음수가 될 수 없습니다.");

            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.AddScoreByDiceValue,
                multiplier,
                1f,
                0f,
                RollPatternType.None,
                $"Add score by dice value x{multiplier}.");
        }

        /// <summary>현재 주사위가 Cast 계산값에 포함되면 Force를 더하는 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition AddForceIfDiceIncluded(int forceAmount)
        {
            if (forceAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(forceAmount), "Force 보정값은 음수가 될 수 없습니다.");

            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.AddForceIfDiceIncluded,
                forceAmount,
                1f,
                0f,
                RollPatternType.None,
                $"Add force +{forceAmount} if dice is included.");
        }

        /// <summary>현재 주사위가 이전 슬롯 주사위와 같으면 Force를 더하는 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition AddForceIfSameAsPrevious(int forceAmount)
        {
            if (forceAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(forceAmount), "Force 보정값은 음수가 될 수 없습니다.");

            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.AddForceIfSameAsPrevious,
                forceAmount,
                1f,
                0f,
                RollPatternType.None,
                $"Add force +{forceAmount} if same as previous.");
        }

        /// <summary>현재 Force가 기준 이상이면 Force에 곱연산을 적용하는 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition MultiplyForceIfCurrentForceAtLeast(float threshold, float multiplier)
        {
            if (threshold < 0f)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Force 기준값은 음수가 될 수 없습니다.");

            if (multiplier < 0f)
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Force 곱연산 값은 음수가 될 수 없습니다.");

            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast,
                0,
                multiplier,
                threshold,
                RollPatternType.None,
                $"Multiply force x{multiplier} if force >= {threshold}.");
        }

        /// <summary>선택 Cast가 지정 타입이면 Score를 더하는 Device를 생성한다.</summary>
        public static SlotPairDeviceDefinition AddScoreIfCastType(RollPatternType patternType, int scoreAmount)
        {
            if (scoreAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(scoreAmount), "Score 보정값은 음수가 될 수 없습니다.");

            return new SlotPairDeviceDefinition(
                SlotPairDeviceType.AddScoreIfCastType,
                scoreAmount,
                1f,
                0f,
                patternType,
                $"Add score +{scoreAmount} if cast is {patternType}.");
        }
    }
}
