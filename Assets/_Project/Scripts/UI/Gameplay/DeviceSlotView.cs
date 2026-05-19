using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>상단 Device 바에서 하나의 SlotPair Device 슬롯 표시와 Hover Tooltip을 관리한다.</summary>
    public class DeviceSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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

        [Header("Icon")]
        [SerializeField] private bool hideIconWhenEmpty = true;

        private SlotPairDeviceDefinitionSO currentDevice;
        private bool isPointerInside;

        /// <summary>현재 슬롯에 표시 중인 Device SO를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

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