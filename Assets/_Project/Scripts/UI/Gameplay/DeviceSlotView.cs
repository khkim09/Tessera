using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>상단 Device 바에서 하나의 SlotPair Device 슬롯 표시, Hover Tooltip, 드래그 재정렬을 관리한다.</summary>
    public class DeviceSlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private DeviceTooltipView tooltipView;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.12f, 0.16f, 0.22f, 0.9f);
        [SerializeField] private Color equippedColor = new Color(0.18f, 0.28f, 0.45f, 0.95f);
        [SerializeField] private Color dragDimColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

        [Header("Icon")]
        [SerializeField] private bool hideIconWhenEmpty = true;

        private SlotPairDeviceDefinitionSO currentDevice;
        private bool isPointerInside;
        private int slotIndex;
        private IDeviceSlotReorderHandler reorderHandler;
        private bool isDragging;

        /// <summary>현재 슬롯에 표시 중인 Device SO를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

        /// <summary>현재 슬롯 인덱스를 반환한다.</summary>
        public int SlotIndex => slotIndex;

        /// <summary>슬롯 인덱스와 재정렬 핸들러를 설정한다. Awake/Start 이후에 호출한다.</summary>
        public void Initialize(int slotIndex, IDeviceSlotReorderHandler reorderHandler)
        {
            this.slotIndex = slotIndex;
            this.reorderHandler = reorderHandler;
        }

        /// <summary>지정한 Device 정보를 UI에 반영한다.</summary>
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

        /// <summary>마우스가 Device 슬롯에 들어왔을 때 Tooltip을 표시한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;

            if (tooltipView == null || currentDevice == null)
                return;

            tooltipView.Show(currentDevice, eventData.position);
        }

        /// <summary>마우스가 Device 슬롯에서 나갔을 때 Tooltip을 숨긴다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;

            if (tooltipView == null)
                return;

            tooltipView.Hide();
        }

        /// <summary>마우스가 Device 슬롯 위에서 움직일 때 Tooltip 위치를 갱신한다.</summary>
        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isPointerInside || tooltipView == null || currentDevice == null)
                return;

            tooltipView.Move(eventData.position);
        }

        /// <summary>드래그 시작 시 슬롯을 약간 어둡게 표시한다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (reorderHandler == null) return;

            isDragging = true;

            // 드래그 중에는 Tooltip을 숨긴다.
            if (tooltipView != null)
                tooltipView.Hide();

            // 배경을 어둡게 하여 드래그 중임을 시각적으로 알린다.
            if (backgroundImage != null)
                backgroundImage.color = dragDimColor;
        }

        /// <summary>드래그 중에는 아무것도 하지 않는다. (고스트 미구현)</summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 고스트 오브젝트 없이 기본 드래그만 허용한다.
        }

        /// <summary>드래그 종료 시 현재 Device 상태 기준으로 슬롯 표시를 다시 갱신한다.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // Drop 과정에서 Presenter가 이미 Device 데이터를 Swap했을 수 있으므로,
            // 드래그 시작 전 색상으로 되돌리지 않고 현재 Device 상태 기준으로 다시 그린다.
            SetDevice(currentDevice);
        }

        /// <summary>다른 DeviceSlotView 위에 드롭되었을 때 Swap을 요청한다.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (reorderHandler == null)
                return;

            // 드래그 중인 오브젝트에서 DeviceSlotView 컴포넌트를 찾는다.
            DeviceSlotView sourceView = eventData.pointerDrag?.GetComponent<DeviceSlotView>();

            if (sourceView == null)
                return;

            // 같은 슬롯이면 무시한다.
            if (sourceView.slotIndex == this.slotIndex)
                return;

            // Swap 요청을 Presenter로 전달한다.
            reorderHandler.RequestDeviceSlotSwap(sourceView.slotIndex, this.slotIndex);
        }

        /// <summary>빈 Device 슬롯 상태로 표시한다.</summary>
        private void SetEmpty()
        {
            // 빈 슬롯은 장착되지 않은 카드 자리처럼 어둡게 처리한다.
            SetImageColor(backgroundImage, emptyColor);
            SetText(nameText, "-");
            SetText(descriptionText, string.Empty);
            SetIcon(null, false);

            if (isPointerInside && tooltipView != null)
                tooltipView.Hide();
        }

        /// <summary>장착된 Device 슬롯 상태로 표시한다.</summary>
        private void SetEquipped(SlotPairDeviceDefinitionSO device)
        {
            // 장착 슬롯은 배경 색상과 아이콘을 통해 현재 장착 상태를 보여준다.
            SetImageColor(backgroundImage, equippedColor);
            SetText(nameText, device.DisplayName);
            SetText(descriptionText, string.Empty);
            SetIcon(device.Icon, true);
        }

        /// <summary>아이콘 이미지를 지정하고 표시 여부를 갱신한다.</summary>
        private void SetIcon(Sprite iconSprite, bool hasDevice)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = iconSprite;

            if (iconSprite != null)
            {
                iconImage.enabled = true;
                return;
            }

            // 아이콘이 없는 장착 Device는 임시로 흰 슬롯 표시가 가능하도록 둔다.
            iconImage.enabled = hasDevice || !hideIconWhenEmpty;
        }

        /// <summary>텍스트 참조가 있을 때만 문자열을 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        /// <summary>이미지 참조가 있을 때만 색상을 갱신한다.</summary>
        private static void SetImageColor(Image targetImage, Color color)
        {
            if (targetImage == null)
                return;

            targetImage.color = color;
        }
    }
}


