namespace Tessera.Core
{
    /// <summary>Round 시작 시 어느 쪽이 먼저 행동하는지 정의한다.</summary>
    public enum FirstTurnPolicy
    {
        /// <summary>플레이어가 먼저 행동한다.</summary>
        PlayerFirst = 0,

        /// <summary>상대가 먼저 행동한다.</summary>
        OpponentFirst = 1
    }
}
