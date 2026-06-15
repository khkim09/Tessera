using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tessera.Presentation
{
    /// <summary>
    /// 마우스 Raycast로 Hover 중인 TesseraHoverHighlightTarget을 찾아 Highlight를 갱신한다.
    /// Unity New Input System 기준으로 동작한다.
    /// </summary>
    public class TesseraHoverHighlightRaycaster : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Raycast")]
        [SerializeField] private LayerMask hoverLayerMask = ~0;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool blockWhenPointerOverUI = true; // UI 아래 있는 3D 오브젝트 Hover 차단 여부

        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
        private TesseraHoverHighlightTarget currentTarget;
        private PointerEventData pointerEventData;

        /// <summary>카메라 참조를 보정한다.</summary>
        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        /// <summary>매 프레임 마우스 Hover 대상을 갱신한다.</summary>
        private void Update()
        {
            if (ShouldBlockByUI())
            {
                SetCurrentTarget(null);
                return;
            }

            TesseraHoverHighlightTarget nextTarget = RaycastHoverTarget();
            SetCurrentTarget(nextTarget);
        }

        /// <summary>비활성화 시 현재 Highlight를 해제한다.</summary>
        private void OnDisable()
        {
            SetCurrentTarget(null);
        }

        /// <summary>현재 Hover Target을 변경하고 이전 Target을 해제한다.</summary>
        private void SetCurrentTarget(TesseraHoverHighlightTarget nextTarget)
        {
            if (currentTarget == nextTarget)
                return;

            if (currentTarget != null)
                currentTarget.SetHighlighted(false);

            currentTarget = nextTarget;

            if (currentTarget != null)
                currentTarget.SetHighlighted(true);
        }

        /// <summary>마우스 위치 기준으로 Hover 가능한 3D 대상을 Raycast한다.</summary>
        private TesseraHoverHighlightTarget RaycastHoverTarget()
        {
            if (targetCamera == null)
                return null;

            if (Mouse.current == null)
                return null;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, hoverLayerMask, triggerInteraction))
                return null;

            return hit.collider.GetComponentInParent<TesseraHoverHighlightTarget>();
        }

        /// <summary>현재 포인터 위치의 UI가 3D Hover를 차단해야 하는지 확인한다.</summary>
        private bool ShouldBlockByUI()
        {
            if (!blockWhenPointerOverUI)
                return false;

            if (EventSystem.current == null)
                return false;

            if (Mouse.current == null)
                return false;

            if (pointerEventData == null)
                pointerEventData = new PointerEventData(EventSystem.current);

            pointerEventData.Reset();
            pointerEventData.position = Mouse.current.position.ReadValue();

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                RaycastResult result = uiRaycastResults[i];

                if (result.module is GraphicRaycaster)
                    return true;
            }

            return false;
        }
    }
}
