using System;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 창의 Cast 한 줄 UI를 표시한다.</summary>
    public class RunInfoCastBookEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text castNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text forceText;
        [SerializeField] private TMP_Text baseImpactText;
        [SerializeField] private TMP_Text remainingUseText;

        [Header("State Visuals")]
        [SerializeField] private GameObject unavailableOverlay;

        [Header("Hover Highlight")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.86f, 0.18f, 1f);
        [SerializeField] private Vector2 hoverOutlineDistance = new Vector2(3f, -3f);

        /// <summary>현재 Entry에 바인딩된 Cast 표시 스냅샷이다.</summary>
        private RunInfoCastBookEntrySnapshot snapshot;

        /// <summary>현재 포인터가 Entry 위에 있는지 여부다.</summary>
        private bool isPointerHovering;

        /// <summary>외부 Scroll 드래그 때문에 Hover 입력을 막는 중인지 여부다.</summary>
        private bool isHoverInputSuppressed;

        /// <summary>Hover 강조용 Outline 컴포넌트다.</summary>
        private Outline hoverOutline;

        /// <summary>Tooltip 배치 기준으로 사용할 RectTransform이다.</summary>
        public RectTransform RectTransform => transform as RectTransform;

        /// <summary>Tooltip 표시 확장용 Hover 시작 이벤트다.</summary>
        public event Action<RunInfoCastBookEntryView, RunInfoCastBookEntrySnapshot> HoverStarted;

        /// <summary>Tooltip 표시 확장용 Hover 종료 이벤트다.</summary>
        public event Action<RunInfoCastBookEntryView, RunInfoCastBookEntrySnapshot> HoverEnded;

        /// <summary>컴포넌트 추가 시 Hover Highlight 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            AssignHoverReferencesIfMissing();
        }

        /// <summary>초기화 시 Hover Highlight 참조를 보정한다.</summary>
        private void Awake()
        {
            AssignHoverReferencesIfMissing();
            SetHoverHighlight(false);
        }

        /// <summary>비활성화 시 Hover 종료 이벤트와 강조 표시를 정리한다.</summary>
        private void OnDisable()
        {
            if (isPointerHovering && snapshot != null)
                HoverEnded?.Invoke(this, snapshot);

            isPointerHovering = false;
            SetHoverHighlight(false);
        }

        /// <summary>Entry에 표시할 Cast 스냅샷을 반영한다.</summary>
        public void Bind(RunInfoCastBookEntrySnapshot newSnapshot)
        {
            snapshot = newSnapshot;
            isPointerHovering = false;
            SetHoverHighlight(false);

            if (snapshot == null)
            {
                SetText(castNameText, "-");
                SetText(scoreText, "0");
                SetText(forceText, "0");
                SetText(baseImpactText, "0");
                SetText(remainingUseText, "0");
                SetUnavailableOverlay(false);
                return;
            }

            SetText(castNameText, snapshot.CastName);
            SetText(scoreText, snapshot.Score.ToString());
            SetText(forceText, snapshot.ForceText);
            SetText(baseImpactText, snapshot.BaseImpactText);
            SetText(remainingUseText, snapshot.RemainingUseText);
            SetUnavailableOverlay(snapshot.IsUnavailable);
        }

        /// <summary>Tooltip용 Cast 타입을 반환한다.</summary>
        public RollPatternType GetPatternType()
        {
            return snapshot != null ? snapshot.PatternType : RollPatternType.None;
        }

        /// <summary>외부 Scroll 드래그 중 Hover 입력 차단 상태를 적용한다.</summary>
        public void SetHoverInputSuppressed(bool suppressed)
        {
            if (isHoverInputSuppressed == suppressed)
                return;

            isHoverInputSuppressed = suppressed;

            if (isHoverInputSuppressed)
            {
                SetHoverHighlight(false);
                return;
            }

            if (isPointerHovering)
                SetHoverHighlight(true);
        }

        /// <summary>포인터 진입 시 Hover 강조와 Tooltip 확장 이벤트를 발생시킨다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (snapshot == null)
                return;

            isPointerHovering = true;

            if (isHoverInputSuppressed)
                return;

            SetHoverHighlight(true);
            HoverStarted?.Invoke(this, snapshot);
        }

        /// <summary>포인터 이탈 시 Hover 강조와 Tooltip 종료 이벤트를 발생시킨다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (snapshot == null)
                return;

            isPointerHovering = false;
            SetHoverHighlight(false);
            HoverEnded?.Invoke(this, snapshot);
        }

        /// <summary>사용 불가 Overlay 활성 상태를 적용한다.</summary>
        private void SetUnavailableOverlay(bool active)
        {
            if (unavailableOverlay == null)
                return;

            unavailableOverlay.SetActive(active);
        }

        /// <summary>Hover Highlight용 UI 참조를 자동 보정한다.</summary>
        private void AssignHoverReferencesIfMissing()
        {
            if (backgroundImage == null)
            {
                Transform backgroundTransform = transform.Find("Background");

                if (backgroundTransform != null)
                    backgroundImage = backgroundTransform.GetComponent<Image>();
            }

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (backgroundImage == null)
                return;

            hoverOutline = backgroundImage.GetComponent<Outline>();

            if (hoverOutline == null)
                hoverOutline = backgroundImage.gameObject.AddComponent<Outline>();

            hoverOutline.effectColor = hoverOutlineColor;
            hoverOutline.effectDistance = hoverOutlineDistance;
            hoverOutline.useGraphicAlpha = false;
            hoverOutline.enabled = false;
        }

        /// <summary>Hover Highlight 표시 상태를 적용한다.</summary>
        private void SetHoverHighlight(bool active)
        {
            if (hoverOutline == null)
                AssignHoverReferencesIfMissing();

            if (hoverOutline != null)
                hoverOutline.enabled = active;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}
