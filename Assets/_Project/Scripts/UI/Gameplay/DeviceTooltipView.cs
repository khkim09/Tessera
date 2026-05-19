using Tessera.Data;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Device 슬롯에 마우스를 올렸을 때 표시되는 Tooltip UI를 관리한다.</summary>
    public class DeviceTooltipView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text effectText;

        [Header("Position")]
        [SerializeField] private Vector2 screenOffset = new Vector2(18f, -18f);

        private Canvas parentCanvas;
        private RectTransform canvasRectTransform;

        /// <summary>Tooltip 초기 참조를 준비하고 기본적으로 숨긴다.</summary>
        private void Awake()
        {
            if (root == null)
                root = transform as RectTransform;

            parentCanvas = GetComponentInParent<Canvas>();

            if (parentCanvas != null)
                canvasRectTransform = parentCanvas.transform as RectTransform;

            Hide();
        }

        /// <summary>지정한 Device 정보를 Tooltip에 표시한다.</summary>
        public void Show(SlotPairDeviceDefinitionSO device, Vector2 screenPosition)
        {
            if (device == null)
            {
                Hide();
                return;
            }

            SetText(titleText, device.DisplayName);
            SetText(descriptionText, device.Description);
            SetText(effectText, BuildEffectText(device));

            SetVisible(true);
            MoveToScreenPosition(screenPosition);
        }

        /// <summary>Tooltip 위치를 현재 마우스 위치 기준으로 갱신한다.</summary>
        public void Move(Vector2 screenPosition)
        {
            if (root == null || !root.gameObject.activeSelf)
                return;

            MoveToScreenPosition(screenPosition);
        }

        /// <summary>Tooltip을 숨긴다.</summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>Tooltip 표시 여부를 변경한다.</summary>
        private void SetVisible(bool isVisible)
        {
            if (root == null)
                return;

            root.gameObject.SetActive(isVisible);
        }

        /// <summary>화면 좌표를 Canvas 로컬 좌표로 변환해 Tooltip 위치를 갱신한다.</summary>
        private void MoveToScreenPosition(Vector2 screenPosition)
        {
            if (root == null || canvasRectTransform == null)
                return;

            Vector2 localPoint;
            Vector2 targetScreenPosition = screenPosition + screenOffset;

            // Overlay Canvas 기준 마우스 좌표를 로컬 좌표로 변환한다.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                targetScreenPosition,
                parentCanvas != null ? parentCanvas.worldCamera : null,
                out localPoint);

            root.anchoredPosition = localPoint;
        }

        /// <summary>Device 효과 정보를 테스트용 한 줄 설명으로 만든다.</summary>
        private static string BuildEffectText(SlotPairDeviceDefinitionSO device)
        {
            if (device == null)
                return string.Empty;

            if (device.DeviceType == Core.SlotPairDeviceType.AddScoreByDiceValue)
                return $"Effect: Score + Dice x {device.IntValue}";

            if (device.DeviceType == Core.SlotPairDeviceType.AddForceIfDiceIncluded)
                return $"Effect: Force +{device.IntValue} if dice is included";

            if (device.DeviceType == Core.SlotPairDeviceType.AddForceIfSameAsPrevious)
                return $"Effect: Force +{device.IntValue} if same as previous";

            if (device.DeviceType == Core.SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast)
                return $"Effect: Force x{device.FloatValue} if Force >= {device.ForceThreshold}";

            if (device.DeviceType == Core.SlotPairDeviceType.AddScoreIfCastType)
                return $"Effect: Score +{device.IntValue} if Cast is {device.RequiredPatternType}";

            return "Effect: None";
        }

        /// <summary>텍스트 참조가 있을 때만 문자열을 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}