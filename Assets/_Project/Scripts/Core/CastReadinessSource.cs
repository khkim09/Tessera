namespace Tessera.Core
{
    /// <summary>현재 Attempt가 Cast 제출 가능 상태가 된 원인을 정의한다.</summary>
    public enum CastReadinessSource
    {
        None = 0,
        RollPerformed = 1,
        ForcedDiceFaceSelection = 2,
        DeviceGeneratedDiceState = 3,
        DebugBypass = 100
    }
}
