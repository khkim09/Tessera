using System;
using Tessera.Data;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 하나의 표시 상태를 관리한다.</summary>
    public class DeviceSlot3DView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer slotRenderer;
        [SerializeField] private TMP_Text displayNameText;
        [SerializeField] private TMP_Text indexText;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.42f, 0.46f, 0.50f, 1f);
        [SerializeField] private Color equippedColor = new Color(0.18f, 0.25f, 0.42f, 1f);
        [SerializeField] private Color highlightedColor = new Color(0.85f, 0.72f, 0.25f, 1f);

        private SlotPairDeviceDefinitionSO currentDevice;
        private Material runtimeMaterial;
        private int slotIndex = -1;

        /// <summary>현재 슬롯 인덱스를 반환한다.</summary>
        public int SlotIndex => slotIndex;

        /// <summary>현재 표시 중인 Device를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

        /// <summary>DeviceSlot 클릭 이벤트.</summary>
        public event Action<int> Clicked;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트의 MeshRenderer를 기본 슬롯 렌더러로 사용한다.
            slotRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>런타임 Material을 준비한다.</summary>
        private void Awake()
        {
            // 원본 Material을 오염시키지 않도록 인스턴스를 사용한다.
            EnsureRuntimeMaterial();

            // 자식 Collider 자동 Relay 등록 추가
            RegisterClickRelays();
        }

        /// <summary>마우스 클릭을 슬롯 클릭 이벤트로 전달한다.</summary>
        private void OnMouseDown()
        {
            NotifySlotClicked();
        }

        /// <summary>외부 Collider Relay에서 슬롯 클릭을 전달받는다.</summary>
        public void NotifySlotClicked()
        {
            if (slotIndex < 0)
                return;

            Clicked?.Invoke(slotIndex);
        }

        /// <summary>슬롯 인덱스를 초기화한다.</summary>
        public void Initialize(int slotIndex)
        {
            this.slotIndex = slotIndex;

            // 슬롯 번호는 디버그 단계에서만 작게 표시한다.
            SetText(indexText, (slotIndex + 1).ToString());
        }

        /// <summary>지정한 Device 데이터를 3D 슬롯에 반영한다.</summary>
        public void SetDevice(SlotPairDeviceDefinitionSO device)
        {
            currentDevice = device;

            if (device == null)
            {
                SetEmpty();
                return;
            }

            SetEquipped(device);
        }

        /// <summary>계산 또는 선택 강조 상태를 표시한다.</summary>
        public void SetHighlighted(bool isHighlighted)
        {
            // 계산 중인 SlotPair를 임시 색상으로 강조한다.
            SetSlotColor(isHighlighted ? highlightedColor : GetBaseColor());
        }

        /// <summary>빈 슬롯 상태로 표시한다.</summary>
        private void SetEmpty()
        {
            // 비어 있는 슬롯은 회색 카드 자리로 표시한다.
            SetSlotColor(emptyColor);
            SetText(displayNameText, string.Empty);
        }

        /// <summary>장착된 Device 상태로 표시한다.</summary>
        private void SetEquipped(SlotPairDeviceDefinitionSO device)
        {
            // 장착된 슬롯은 남색 계열로 표시하고 이름을 표시한다.
            SetSlotColor(equippedColor);
            SetText(displayNameText, device.DisplayName);
        }

        /// <summary>현재 상태 기준 기본 색상을 반환한다.</summary>
        private Color GetBaseColor()
        {
            return currentDevice == null ? emptyColor : equippedColor;
        }

        /// <summary>슬롯 렌더러 색상을 변경한다.</summary>
        private void SetSlotColor(Color color)
        {
            if (slotRenderer == null)
                return;

            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
                return;

            runtimeMaterial.color = color;
        }

        /// <summary>런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeMaterial()
        {
            if (slotRenderer == null)
                return;

            if (runtimeMaterial != null)
                return;

            // sharedMaterial 대신 material을 통해 슬롯별 색상 변경을 독립시킨다.
            runtimeMaterial = slotRenderer.material;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        /// <summary>자식 Collider에 클릭 Relay를 자동 등록한다.</summary>
        private void RegisterClickRelays()
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] == null)
                    continue;

                DeviceSlotClickRelay3D relay = childColliders[i].GetComponent<DeviceSlotClickRelay3D>();

                if (relay == null)
                    relay = childColliders[i].gameObject.AddComponent<DeviceSlotClickRelay3D>();

                relay.Bind(this);
            }
        }
    }
}
