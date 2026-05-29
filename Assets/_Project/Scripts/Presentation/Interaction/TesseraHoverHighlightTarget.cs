using UnityEngine;

namespace Tessera.Presentation
{
    /// <summary>
    /// Hover 대상 오브젝트의 Layer를 일시적으로 Highlight Layer로 변경하고,
    /// 필요하면 약한 Hover Motion을 재생하는 컴포넌트다.
    /// </summary>
    public class TesseraHoverHighlightTarget : MonoBehaviour
    {
        [Header("Outline")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private LayerMask highlightLayerMask;
        [SerializeField] private bool includeInactiveRenderers = true;

        [Header("Hover Motion")]
        [SerializeField] private bool useHoverMotion;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private float shakePositionAmount = 0.015f;
        [SerializeField] private float shakeRotationAmount = 1.2f;
        [SerializeField] private float shakeSpeed = 18f;

        private GameObject[] layerTargets;
        private int[] originalLayers;
        private int highlightLayer = -1;
        private bool isInitialized;
        private bool isHighlighted;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private float hoverTime;

        /// <summary>컴포넌트 초기화 시 Renderer와 원본 Layer 정보를 캐싱한다.</summary>
        private void Awake()
        {
            InitializeIfNeeded();
        }

        /// <summary>비활성화 시 Highlight와 Motion을 원상 복구한다.</summary>
        private void OnDisable()
        {
            SetHighlighted(false);
        }

        /// <summary>Hover Motion이 켜져 있고 Highlight 중이면 약한 흔들림을 적용한다.</summary>
        private void Update()
        {
            if (!isHighlighted || !useHoverMotion || motionRoot == null)
                return;

            hoverTime += Time.unscaledDeltaTime * shakeSpeed;

            float positionOffset = Mathf.Sin(hoverTime) * shakePositionAmount;
            float rotationOffset = Mathf.Sin(hoverTime * 1.37f) * shakeRotationAmount;

            motionRoot.localPosition = originalLocalPosition + new Vector3(0f, positionOffset, 0f);
            motionRoot.localRotation = originalLocalRotation * Quaternion.Euler(0f, rotationOffset, 0f);
        }

        /// <summary>Highlight 표시 상태를 변경한다.</summary>
        public void SetHighlighted(bool highlighted)
        {
            InitializeIfNeeded();

            if (!isInitialized)
                return;

            if (isHighlighted == highlighted)
                return;

            isHighlighted = highlighted;

            ApplyLayerState(highlighted);

            if (!highlighted)
                ResetMotion();
            else
                CacheMotionOrigin();
        }

        /// <summary>Highlight를 강제로 해제한다.</summary>
        public void ResetHighlight()
        {
            SetHighlighted(false);
        }

        /// <summary>필요한 캐시를 지연 초기화한다.</summary>
        private void InitializeIfNeeded()
        {
            if (isInitialized)
                return;

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);

            highlightLayer = ResolveSingleLayerIndex(highlightLayerMask);

            if (highlightLayer < 0)
            {
                Debug.LogWarning($"[Tessera][HoverHighlight] Invalid highlight layer mask on {name}. Select exactly one layer.", this);
                return;
            }

            BuildLayerTargetCache();

            if (motionRoot == null)
                motionRoot = transform;

            CacheMotionOrigin();

            isInitialized = true;
        }

        /// <summary>Renderer GameObject와 Root GameObject의 원본 Layer를 캐싱한다.</summary>
        private void BuildLayerTargetCache()
        {
            int rendererCount = targetRenderers != null ? targetRenderers.Length : 0;
            layerTargets = new GameObject[rendererCount + 1];
            originalLayers = new int[rendererCount + 1];

            layerTargets[0] = gameObject;
            originalLayers[0] = gameObject.layer;

            for (int i = 0; i < rendererCount; i++)
            {
                GameObject targetObject = targetRenderers[i] != null
                    ? targetRenderers[i].gameObject
                    : null;

                layerTargets[i + 1] = targetObject;
                originalLayers[i + 1] = targetObject != null ? targetObject.layer : 0;
            }
        }

        /// <summary>현재 Hover Motion 기준 위치와 회전을 저장한다.</summary>
        private void CacheMotionOrigin()
        {
            if (motionRoot == null)
                return;

            originalLocalPosition = motionRoot.localPosition;
            originalLocalRotation = motionRoot.localRotation;
            hoverTime = 0f;
        }

        /// <summary>Highlight 여부에 따라 캐싱된 대상들의 Layer를 변경한다.</summary>
        private void ApplyLayerState(bool highlighted)
        {
            int targetLayer;

            for (int i = 0; i < layerTargets.Length; i++)
            {
                if (layerTargets[i] == null)
                    continue;

                targetLayer = highlighted ? highlightLayer : originalLayers[i];
                layerTargets[i].layer = targetLayer;
            }
        }

        /// <summary>Hover Motion을 원래 Transform 상태로 되돌린다.</summary>
        private void ResetMotion()
        {
            if (motionRoot == null)
                return;

            motionRoot.localPosition = originalLocalPosition;
            motionRoot.localRotation = originalLocalRotation;
            hoverTime = 0f;
        }

        /// <summary>단일 LayerMask에서 Layer Index를 추출한다. 단일 Layer가 아니면 -1을 반환한다.</summary>
        private static int ResolveSingleLayerIndex(LayerMask layerMask)
        {
            int value = layerMask.value;

            if (value == 0)
                return -1;

            if ((value & (value - 1)) != 0)
                return -1;

            int layerIndex = 0;

            while (value > 1)
            {
                value >>= 1;
                layerIndex++;
            }

            return layerIndex;
        }
    }
}
