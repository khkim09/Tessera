using System;
using Tessera.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 Play 3D 버튼의 클릭 입력과 상호작용 가능 상태를 관리한다.</summary>
    public class PlayButton3DView : MonoBehaviour
    {
        [Header("Click")]
        [SerializeField] private Collider clickCollider;
        [SerializeField] private bool autoEnableCollider = true;

        [Header("Hover")]
        [SerializeField] private TesseraHoverHighlightTarget hoverHighlightTarget;

        private PlayButtonClickRelay3D clickRelay;
        private bool isInteractable = true;

        /// <summary>Play 버튼 클릭 이벤트다.</summary>
        public event Action Clicked;

        /// <summary>컴포넌트 추가 시 클릭 Collider와 Hover Target을 자동 수집한다.</summary>
        private void Reset()
        {
            clickCollider = GetComponentInChildren<Collider>(true);
            hoverHighlightTarget = GetComponent<TesseraHoverHighlightTarget>();
        }

        /// <summary>런타임 시작 시 Relay와 Collider 상태를 보정한다.</summary>
        private void Awake()
        {
            if (clickCollider == null)
                clickCollider = GetComponentInChildren<Collider>(true);

            if (hoverHighlightTarget == null)
                hoverHighlightTarget = GetComponent<TesseraHoverHighlightTarget>();

            EnsureClickRelay();

            if (autoEnableCollider && clickCollider != null)
                clickCollider.enabled = isInteractable;
        }

        /// <summary>오브젝트 제거 시 Relay 연결을 해제한다.</summary>
        private void OnDestroy()
        {
            if (clickRelay != null)
                clickRelay.Unbind(this);
        }

        /// <summary>Play 버튼 상호작용 가능 여부를 변경한다.</summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (clickCollider != null)
                clickCollider.enabled = interactable;

            if (hoverHighlightTarget != null)
                hoverHighlightTarget.SetHoverFeedbackEnabled(interactable);
        }

        /// <summary>Relay에서 전달받은 클릭 이벤트를 Play 제출 이벤트로 변환한다.</summary>
        public void NotifyClicked(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Clicked?.Invoke();
        }

        /// <summary>클릭 Collider 오브젝트에 Relay를 자동 연결한다.</summary>
        private void EnsureClickRelay()
        {
            if (clickCollider == null)
                return;

            clickRelay = clickCollider.GetComponent<PlayButtonClickRelay3D>();

            if (clickRelay == null)
                clickRelay = clickCollider.gameObject.AddComponent<PlayButtonClickRelay3D>();

            clickRelay.Bind(this);
        }
    }
}
