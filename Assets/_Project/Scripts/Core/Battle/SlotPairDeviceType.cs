namespace Tessera.Core
{
    /// <summary>LockSlot과 1:1 대응되는 Device 효과 종류를 정의한다.</summary>
    public enum SlotPairDeviceType
    {
        None = 0,
        AddScoreByDiceValue = 1,
        AddForceIfDiceIncluded = 2,
        AddForceIfSameAsPrevious = 3,
        MultiplyForceIfCurrentForceAtLeast = 4,
        AddScoreIfCastType = 5
    }
}
