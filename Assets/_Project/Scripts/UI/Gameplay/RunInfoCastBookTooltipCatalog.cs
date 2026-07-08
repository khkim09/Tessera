using Tessera.Core;

namespace Tessera.UI
{
    /// <summary>RunInfo Cast Tooltip의 임시 설명과 미리보기 데이터를 제공한다.</summary>
    public static class RunInfoCastBookTooltipCatalog
    {
        /// <summary>지정 Cast 타입의 Tooltip 표시 데이터를 반환한다.</summary>
        public static RunInfoCastBookTooltipContent Resolve(RollPatternType patternType)
        {
            switch (patternType)
            {
                case RollPatternType.Aces:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "1이 나온 주사위 눈의 합입니다.\n[예시 : 2점]",
                        "Sum of dice showing 1.\n[e.g., 2 pts]",
                        new int[] { 5, 1, 1, 3, 6 },
                        new int[] { 1, 2 });

                case RollPatternType.Twos:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "2가 나온 주사위 눈의 합입니다.\n[예시 : 8점]",
                        "Sum of dice showing 2.\n[e.g., 8 pts]",
                        new int[] { 2, 4, 2, 2, 2 },
                        new int[] { 0, 2, 3, 4 });

                case RollPatternType.Threes:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "3이 나온 주사위 눈의 합입니다.\n[예시 : 9점]",
                        "Sum of dice showing 3.\n[e.g., 9 pts]",
                        new int[] { 3, 3, 3, 5, 6 },
                        new int[] { 0, 1, 2 });

                case RollPatternType.Fours:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "4가 나온 주사위 눈의 합입니다.\n[예시 : 12점]",
                        "Sum of dice showing 4.\n[e.g., 12 pts]",
                        new int[] { 2, 3, 4, 4, 4 },
                        new int[] { 2, 3, 4 });

                case RollPatternType.Fives:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "5가 나온 주사위 눈의 합입니다.\n[예시 : 5점]",
                        "Sum of dice showing 5.\n[e.g., 5 pts]",
                        new int[] { 3, 1, 5, 2, 6 },
                        new int[] { 2 });

                case RollPatternType.Sixes:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "6이 나온 주사위 눈의 합입니다.\n[예시 : 12점]",
                        "Sum of dice showing 6.\n[e.g., 12 pts]",
                        new int[] { 1, 6, 2, 6, 4 },
                        new int[] { 1, 3 });

                case RollPatternType.Chance:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "조건 없이 제출할 수 있습니다.\n모든 눈금의 합 [예시 : 18점]",
                        "No condition required.\nScore is the sum of all dice. [e.g., 18 pts]",
                        new int[] { 3, 2, 5, 6, 2 },
                        new int[] { 0, 1, 2, 3, 4 });

                case RollPatternType.ThreeOfAKind:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "주사위 3개의 눈금이 동일해야 합니다.\n모든 눈금의 합 [예시 : 17점]",
                        "Requires at least 3 matching dice.\nScore is the sum of all dice. [e.g., 17 pts]",
                        new int[] { 2, 2, 2, 5, 6 },
                        new int[] { 0, 1, 2 });

                case RollPatternType.SmallStraight:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "이어지는 눈금 4개 이상이 필요합니다.\n[고정 : 30점]",
                        "Requires at least 4 consecutive values.\n[Fixed : 30 pts]",
                        new int[] { 1, 2, 3, 4, 6 },
                        new int[] { 0, 1, 2, 3 });

                case RollPatternType.FullHouse:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "눈금이 동일한 주사위가 각각\n3개와 2개가 필요합니다. [고정 : 25점]",
                        "Requires three of one value and a pair.\n[Fixed : 25 pts]",
                        new int[] { 3, 3, 3, 6, 6 },
                        new int[] { 0, 1, 2, 3, 4 });

                case RollPatternType.FourOfAKind:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "주사위 4개의 눈금이 동일해야 합니다.\n모든 눈금의 합 [예시 : 22점]",
                        "Requires at least 4 matching dice.\nScore is the sum of all dice. [e.g., 22 pts]",
                        new int[] { 5, 5, 5, 5, 2 },
                        new int[] { 0, 1, 2, 3 });

                case RollPatternType.LargeStraight:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "이어지는 눈금 5개가 필요합니다.\n[고정 : 40점]",
                        "Requires 5 consecutive values.\n[Fixed : 40 pts]",
                        new int[] { 2, 3, 4, 5, 6 },
                        new int[] { 0, 1, 2, 3, 4 });

                case RollPatternType.Tessera:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "5개 주사위의 눈금이 모두 같아야 합니다!\n[고정 : 50점]",
                        "All 5 dice must match!\n[Fixed : 50 pts]",
                        new int[] { 2, 2, 2, 2, 2 },
                        new int[] { 0, 1, 2, 3, 4 });

                case RollPatternType.BrokenCast:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "항상 제출할 수 있는 방어 Cast입니다.\n피해 즉시 50% 감소, 과부하 +1. [0점]",
                        "A defensive Cast you can always play.\nInstantly reduce damage by 50%, Overcharge +1. [0pts]",
                        new int[] { 1, 2, 1, 2, 1 },
                        new int[] { });

                default:
                    return new RunInfoCastBookTooltipContent(
                        patternType,
                        "아직 설명이 등록되지 않은 Cast입니다.",
                        "No description has been registered for this Cast yet.",
                        new int[] { 1, 2, 3, 4, 5 },
                        new int[] { });
            }
        }
    }
}
