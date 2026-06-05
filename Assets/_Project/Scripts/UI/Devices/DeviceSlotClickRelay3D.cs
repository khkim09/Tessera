using UnityEngine;

namespace Tessera.UI
{
    /// <summary>자식 Collider 클릭을 부모 DeviceSlot3DView로 전달하는 Relay다.</summary>
    public class DeviceSlotClickRelay3D : MonoBehaviour
    {
        private DeviceSlot3DView owner;

        /// <summary>클릭 전달 대상 슬롯 View를 설정한다.</summary>
        public void Bind(DeviceSlot3DView targetOwner)
        {
            owner = targetOwner;
        }

        /// <summary>Collider 클릭을 부모 DeviceSlot3DView로 전달한다.</summary>
        private void OnMouseDown()
        {
            if (owner == null)
                owner = GetComponentInParent<DeviceSlot3DView>();

            if (owner == null)
                return;

            owner.NotifySlotClicked();
        }
    }
}
