using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D RunInfo 족보 버튼의 Pointer 입력을 처리한다.</summary>
    public class RunInfoCastBookButton3DView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover")]
        [SerializeField] private Transform scaleTarget;
        [SerializeField] private float hoverScaleMultiplier = 1.1f;

        private Vector3 defaultScale;
        private bool hasDefaultScale;

        /// <summary>RunInfo 버튼 클릭 이벤트다.</summary>
        public event Action Clicked;

        /// <summary>컴포넌트 추가 시 Scale 대상 참조를 보정한다.</summary>
        private void Reset()
        {
            scaleTarget = transform;
        }

        /// <summary>기본 Scale 값을 캐싱한다.</summary>
        private void Awake()
        {
            CacheDefaultScale();
        }

        /// <summary>비활성화 시 Hover Scale을 원복한다.</summary>
        private void OnDisable()
        {
            ApplyHoverScale(false);
        }

        /// <summary>Pointer Click을 RunInfo 열기 요청으로 변환한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            Clicked?.Invoke();
        }

        /// <summary>Pointer 진입 시 버튼 Hover Scale을 적용한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            ApplyHoverScale(true);
        }

        /// <summary>Pointer 이탈 시 버튼 Hover Scale을 원복한다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyHoverScale(false);
        }

        /// <summary>Hover Scale 표시 상태를 적용한다.</summary>
        private void ApplyHoverScale(bool isHovering)
        {
            Transform target = ResolveScaleTarget();

            if (target == null)
                return;

            CacheDefaultScale();

            target.localScale = isHovering
                ? defaultScale * Mathf.Max(1f, hoverScaleMultiplier)
                : defaultScale;
        }

        /// <summary>기본 Scale 값을 한 번만 캐싱한다.</summary>
        private void CacheDefaultScale()
        {
            if (hasDefaultScale)
                return;

            Transform target = ResolveScaleTarget();

            if (target == null)
                return;

            defaultScale = target.localScale;
            hasDefaultScale = true;
        }

        /// <summary>Scale 적용 대상 Transform을 반환한다.</summary>
        private Transform ResolveScaleTarget()
        {
            return scaleTarget != null ? scaleTarget : transform;
        }
    }
}
