namespace Tessera.UI
{
    /// <summary>Device 슬롯 재정렬 요청을 처리하기 위한 콜백 인터페이스.</summary>
    public interface IDeviceSlotReorderHandler
    {
        /// <summary>sourceSlotIndex의 Device를 targetSlotIndex의 Device와 교체한다.</summary>
        void RequestDeviceSlotSwap(int sourceSlotIndex, int targetSlotIndex);
    }
}
