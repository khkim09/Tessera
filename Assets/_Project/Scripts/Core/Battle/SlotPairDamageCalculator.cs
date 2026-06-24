using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>LockSlot과 대응 Device를 좌에서 우로 계산해 전투 피해를 산출한다.</summary>
    public class SlotPairDamageCalculator
    {
        public const int SlotPairCount = 5;

        public SlotPairDamagePreview Calculate(
            PatternResult patternResult,
            IReadOnlyList<int> currentDiceValues,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            SlotPairCalculationContext calculationContext = null)
        {
            if (patternResult == null)
                throw new ArgumentNullException(nameof(patternResult));

            if (currentDiceValues == null)
                throw new ArgumentNullException(nameof(currentDiceValues));

            if (lockSlotDiceIndexes == null)
                throw new ArgumentNullException(nameof(lockSlotDiceIndexes));

            calculationContext ??= SlotPairCalculationContext.Empty;

            int currentScore = patternResult.RawCastScore + patternResult.FlatBonus;
            float currentForce = patternResult.BaseForce;
            int currentTruePower = 0;
            int currentDeviceImpactBonus = 0;
            int currentTrueImpactDamage = 0;

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
                    calculationContext,
                    currentScore,
                    currentForce,
                    currentTruePower,
                    out currentScore,
                    out currentForce,
                    out currentTruePower);

                steps.Add(step);
            }

            int castPowerBeforeRules = CalculateCastPowerBeforeRules(
                currentScore,
                currentForce,
                patternResult.TruePower + currentTruePower);

            return new SlotPairDamagePreview(
                patternResult.PatternType,
                patternResult.RawCastScore,
                patternResult.RawCastScore + patternResult.FlatBonus,
                patternResult.BaseForce,
                currentScore,
                currentForce,
                currentTruePower,
                castPowerBeforeRules,
                currentDeviceImpactBonus,
                currentTrueImpactDamage,
                steps);
        }

        private SlotPairDamageStep ApplyDevice(
            int slotIndex,
            int diceIndex,
            int diceValue,
            PatternResult patternResult,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues,
            SlotPairDeviceDefinition device,
            SlotPairCalculationContext calculationContext,
            int scoreBefore,
            float forceBefore,
            int truePowerBefore,
            out int scoreAfter,
            out float forceAfter,
            out int truePowerAfter)
        {
            scoreAfter = scoreBefore;
            forceAfter = forceBefore;
            truePowerAfter = truePowerBefore;

            if (device == null || device.DeviceType == SlotPairDeviceType.None)
                return CreateStep(slotIndex, diceIndex, diceValue, SlotPairDeviceType.None, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, false, "No device.");

            if (diceIndex < 0 || diceValue <= 0)
                return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, false, "No dice in slot.");

            switch (device.DeviceType)
            {
                case SlotPairDeviceType.AddScoreByDiceValue:
                    {
                        int addedScore = diceValue * device.IntValue;
                        scoreAfter += addedScore;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{addedScore}.");
                    }

                case SlotPairDeviceType.AddForceIfDiceIncluded:
                    {
                        if (!IsDiceValueIncluded(patternResult, diceValue))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is not included.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfSameAsPrevious:
                    {
                        int previousDiceValue = GetPreviousSlotDiceValue(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                        if (previousDiceValue != diceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Previous dice is different.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast:
                    {
                        if (forceBefore < device.ForceThreshold)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Force threshold not met.");

                        forceAfter *= device.FloatValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force x{device.FloatValue:0.##}.");
                    }

                case SlotPairDeviceType.AddScoreIfCastType:
                    {
                        if (patternResult.PatternType != device.RequiredPatternType)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Cast type not matched.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddScoreIfDiceParity:
                    {
                        if (!MatchesParity(diceValue, device.RequiredParity))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice parity not matched.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfDiceParity:
                    {
                        if (!MatchesParity(diceValue, device.RequiredParity))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice parity not matched.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.MultiplyForceIfDiceParity:
                    {
                        if (!MatchesParity(diceValue, device.RequiredParity))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice parity not matched.");

                        forceAfter *= device.FloatValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force x{device.FloatValue:0.##}.");
                    }

                case SlotPairDeviceType.AddScoreIfDiceValueAtLeast:
                    {
                        if (diceValue < device.RequiredMinDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is too low.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddScoreIfDiceValueAtMost:
                    {
                        if (diceValue > device.RequiredMaxDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is too high.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfDiceValueAtLeast:
                    {
                        if (diceValue < device.RequiredMinDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is too low.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfDiceValueAtMost:
                    {
                        if (diceValue > device.RequiredMaxDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is too high.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.MultiplyForceIfDiceValueAtLeast:
                    {
                        if (diceValue < device.RequiredMinDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice value is too low.");

                        forceAfter *= device.FloatValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force x{device.FloatValue:0.##}.");
                    }

                case SlotPairDeviceType.AddScoreIfSlotIndex:
                    {
                        if (slotIndex != device.RequiredSlotIndex)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Slot index not matched.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddTruePowerIfSlotIndex:
                    {
                        if (slotIndex != device.RequiredSlotIndex)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Slot index not matched.");

                        truePowerAfter += device.TruePowerValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"True Power +{device.TruePowerValue}.");
                    }

                case SlotPairDeviceType.AddForceIfGreaterThanPrevious:
                    {
                        int previousDiceValue = GetPreviousSlotDiceValue(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                        if (previousDiceValue <= 0 || diceValue <= previousDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice is not greater than previous.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.MultiplyForceIfGreaterThanPrevious:
                    {
                        int previousDiceValue = GetPreviousSlotDiceValue(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                        if (previousDiceValue <= 0 || diceValue <= previousDiceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Dice is not greater than previous.");

                        forceAfter *= device.FloatValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force x{device.FloatValue:0.##}.");
                    }

                case SlotPairDeviceType.AddForceIfSameAsMirrorSlot:
                    {
                        int mirrorDiceValue = GetMirrorSlotDiceValue(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                        if (mirrorDiceValue <= 0 || mirrorDiceValue != diceValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Mirror slot dice is different.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddScoreIfIsolatedFromNeighbors:
                    {
                        if (!IsIsolatedFromNeighbors(slotIndex, diceValue, lockSlotDiceIndexes, currentDiceValues))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Neighbor difference condition not met.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddScoreIfCastTypeEither:
                    {
                        if (!MatchesEitherCast(patternResult.PatternType, device.RequiredPatternType, device.SecondaryPatternType))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Cast type not matched.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfCastTypeEither:
                    {
                        if (!MatchesEitherCast(patternResult.PatternType, device.RequiredPatternType, device.SecondaryPatternType))
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Cast type not matched.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                case SlotPairDeviceType.MultiplyForceIfCastType:
                    {
                        if (patternResult.PatternType != device.RequiredPatternType)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Cast type not matched.");

                        forceAfter *= device.FloatValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force x{device.FloatValue:0.##}.");
                    }

                case SlotPairDeviceType.AddTruePowerIfPreviousSlotsSumAtLeast:
                    {
                        int previousSum = GetPreviousSlotsDiceValueSum(slotIndex, lockSlotDiceIndexes, currentDiceValues);

                        if (previousSum < device.IntValue)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, $"Previous slot sum {previousSum} is too low.");

                        truePowerAfter += device.TruePowerValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"True Power +{device.TruePowerValue}.");
                    }

                case SlotPairDeviceType.AddScoreIfStageThreatAtLeast:
                    {
                        if (calculationContext.StageThreatLevel < device.RequiredStageThreatLevel)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "StageThreat condition not met.");

                        scoreAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Score +{device.IntValue}.");
                    }

                case SlotPairDeviceType.AddForceIfStageThreatAtLeast:
                    {
                        if (calculationContext.StageThreatLevel < device.RequiredStageThreatLevel)
                            return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "StageThreat condition not met.");

                        forceAfter += device.IntValue;
                        return CreateStep(slotIndex, diceIndex, diceValue, device.DeviceType, scoreBefore, scoreAfter, forceBefore, forceAfter, truePowerBefore, truePowerAfter, true, $"Force +{device.IntValue}.");
                    }

                default:
                    return Inactive(slotIndex, diceIndex, diceValue, device, scoreBefore, forceBefore, truePowerBefore, "Unsupported device.");
            }
        }

        private static SlotPairDamageStep Inactive(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceDefinition device,
            int scoreBefore,
            float forceBefore,
            int truePowerBefore,
            string message)
        {
            return CreateStep(
                slotIndex,
                diceIndex,
                diceValue,
                device.DeviceType,
                scoreBefore,
                scoreBefore,
                forceBefore,
                forceBefore,
                truePowerBefore,
                truePowerBefore,
                false,
                message);
        }

        private static SlotPairDamageStep CreateStep(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceType deviceType,
            int scoreBefore,
            int scoreAfter,
            float forceBefore,
            float forceAfter,
            int truePowerBefore,
            int truePowerAfter,
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
                truePowerBefore,
                truePowerAfter,
                didApply,
                message);
        }

        /// <summary>Score, Force, TruePower를 조합해 TableRule 적용 전 CastPower를 계산한다.</summary>
        private static int CalculateCastPowerBeforeRules(int score, float force, int truePower)
        {
            float multipliedPower = score * force;
            return (int)Math.Floor(multipliedPower) + truePower;
        }

        private static int GetDiceIndexOrEmpty(IReadOnlyList<int> lockSlotDiceIndexes, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= lockSlotDiceIndexes.Count)
                return -1;

            return lockSlotDiceIndexes[slotIndex];
        }

        private static int GetDiceValueOrZero(IReadOnlyList<int> currentDiceValues, int diceIndex)
        {
            if (diceIndex < 0 || diceIndex >= currentDiceValues.Count)
                return 0;

            return currentDiceValues[diceIndex];
        }

        private static SlotPairDeviceDefinition GetDeviceOrNone(IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions, int slotIndex)
        {
            if (deviceDefinitions == null)
                return SlotPairDeviceDefinition.None();

            if (slotIndex < 0 || slotIndex >= deviceDefinitions.Count)
                return SlotPairDeviceDefinition.None();

            return deviceDefinitions[slotIndex] ?? SlotPairDeviceDefinition.None();
        }

        private static bool IsDiceValueIncluded(PatternResult patternResult, int diceValue)
        {
            for (int i = 0; i < patternResult.IncludedDiceValues.Count; i++)
            {
                if (patternResult.IncludedDiceValues[i] == diceValue)
                    return true;
            }

            return false;
        }

        private static bool MatchesParity(int diceValue, DiceValueParity parity)
        {
            if (parity == DiceValueParity.Any)
                return true;

            bool isEven = diceValue % 2 == 0;

            if (parity == DiceValueParity.Even)
                return isEven;

            return !isEven;
        }

        private static bool MatchesEitherCast(
            RollPatternType current,
            RollPatternType first,
            RollPatternType second)
        {
            return current == first || current == second;
        }

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

        private static int GetMirrorSlotDiceValue(
            int slotIndex,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues)
        {
            int mirrorSlotIndex = SlotPairCount - 1 - slotIndex;

            if (mirrorSlotIndex == slotIndex)
                return 0;

            int mirrorDiceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, mirrorSlotIndex);
            return GetDiceValueOrZero(currentDiceValues, mirrorDiceIndex);
        }

        private static bool IsIsolatedFromNeighbors(
            int slotIndex,
            int diceValue,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues)
        {
            if (slotIndex <= 0 || slotIndex >= SlotPairCount - 1)
                return false;

            int leftDiceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, slotIndex - 1);
            int rightDiceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, slotIndex + 1);

            int leftValue = GetDiceValueOrZero(currentDiceValues, leftDiceIndex);
            int rightValue = GetDiceValueOrZero(currentDiceValues, rightDiceIndex);

            if (leftValue <= 0 || rightValue <= 0)
                return false;

            return Math.Abs(diceValue - leftValue) >= 2 &&
                    Math.Abs(diceValue - rightValue) >= 2;
        }

        private static int GetPreviousSlotsDiceValueSum(
            int slotIndex,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<int> currentDiceValues)
        {
            int sum = 0;

            for (int i = 0; i < slotIndex; i++)
            {
                int diceIndex = GetDiceIndexOrEmpty(lockSlotDiceIndexes, i);
                sum += GetDiceValueOrZero(currentDiceValues, diceIndex);
            }

            return sum;
        }
    }
}
