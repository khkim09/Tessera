using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>LockSlot과 대응 Device를 좌에서 우로 계산해 전투 피해를 산출한다.</summary>
    public class SlotPairDamageCalculator
    {
        /// <summary>SlotPair 계산에 필요한 기본 슬롯 개수.</summary>
        public const int SlotPairCount = 5;

        /// <summary>PatternResult와 슬롯 배치를 기준으로 피해 미리보기를 계산한다.</summary>
        public SlotPairDamagePreview Calculate(
            PatternResult patternResult,
            IReadOnlyList<int> currentDiceValues,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions)
        {
            if (patternResult == null)
                throw new ArgumentNullException(nameof(patternResult));

            if (currentDiceValues == null)
                throw new ArgumentNullException(nameof(currentDiceValues));

            if (lockSlotDiceIndexes == null)
                throw new ArgumentNullException(nameof(lockSlotDiceIndexes));

            int currentScore = patternResult.RawCastScore + patternResult.FlatBonus;
            float currentForce = patternResult.BaseForce;
            List<SlotPairDamageStep> steps = new List<SlotPairDamageStep>();

            for (int slotIndex = 0; slotIndex < SlotPairCount; slotIndex++)
            {
                int diceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, slotIndex);
                int diceValue = GetDiceValueOrZero(currentDiceValues, diceIndex);
                SlotPairDeviceDefinition device = GetDeviceOrNone(deviceDefinitions, slotIndex);

                SlotPairDamageStep step = ApplyDevice(
                    slotIndex,
                    diceIndex,
                    diceValue,
                    patternResult,
                    lockSlotDiceIndexes,
                    currentDiceValues,
                    device,
                    currentScore,
                    currentForce,
                    out currentScore,
                    out currentForce);

                steps.Add(step);
            }

            int damageBeforeRules = CalculateDamageBeforeRules(currentScore, currentForce, patternResult.ExtraBonus);

            return new SlotPairDamagePreview(
                patternResult.PatternType,
                patternResult.RawCastScore,
                patternResult.RawCastScore + patternResult.FlatBonus,
                patternResult.BaseForce,
                currentScore,
                currentForce,
                damageBeforeRules,
                steps);
        }

        /// <summary>단일 SlotPair의 Device 효과를 적용한다.</summary>
        private SlotPairDamageStep ApplyDevice(
            int slotIndex,
            int diceIndex,
            int diceValue,
            PatternResult patternResult,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues,
            SlotPairDeviceDefinition device,
            int scoreBefore,
            float forceBefore,
            out int scoreAfter,
            out float forceAfter)
        {
            scoreAfter = scoreBefore;
            forceAfter = forceBefore;

            if (device == null || device.DeviceType == SlotPairDeviceType.None)
                return CreateStep(slotIndex, diceIndex, diceValue, SlotPairDeviceType.None, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "No device.");

            if (diceIndex < 0 || diceValue <= 0)
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "No dice in slot.");

            if (device.DeviceType == SlotPairDeviceType.AddScoreByDiceValue)
            {
                int addedScore = diceValue * device.IntValue;
                scoreAfter += addedScore;
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, true, $"Score +{addedScore}.");
            }

            if (device.DeviceType == SlotPairDeviceType.AddForceIfDiceIncluded)
            {
                if (!IsDiceValueIncluded(patternResult, diceValue))
                    return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "Dice value is not included.");

                forceAfter += device.IntValue;
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, true, $"Force +{device.IntValue}.");
            }

            if (device.DeviceType == SlotPairDeviceType.AddForceIfSameAsPrevious)
            {
                int previousDiceValue = GetPreviousSlotDiceValue(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                if (previousDiceValue != diceValue)
                    return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "Previous dice is different.");

                forceAfter += device.IntValue;
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, true, $"Force +{device.IntValue}.");
            }

            if (device.DeviceType == SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast)
            {
                if (forceBefore < device.ForceThreshold)
                    return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "Force threshold not met.");

                forceAfter *= device.FloatValue;
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, true, $"Force x{device.FloatValue:0.##}.");
            }

            if (device.DeviceType == SlotPairDeviceType.AddScoreIfCastType)
            {
                if (patternResult.PatternType != device.RequiredPatternType)
                    return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "Cast type not matched.");

                scoreAfter += device.IntValue;
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, true, $"Score +{device.IntValue}.");
            }

            return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, false, "Unsupported device.");
        }

        /// <summary>계산 전후 값을 이용해 SlotPair 단계 기록을 생성한다.</summary>
        private static SlotPairDamageStep CreateStep(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceType deviceType,
            int scoreBefore,
            int scoreAfter,
            float forceBefore,
            float forceAfter,
            bool didApply,
            string message)
        {
            return new SlotPairDamageStep(
                slotIndex,
                diceIndex,
                diceValue,
                deviceType,
                scoreBefore,
                scoreAfter,
                forceBefore,
                forceAfter,
                didApply,
                message);
        }

        /// <summary>Score와 Force로 Table Rule 적용 전 피해량을 계산한다.</summary>
        private static int CalculateDamageBeforeRules(int score, float force, int extraBonus)
        {
            float multipliedDamage = score * force;
            return (int)Math.Floor(multipliedDamage) + extraBonus;
        }

        /// <summary>지정 슬롯에 배치된 주사위 인덱스를 반환한다.</summary>
        private static int GetDiceIndexOrEmpty(IReadOnlyList<int> lockSlotDiceIndexes, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= lockSlotDiceIndexes.Count)
                return -1;

            return lockSlotDiceIndexes[slotIndex];
        }

        /// <summary>지정 주사위 인덱스의 현재 값을 반환한다.</summary>
        private static int GetDiceValueOrZero(IReadOnlyList<int> currentDiceValues, int diceIndex)
        {
            if (diceIndex < 0 || diceIndex >= currentDiceValues.Count)
                return 0;

            return currentDiceValues[diceIndex];
        }

        /// <summary>지정 슬롯의 Device 정의를 반환한다.</summary>
        private static SlotPairDeviceDefinition GetDeviceOrNone(IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions, int slotIndex)
        {
            if (deviceDefinitions == null)
                return SlotPairDeviceDefinition.None();

            if (slotIndex < 0 || slotIndex >= deviceDefinitions.Count)
                return SlotPairDeviceDefinition.None();

            return deviceDefinitions[slotIndex] ?? SlotPairDeviceDefinition.None();
        }

        /// <summary>현재 주사위 값이 Cast 계산에 포함된 값인지 확인한다.</summary>
        private static bool IsDiceValueIncluded(PatternResult patternResult, int diceValue)
        {
            for (int i = 0; i < patternResult.IncludedDiceValues.Count; i++)
            {
                if (patternResult.IncludedDiceValues[i] == diceValue)
                    return true;
            }

            return false;
        }

        /// <summary>이전 슬롯에 배치된 주사위 값을 반환한다.</summary>
        private static int GetPreviousSlotDiceValue(
            int slotIndex,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues)
        {
            if (slotIndex <= 0)
                return 0;

            int previousDiceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, slotIndex - 1);
            return GetDiceValueOrZero(currentDiceValues, previousDiceIndex);
        }
    }
}
