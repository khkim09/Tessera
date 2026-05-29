namespace Tessera.Runtime
{
    /// <summary>현재 게임이 표시하거나 처리 중인 주요 화면 모드를 정의한다.</summary>
    public enum GameModeType
    {
        None = 0,
        RoundSelect = 1,
        Gameplay = 2,
        Shop = 3,
        BountyBoard = 4,
        RewardDecision = 5,
        Result = 6,
        RoundFailureDecision = 7
    }
}
