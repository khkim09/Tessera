using System;
using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Shop 상품 하나의 표시와 구매 버튼 입력을 관리한다.</summary>
    public class ShopProductEntryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text stateText;

        [Header("Colors")]
        [SerializeField] private Color availableColor = new Color(0.18f, 0.25f, 0.35f, 0.95f);
        [SerializeField] private Color unavailableColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);

        private ShopProductDefinitionSO currentProduct;
        private Action<ShopProductDefinitionSO> onClick;

        /// <summary>상품 정보를 표시하고 클릭 콜백을 연결한다.</summary>
        public void Bind(
            ShopProductDefinitionSO product,
            bool canBuy,
            string unavailableReason,
            Action<ShopProductDefinitionSO> onClick)
        {
            ClearButtonListener();

            currentProduct = product;
            this.onClick = onClick;

            if (product == null)
            {
                BindEmpty();
                return;
            }

            // 상품 기본 정보를 표시한다.
            SetText(nameText, product.DisplayName);
            SetText(priceText, $"{product.Price} Parts");
            SetText(descriptionText, product.Description);
            SetText(stateText, canBuy ? "Buy" : unavailableReason);

            // 상품 아이콘을 표시한다.
            if (iconImage != null)
            {
                iconImage.sprite = product.Icon;
                iconImage.enabled = product.Icon != null;
            }

            // 구매 가능 여부에 따라 배경 색을 바꾼다.
            if (backgroundImage != null)
                backgroundImage.color = canBuy ? availableColor : unavailableColor;

            // 구매 가능할 때만 버튼을 활성화한다.
            if (button != null)
            {
                button.interactable = canBuy;
                button.onClick.AddListener(HandleClick);
            }

            gameObject.SetActive(true);
        }

        /// <summary>빈 상품 슬롯으로 표시한다.</summary>
        public void BindEmpty()
        {
            ClearButtonListener();

            currentProduct = null;
            onClick = null;

            SetText(nameText, "-");
            SetText(priceText, "-");
            SetText(descriptionText, string.Empty);
            SetText(stateText, "Empty");

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (backgroundImage != null)
                backgroundImage.color = unavailableColor;

            if (button != null)
                button.interactable = false;

            gameObject.SetActive(true);
        }

        /// <summary>버튼 클릭 시 현재 상품 구매 요청을 전달한다.</summary>
        private void HandleClick()
        {
            if (currentProduct == null || onClick == null)
                return;

            onClick.Invoke(currentProduct);
        }

        /// <summary>버튼 리스너를 제거한다.</summary>
        private void ClearButtonListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(HandleClick);
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
