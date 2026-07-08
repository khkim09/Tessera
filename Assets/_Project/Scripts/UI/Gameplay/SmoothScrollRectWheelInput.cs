using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>ScrollRect 마우스 휠 입력을 목표 위치 기반으로 부드럽게 보간한다.</summary>
    [RequireComponent(typeof(ScrollRect))]
    public class SmoothScrollRectWheelInput : MonoBehaviour, IScrollHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Target")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Wheel Smooth")]
        [SerializeField] private bool disableNativeWheelScroll = true;
        [SerializeField] private float wheelStepNormalized = 0.085f;
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float maxWheelDeltaPerEvent = 3f;

        private float targetVerticalNormalizedPosition;
        private float verticalVelocity;
        private bool isWheelAnimating;
        private bool isDragging;

        /// <summary>현재 포인터 드래그로 ScrollRect를 조작 중인지 여부다.</summary>
        public bool IsDragging => isDragging;

        /// <summary>포인터 드래그 스크롤 상태 변경 이벤트다.</summary>
        public event Action<bool> DragStateChanged;

        /// <summary>컴포넌트 추가 시 ScrollRect 참조를 자동 연결한다.</summary>
        private void Reset()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        /// <summary>초기화 시 ScrollRect 참조와 목표 위치를 보정한다.</summary>
        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            targetVerticalNormalizedPosition = ResolveCurrentVerticalPosition();

            if (disableNativeWheelScroll && scrollRect != null)
                scrollRect.scrollSensitivity = 0f;
        }

        /// <summary>활성화 시 현재 ScrollRect 위치를 목표 위치로 동기화한다.</summary>
        private void OnEnable()
        {
            targetVerticalNormalizedPosition = ResolveCurrentVerticalPosition();
            verticalVelocity = 0f;
            isWheelAnimating = false;
            SetDragging(false);
        }

        /// <summary>비활성화 시 드래그 상태를 해제한다.</summary>
        private void OnDisable()
        {
            SetDragging(false);
        }

        /// <summary>매 프레임 목표 스크롤 위치로 부드럽게 이동한다.</summary>
        private void Update()
        {
            if (scrollRect == null)
                return;

            if (isDragging)
                return;

            if (!isWheelAnimating)
                return;

            float currentPosition = scrollRect.verticalNormalizedPosition;
            float nextPosition = Mathf.SmoothDamp(
                currentPosition,
                targetVerticalNormalizedPosition,
                ref verticalVelocity,
                Mathf.Max(0.001f, smoothTime),
                Mathf.Max(0.01f, maxSpeed),
                Time.unscaledDeltaTime);

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(nextPosition);

            if (Mathf.Abs(scrollRect.verticalNormalizedPosition - targetVerticalNormalizedPosition) <= 0.0005f &&
                Mathf.Abs(verticalVelocity) <= 0.0005f)
            {
                scrollRect.verticalNormalizedPosition = targetVerticalNormalizedPosition;
                verticalVelocity = 0f;
                isWheelAnimating = false;
            }
        }

        /// <summary>마우스 휠 입력을 누적 목표 위치로 변환한다.</summary>
        public void OnScroll(PointerEventData eventData)
        {
            if (scrollRect == null || eventData == null)
                return;

            if (!scrollRect.vertical)
                return;

            float wheelDelta = Mathf.Clamp(
                eventData.scrollDelta.y,
                -Mathf.Max(0.01f, maxWheelDeltaPerEvent),
                Mathf.Max(0.01f, maxWheelDeltaPerEvent));

            if (Mathf.Abs(wheelDelta) <= 0.001f)
                return;

            targetVerticalNormalizedPosition = Mathf.Clamp01(
                targetVerticalNormalizedPosition + wheelDelta * Mathf.Max(0.001f, wheelStepNormalized));

            isWheelAnimating = true;
        }

        /// <summary>드래그 시작 시 휠 보간을 중단하고 ScrollRect 기본 드래그를 우선한다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            isWheelAnimating = false;
            verticalVelocity = 0f;
            targetVerticalNormalizedPosition = ResolveCurrentVerticalPosition();
            SetDragging(true);
        }

        /// <summary>드래그 종료 시 현재 위치를 목표 위치로 동기화한다.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            SetDragging(false);
            targetVerticalNormalizedPosition = ResolveCurrentVerticalPosition();
        }

        /// <summary>드래그 상태를 갱신하고 변경 이벤트를 발생시킨다.</summary>
        private void SetDragging(bool dragging)
        {
            if (isDragging == dragging)
                return;

            isDragging = dragging;
            DragStateChanged?.Invoke(isDragging);
        }

        /// <summary>현재 ScrollRect의 Vertical Normalized Position을 반환한다.</summary>
        private float ResolveCurrentVerticalPosition()
        {
            if (scrollRect == null)
                return 1f;

            return Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }
}
