namespace Tessera.Core
{
    /// <summary>Cast Board에서 선택할 수 있는 주사위 카테고리 종류를 정의한다.</summary>
    public enum RollPatternType
    {
        None = 0,

        Aces = 1,
        Twos = 2,
        Threes = 3,
        Fours = 4,
        Fives = 5,
        Sixes = 6,

        ThreeOfAKind = 10,
        FourOfAKind = 11,
        FullHouse = 12,
        SmallStraight = 13,
        LargeStraight = 14,
        Chance = 15,
        Tessera = 16,

        BrokenCast = 100
    }
}
