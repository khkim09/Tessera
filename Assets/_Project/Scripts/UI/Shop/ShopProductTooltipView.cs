using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Shop 상품 설명 툴팁 View다.</summary>
    public class ShopProductTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text tierText;

        /// <summary>툴팁 내용을 표시한다.</summary>
        public void Show(string displayName, string description, string tierLabel)
        {
            SetVisible(true);
            SetText(nameText, displayName);
            SetText(descriptionText, description);
            SetText(tierText, tierLabel);
        }

        /// <summary>툴팁을 숨긴다.</summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value ?? string.Empty;
        }
    }
}
