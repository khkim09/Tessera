using UnityEngine;

namespace Tessera.Presentation
{
    /// <summary>
    /// Hover 대상 오브젝트의 Layer를 일시적으로 Highlight Layer로 변경하고,
    /// 필요하면 약한 Hover Motion을 재생하는 컴포넌트다.
    /// </summary>
    public class TesseraHoverHighlightTarget : MonoBehaviour
    {
        [Header("Scale")]
        [SerializeField] private Transform scaleRoot;
        [SerializeField] private float hoverScaleMultiplier = 1.2f;
        [SerializeField] private bool hoverFeedbackEnabled = true;

        private bool isInitialized;
        private bool isHighlighted;
        private Vector3 originalLocalScale;

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

        /// <summary>Hover 피드백 허용 여부를 설정한다.</summary>
        public void SetHoverFeedbackEnabled(bool enabled)
        {
            hoverFeedbackEnabled = enabled;

            if (!hoverFeedbackEnabled)
                SetHighlighted(false);
        }

        /// <summary>현재 Hover 허용 상태에서 Hover Scale을 즉시 적용한다.</summary>
        public void ForceApplyHoverHighlight()
        {
            if (!hoverFeedbackEnabled)
            {
                ResetHighlight();
                return;
            }

            SetHighlighted(true);
        }

        /// <summary>Hover Scale 표시 상태를 변경한다.</summary>
        public void SetHighlighted(bool highlighted)
        {
            InitializeIfNeeded();

            if (!isInitialized) return;
            if (!hoverFeedbackEnabled && highlighted) return;
            if (isHighlighted == highlighted) return;

            isHighlighted = highlighted;

            ApplyScaleState(highlighted);
        }

        /// <summary>Highlight를 강제로 해제한다.</summary>
        public void ResetHighlight()
        {
            SetHighlighted(false);
        }

        /// <summary>필요한 Scale 캐시를 지연 초기화한다.</summary>
        private void InitializeIfNeeded()
        {
            if (isInitialized)
                return;

            if (scaleRoot == null)
                scaleRoot = transform;

            originalLocalScale = scaleRoot.localScale;
            isInitialized = true;
        }

        /// <summary>Highlight 여부에 따라 Scale 피드백을 적용한다.</summary>
        private void ApplyScaleState(bool highlighted)
        {
            if (scaleRoot == null)
                return;

            scaleRoot.localScale = highlighted
                ? originalLocalScale * hoverScaleMultiplier
                : originalLocalScale;
        }
    }
}
