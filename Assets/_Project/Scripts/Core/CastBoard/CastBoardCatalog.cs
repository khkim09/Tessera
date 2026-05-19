namespace Tessera.Core
{
    /// <summary>Cast Board 표시 순서와 표시 이름을 제공한다.</summary>
    public static class CastBoardCatalog
    {
        private static readonly RollPatternType[] OrderedPatternTypes =
        {
            RollPatternType.Aces,
            RollPatternType.Twos,
            RollPatternType.Threes,
            RollPatternType.Fours,
            RollPatternType.Fives,
            RollPatternType.Sixes,

            RollPatternType.ThreeOfAKind,
            RollPatternType.FourOfAKind,
            RollPatternType.FullHouse,
            RollPatternType.SmallStraight,
            RollPatternType.LargeStraight,
            RollPatternType.Chance,
            RollPatternType.Tessera,

            RollPatternType.BrokenCast
        };

        /// <summary>Cast Board에 표시할 카테고리 순서 목록을 반환한다.</summary>
        public static RollPatternType[] GetOrderedPatternTypes()
        {
            RollPatternType[] copy = new RollPatternType[OrderedPatternTypes.Length];

            for (int i = 0; i < OrderedPatternTypes.Length; i++)
                copy[i] = OrderedPatternTypes[i];

            return copy;
        }

        /// <summary>Cast 카테고리의 UI 표시 이름을 반환한다.</summary>
        public static string GetDisplayName(RollPatternType patternType)
        {
            switch (patternType)
            {
                case RollPatternType.Aces:
                    return "Aces";
                case RollPatternType.Twos:
                    return "Twos";
                case RollPatternType.Threes:
                    return "Threes";
                case RollPatternType.Fours:
                    return "Fours";
                case RollPatternType.Fives:
                    return "Fives";
                case RollPatternType.Sixes:
                    return "Sixes";
                case RollPatternType.ThreeOfAKind:
                    return "Three of a Kind";
                case RollPatternType.FourOfAKind:
                    return "Four of a Kind";
                case RollPatternType.FullHouse:
                    return "Full House";
                case RollPatternType.SmallStraight:
                    return "Small Straight";
                case RollPatternType.LargeStraight:
                    return "Large Straight";
                case RollPatternType.Chance:
                    return "Chance";
                case RollPatternType.Tessera:
                    return "Tessera";
                case RollPatternType.BrokenCast:
                    return "Broken Cast";
                default:
                    return patternType.ToString();
            }
        }
    }
}
