using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>ActionButton의 Pointer Enter/Exit를 슬롯 Hover UI에 전달하는 Relay다.</summary>
    public sealed class DeviceHoverActionPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Pointer 상태를 전달할 소유 Hover UI다.</summary>
        [SerializeField] private DeviceSlotHoverActionUIView owner;

        /// <summary>Relay 소유 Hover UI를 연결한다.</summary>
        public void Bind(DeviceSlotHoverActionUIView hoverUI)
        {
            // 버튼 Hover 상태를 지정 Hover UI로 전달하도록 owner를 저장한다.
            owner = hoverUI;
        }

        /// <summary>Relay 소유 Hover UI 연결을 해제한다.</summary>
        public void Unbind(DeviceSlotHoverActionUIView hoverUI)
        {
            // 현재 owner와 일치할 때만 해제해 다른 UI 참조 오염을 막는다.
            if (owner == hoverUI)
                owner = null;
        }

        /// <summary>Pointer Enter 이벤트를 Hover UI 유지 상태로 전달한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 마우스가 버튼 위에 있으면 DeviceSlot 밖으로 나가도 UI를 유지한다.
            owner?.NotifyPointerEntered();
        }

        /// <summary>Pointer Exit 이벤트를 Hover UI 이탈 상태로 전달한다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // 마우스가 버튼 밖으로 나가면 Bridge가 최종 Hide 여부를 판단한다.
            owner?.NotifyPointerExited();
        }
    }
}
