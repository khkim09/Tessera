namespace Tessera.Core
{
    /// <summary>Stage Round의 진행 완료 상태를 정의한다.</summary>
    public enum StageRoundCompletionType
    {
        NotStarted = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3
    }
}
