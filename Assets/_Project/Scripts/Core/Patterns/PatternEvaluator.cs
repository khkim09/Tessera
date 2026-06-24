using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>5개의 주사위 눈금을 검사해 Cast Board 카테고리와 피해량을 판정한다.</summary>
    public class PatternEvaluator
    {
        private const int RequiredDiceCount = 5;

        private readonly CastPowerCalculator _castPowerCalculator;

        /// <summary>피해 계산기를 사용하는 Cast 판정기를 생성한다.</summary>
        public PatternEvaluator(CastPowerCalculator castPowerCalculator)
        {
            _castPowerCalculator = castPowerCalculator ?? throw new ArgumentNullException(nameof(castPowerCalculator));
        }

        /// <summary>초기 기본 보정표를 사용하는 Cast 판정기를 생성한다.</summary>
        public static PatternEvaluator CreateDefault()
        {
            IReadOnlyDictionary<RollPatternType, PatternDefinition> definitions = PatternDefinition.CreateDefaultDefinitions();
            CastPowerCalculator castPowerCalculator = new CastPowerCalculator(definitions);
            return new PatternEvaluator(castPowerCalculator);
        }

        /// <summary>주사위 인스턴스 목록에서 선택 가능한 모든 Cast 카테고리를 판정한다.</summary>
        public List<PatternResult> EvaluateAll(IReadOnlyList<DiceInstance> dice)
        {
            if (dice == null)
                throw new ArgumentNullException(nameof(dice));

            int[] values = new int[dice.Count];

            for (int i = 0; i < dice.Count; i++)
                values[i] = dice[i].GetCurrentNumberValue();

            return EvaluateAll(values);
        }

        /// <summary>숫자 눈금 목록에서 선택 가능한 모든 Cast 카테고리를 판정한다.</summary>
        public List<PatternResult> EvaluateAll(IReadOnlyList<int> diceValues)
        {
            ValidateDiceValues(diceValues);

            List<PatternResult> results = new List<PatternResult>();
            int[] counts = BuildCounts(diceValues);
            bool[] exists = BuildExists(counts);
            int totalSum = Sum(diceValues);

            AddUpperNumberCast(results, RollPatternType.Aces, 1, counts);
            AddUpperNumberCast(results, RollPatternType.Twos, 2, counts);
            AddUpperNumberCast(results, RollPatternType.Threes, 3, counts);
            AddUpperNumberCast(results, RollPatternType.Fours, 4, counts);
            AddUpperNumberCast(results, RollPatternType.Fives, 5, counts);
            AddUpperNumberCast(results, RollPatternType.Sixes, 6, counts);

            TryAddThreeOfAKind(results, counts, diceValues, totalSum);
            TryAddFourOfAKind(results, counts, diceValues, totalSum);
            TryAddFullHouse(results, counts, diceValues);
            TryAddSmallStraight(results, exists);
            TryAddLargeStraight(results, exists, diceValues);
            AddChance(results, diceValues, totalSum);
            TryAddTessera(results, counts, diceValues);
            AddBrokenCast(results);

            SortByCastPowerDescending(results);
            return results;
        }

        /// <summary>주사위 인스턴스 목록에서 Broken Cast를 제외한 최고 피해 Cast를 판정한다.</summary>
        public PatternResult EvaluateBest(IReadOnlyList<DiceInstance> dice)
        {
            List<PatternResult> results = EvaluateAll(dice);
            return FindBestNonBrokenResult(results);
        }

        /// <summary>숫자 눈금 목록에서 Broken Cast를 제외한 최고 피해 Cast를 판정한다.</summary>
        public PatternResult EvaluateBest(IReadOnlyList<int> diceValues)
        {
            List<PatternResult> results = EvaluateAll(diceValues);
            return FindBestNonBrokenResult(results);
        }

        /// <summary>선택한 Cast 카테고리가 현재 주사위에서 제출 가능한지 확인하고 결과를 반환한다.</summary>
        public bool TryEvaluateSpecificPattern(IReadOnlyList<int> diceValues, RollPatternType requestedPatternType, out PatternResult result)
        {
            ValidateDiceValues(diceValues);

            List<PatternResult> results = EvaluateAll(diceValues);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].PatternType != requestedPatternType)
                    continue;

                result = results[i];
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>선택한 Cast 카테고리가 현재 주사위에서 제출 가능한지 확인한다.</summary>
        public bool CanUsePattern(IReadOnlyList<int> diceValues, RollPatternType requestedPatternType)
        {
            return TryEvaluateSpecificPattern(diceValues, requestedPatternType, out PatternResult _);
        }

        private void AddUpperNumberCast(List<PatternResult> results, RollPatternType patternType, int targetNumber, int[] counts)
        {
            int rawScore = targetNumber * counts[targetNumber];
            List<int> includedValues = new List<int>(counts[targetNumber]);

            for (int i = 0; i < counts[targetNumber]; i++)
                includedValues.Add(targetNumber);

            results.Add(_castPowerCalculator.CreateResult(patternType, includedValues, rawScore));
        }

        private void TryAddThreeOfAKind(List<PatternResult> results, int[] counts, IReadOnlyList<int> diceValues, int totalSum)
        {
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] < 3)
                    continue;

                results.Add(_castPowerCalculator.CreateResult(RollPatternType.ThreeOfAKind, diceValues, totalSum));
                return;
            }
        }

        private void TryAddFourOfAKind(List<PatternResult> results, int[] counts, IReadOnlyList<int> diceValues, int totalSum)
        {
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] < 4)
                    continue;

                results.Add(_castPowerCalculator.CreateResult(RollPatternType.FourOfAKind, diceValues, totalSum));
                return;
            }
        }

        private void TryAddFullHouse(List<PatternResult> results, int[] counts, IReadOnlyList<int> diceValues)
        {
            bool hasTriple = false;
            bool hasPair = false;

            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] == 3)
                    hasTriple = true;

                if (counts[value] == 2)
                    hasPair = true;
            }

            if (!hasTriple || !hasPair)
                return;

            results.Add(_castPowerCalculator.CreateResult(RollPatternType.FullHouse, diceValues, 25));
        }

        private void TryAddSmallStraight(List<PatternResult> results, bool[] exists)
        {
            bool hasLow = exists[1] && exists[2] && exists[3] && exists[4];
            bool hasMiddle = exists[2] && exists[3] && exists[4] && exists[5];
            bool hasHigh = exists[3] && exists[4] && exists[5] && exists[6];

            if (!hasLow && !hasMiddle && !hasHigh)
                return;

            results.Add(_castPowerCalculator.CreateResult(RollPatternType.SmallStraight, new List<int>(), 30));
        }

        private void TryAddLargeStraight(List<PatternResult> results, bool[] exists, IReadOnlyList<int> diceValues)
        {
            bool hasLow = exists[1] && exists[2] && exists[3] && exists[4] && exists[5];
            bool hasHigh = exists[2] && exists[3] && exists[4] && exists[5] && exists[6];

            if (!hasLow && !hasHigh)
                return;

            results.Add(_castPowerCalculator.CreateResult(RollPatternType.LargeStraight, diceValues, 40));
        }

        private void AddChance(List<PatternResult> results, IReadOnlyList<int> diceValues, int totalSum)
        {
            results.Add(_castPowerCalculator.CreateResult(RollPatternType.Chance, diceValues, totalSum));
        }

        private void TryAddTessera(List<PatternResult> results, int[] counts, IReadOnlyList<int> diceValues)
        {
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] < 5)
                    continue;

                results.Add(_castPowerCalculator.CreateResult(RollPatternType.Tessera, diceValues, 50));
                return;
            }
        }

        private void AddBrokenCast(List<PatternResult> results)
        {
            results.Add(_castPowerCalculator.CreateResult(RollPatternType.BrokenCast, Array.Empty<int>(), 0));
        }

        private static PatternResult FindBestNonBrokenResult(IReadOnlyList<PatternResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].PatternType != RollPatternType.BrokenCast)
                    return results[i];
            }

            return results[0];
        }

        private static int[] BuildCounts(IReadOnlyList<int> diceValues)
        {
            int[] counts = new int[7];

            for (int i = 0; i < diceValues.Count; i++)
                counts[diceValues[i]]++;

            return counts;
        }

        private static bool[] BuildExists(int[] counts)
        {
            bool[] exists = new bool[7];

            for (int value = 1; value <= 6; value++)
                exists[value] = counts[value] > 0;

            return exists;
        }

        private static void SortByCastPowerDescending(List<PatternResult> results)
        {
            results.Sort((a, b) =>
            {
                int castPowerComparison = b.CastPower.CompareTo(a.CastPower);

                if (castPowerComparison != 0)
                    return castPowerComparison;

                return ((int)b.PatternType).CompareTo((int)a.PatternType);
            });
        }

        private static void ValidateDiceValues(IReadOnlyList<int> diceValues)
        {
            if (diceValues == null)
                throw new ArgumentNullException(nameof(diceValues));

            if (diceValues.Count != RequiredDiceCount)
                throw new ArgumentException($"정확히 {RequiredDiceCount}개의 주사위 값이 필요합니다.", nameof(diceValues));

            for (int i = 0; i < diceValues.Count; i++)
            {
                if (diceValues[i] < 1 || diceValues[i] > 6)
                    throw new ArgumentOutOfRangeException(nameof(diceValues), $"주사위 값은 1~6 사이여야 합니다. Index: {i}");
            }
        }

        private static int Sum(IReadOnlyList<int> values)
        {
            int sum = 0;

            for (int i = 0; i < values.Count; i++)
                sum += values[i];

            return sum;
        }
    }
}
