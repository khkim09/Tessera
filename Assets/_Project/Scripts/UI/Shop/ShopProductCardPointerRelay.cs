using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>ProductCard의 실제 raycast 대상에서 루트 카드 View로 포인터 이벤트를 전달한다.</summary>
    public class ShopProductCardPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ShopProductCardView targetView;

        /// <summary>컴포넌트 추가 시 부모 ProductCard View를 자동 연결한다.</summary>
        private void Reset()
        {
            // ContentRoot 기준 부모 ProductCard에서 View를 찾는다.
            targetView = GetComponentInParent<ShopProductCardView>(true);
        }

        /// <summary>초기화 시 누락된 ProductCard View 참조를 보정한다.</summary>
        private void Awake()
        {
            // Prefab 복제나 수동 연결 누락에 대비한다.
            if (targetView == null)
                targetView = GetComponentInParent<ShopProductCardView>(true);
        }

        /// <summary>포인터 진입 이벤트를 ProductCard View로 전달한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Tooltip 표시는 루트 View가 담당한다.
            if (targetView != null)
                targetView.HandlePointerEntered(eventData);
        }

        /// <summary>포인터 이탈 이벤트를 ProductCard View로 전달한다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // Tooltip 숨김은 루트 View가 담당한다.
            if (targetView != null)
                targetView.HandlePointerExited(eventData);
        }
    }
}
