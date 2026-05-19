namespace Tessera.Core
{
    /// <summary>Cast Board 한 행의 현재 표시 상태를 정의한다.</summary>
    public enum CastBoardEntryStatus
    {
        Available = 0,
        Used = 1,
        ConditionNotMet = 2,
        BlockedByTableRule = 3
    }
}
