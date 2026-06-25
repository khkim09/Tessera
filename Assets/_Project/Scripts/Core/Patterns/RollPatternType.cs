namespace Tessera.Core
{
    /// <summary>Cast Board에서 선택할 수 있는 주사위 카테고리 종류를 정의한다.</summary>
    public enum RollPatternType
    {
        None = 0, // 유효한 Cast가 없는 상태다.

        Aces = 1, // 1 눈금 Cast다.
        Twos = 2, // 2 눈금 Cast다.
        Threes = 3, // 3 눈금 Cast다.
        Fours = 4, // 4 눈금 Cast다.
        Fives = 5, // 5 눈금 Cast다.
        Sixes = 6, // 6 눈금 Cast다.

        ThreeOfAKind = 10, // 같은 눈금 3개 Cast다.
        FourOfAKind = 11, // 같은 눈금 4개 Cast다.
        FullHouse = 12, // 풀하우스 Cast다.
        SmallStraight = 13, // 작은 스트레이트 Cast다.
        LargeStraight = 14, // 큰 스트레이트 Cast다.
        Chance = 15, // 찬스 Cast다.
        Tessera = 16, // 테세라 Cast다.

        BrokenCast = 100 // 방어용 Broken Cast다.
    }
}
