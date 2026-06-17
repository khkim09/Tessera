using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>자식 Collider의 Pointer 이벤트를 부모 DeviceSlot3DView로 전달하는 Relay다.</summary>
    public class DeviceSlotClickRelay3D : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        /// <summary>Pointer 이벤트를 전달받을 소유 DeviceSlot이다.</summary>
        private DeviceSlot3DView owner;

        /// <summary>Relay 소유 슬롯을 연결한다.</summary>
        public void Bind(DeviceSlot3DView slot)
        {
            owner = slot;
        }

        /// <summary>Relay 소유 슬롯 연결을 해제한다.</summary>
        public void Unbind(DeviceSlot3DView slot)
        {
            if (owner == slot)
                owner = null;
        }

        /// <summary>Pointer Enter 이벤트를 슬롯 Hover 진입으로 전달한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Collider hover 진입을 DeviceSlot hover 이벤트로 변환한다.
            owner?.NotifySlotHoverEntered();
        }

        /// <summary>Pointer Exit 이벤트를 슬롯 Hover 이탈로 전달한다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // Collider hover 이탈을 DeviceSlot hover 이벤트로 변환한다.
            owner?.NotifySlotHoverExited();
        }

        /// <summary>Begin Drag 이벤트를 슬롯 드래그 시작으로 전달한다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.NotifySlotDragStarted(eventData);
        }

        /// <summary>Drag 진행 이벤트를 슬롯 드래그 이동으로 전달한다.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            owner?.NotifySlotDragged(eventData);
        }

        /// <summary>End Drag 이벤트를 슬롯 드래그 종료로 전달한다.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.NotifySlotDragEnded(eventData);
        }

        /// <summary>Drop 이벤트를 슬롯 Drop 대상으로 전달한다.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            // Collider drop을 DeviceSlot drop 이벤트로 변환한다.
            owner?.NotifySlotDropped();
        }
    }
}
