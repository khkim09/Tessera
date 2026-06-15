using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>DeviceSlot에 장착된 Device 카드형 3D 표시를 갱신한다.</summary>
    public class EquippedDevice3DView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text debugText;

        [Header("Canvas")]
        [SerializeField] private bool assignMainCameraToWorldSpaceCanvas = true;

        [Header("Display")]
        [SerializeField] private bool hideIconWhenEmpty = true;
        [SerializeField] private bool applyBodyColorOnBind = true;
        [SerializeField] private Color normalBodyColor = new Color(0.18f, 0.25f, 0.42f, 1f);
        [SerializeField] private Color highlightedBodyColor = new Color(0.85f, 0.72f, 0.25f, 1f);

        private Material runtimeBodyMaterial;
        private SlotPairDeviceDefinitionSO currentDevice;

        /// <summary>현재 표시 중인 Device 정의를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

        /// <summary>컴포넌트 추가 시 기본 참조를 자동 수집한다.</summary>
        private void Reset()
        {
            bodyRenderer = GetComponentInChildren<MeshRenderer>(true);
            iconImage = GetComponentInChildren<Image>(true);
            debugText = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>초기화 시 World Space Canvas 카메라 참조를 보정한다.</summary>
        private void Awake()
        {
            AssignCanvasEventCamerasIfNeeded();
            RefreshBodyColor(false);
        }

        /// <summary>활성화 시 World Space Canvas 카메라 참조와 Body 색상을 보정한다.</summary>
        private void OnEnable()
        {
            AssignCanvasEventCamerasIfNeeded();
            RefreshBodyColor(false);
        }

        /// <summary>Device 정의를 카드형 장착 View에 반영한다.</summary>
        public void Bind(SlotPairDeviceDefinitionSO device)
        {
            currentDevice = device;

            RefreshIcon();
            RefreshDebugText();
            RefreshBodyColor(false);
            AssignCanvasEventCamerasIfNeeded();
        }

        /// <summary>장착 Device 카드의 강조 상태를 갱신한다.</summary>
        public void SetHighlighted(bool isHighlighted)
        {
            RefreshBodyColor(isHighlighted);
        }

        /// <summary>Device 아이콘 표시를 갱신한다.</summary>
        private void RefreshIcon()
        {
            if (iconImage == null)
                return;

            Sprite icon = currentDevice != null ? currentDevice.Icon : null;
            iconImage.sprite = icon;

            if (hideIconWhenEmpty)
                iconImage.gameObject.SetActive(icon != null);
        }

        /// <summary>프로토타입 디버그 텍스트 표시를 갱신한다.</summary>
        private void RefreshDebugText()
        {
            if (debugText == null)
                return;

            debugText.text = currentDevice != null ? currentDevice.DisplayName : string.Empty;
        }

        /// <summary>Body Mesh 색상을 현재 표시 상태에 맞게 갱신한다.</summary>
        private void RefreshBodyColor(bool isHighlighted)
        {
            if (!applyBodyColorOnBind)
                return;

            Color targetColor = isHighlighted ? highlightedBodyColor : normalBodyColor;
            SetBodyColor(targetColor);
        }

        /// <summary>장착 Device 카드의 Body 색상을 갱신한다.</summary>
        public void SetBodyColor(Color color)
        {
            if (bodyRenderer == null)
                return;

            EnsureRuntimeBodyMaterial();

            if (runtimeBodyMaterial == null)
                return;

            runtimeBodyMaterial.color = color;
        }

        /// <summary>Body Mesh의 런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeBodyMaterial()
        {
            if (bodyRenderer == null)
                return;

            if (runtimeBodyMaterial != null)
                return;

            runtimeBodyMaterial = bodyRenderer.material;
        }

        /// <summary>하위 World Space Canvas에 MainCamera를 연결한다.</summary>
        private void AssignCanvasEventCamerasIfNeeded()
        {
            if (!assignMainCameraToWorldSpaceCanvas)
                return;

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null)
                    continue;

                if (canvases[i].renderMode != RenderMode.WorldSpace)
                    continue;

                if (canvases[i].worldCamera != null)
                    continue;

                canvases[i].worldCamera = mainCamera;
            }
        }
    }
}
