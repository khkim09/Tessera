namespace Tessera.Core
{
    /// <summary>주사위 면의 종류를 정의한다.</summary>
    public enum DiceFaceType
    {
        /// <summary>일반 숫자 면 (1~6)</summary>
        Number = 0,

        /// <summary>와일드카드 면 (모든 숫자로 취급 가능)</summary>
        Wild = 1,

        /// <summary>빈 면 (패턴에 기여하지 않음)</summary>
        Blank = 2,

        /// <summary>거울 면 (왼쪽 주사위 값 복사)</summary>
        Mirror = 3,
    }
}
