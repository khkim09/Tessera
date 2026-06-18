using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Shop 상품과 장착 Device 설명을 표시하는 툴팁 View다.</summary>
    public class ShopProductTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text tierText;

        /// <summary>툴팁 RectTransform을 반환한다.</summary>
        public RectTransform RectTransform => transform as RectTransform;

        /// <summary>컴포넌트 추가 시 기본 Canvas 참조를 자동 연결한다.</summary>
        private void Reset()
        {
            // 비활성 자식까지 포함해 TMP 참조를 자동 수집한다.
            AssignReferencesIfMissing();
        }

        /// <summary>초기화 시 Canvas Camera를 보정하고 툴팁을 숨긴다.</summary>
        private void Awake()
        {
            // 비활성 TooltipRoot가 최초 Show 중 Awake될 수 있으므로 여기서 Hide하지 않는다.
            AssignReferencesIfMissing();
            DisableRaycastTargets();
        }

        /// <summary>텍스트 값으로 툴팁을 표시한다.</summary>
        public void Show(string displayName, string description, string tierLabel)
        {
            // 비활성 자식까지 포함해 텍스트 참조를 먼저 보정한다.
            AssignReferencesIfMissing();
            DisableRaycastTargets();

            // 활성화 전에 표시 데이터를 먼저 채워 첫 표시 프레임의 빈 텍스트를 방지한다.
            SetText(nameText, displayName);
            SetText(descriptionText, description);
            SetText(tierText, tierLabel);

            // 모든 데이터 세팅 후 TooltipRoot를 켠다.
            SetVisible(true);

            // LayoutGroup / ContentSizeFitter 사용 시 첫 프레임 레이아웃 누락을 방지한다.
            ForceRebuildLayout();
        }

        /// <summary>Tooltip 레이아웃을 즉시 갱신한다.</summary>
        private void ForceRebuildLayout()
        {
            // 비활성 상태이거나 RectTransform이 없으면 생략한다.
            RectTransform rectTransform = RectTransform;

            if (rectTransform == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        /// <summary>툴팁을 숨긴다.</summary>
        public void Hide()
        {
            // Root가 있으면 Root만 끄고, 없으면 전체 오브젝트를 끈다.
            SetVisible(false);
        }

        /// <summary>툴팁 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            // Prefab 구조 차이를 허용하기 위해 root 유무에 따라 분기한다.
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>누락된 UI 참조를 런타임에서 보정한다.</summary>
        private void AssignReferencesIfMissing()
        {
            // Prefab unpack 또는 씬 수동 배치 중 빠진 참조를 보정한다.
            if (root == null)
                root = gameObject;

            if (nameText == null)
                nameText = FindTextByName("NameText");

            if (descriptionText == null)
                descriptionText = FindTextByName("DescriptionText");

            if (tierText == null)
                tierText = FindTextByName("TierText");
        }

        /// <summary>지정 이름의 TMP_Text 자식 참조를 찾는다.</summary>
        private TMP_Text FindTextByName(string targetName)
        {
            // 비활성 자식까지 포함해 이름 기반 텍스트 참조를 찾는다.
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                    continue;

                if (texts[i].name == targetName)
                    return texts[i];
            }

            return null;
        }

        /// <summary>툴팁 내부 Graphic이 Pointer 이벤트를 막지 않도록 RaycastTarget을 끈다.</summary>
        private void DisableRaycastTargets()
        {
            // Tooltip은 hover/click 대상이 아니므로 모든 Graphic Raycast를 비활성화한다.
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;

                graphics[i].raycastTarget = false;
            }
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            // TMP 참조가 비어 있으면 해당 줄만 표시하지 않는다.
            if (targetText == null)
                return;

            targetText.text = value ?? string.Empty;
        }
    }
}
