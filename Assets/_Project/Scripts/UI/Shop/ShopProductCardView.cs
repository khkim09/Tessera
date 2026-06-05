using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tessera.Data;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Shop 상품 카드 UI 하나를 표시하고 선택/구매/툴팁 이벤트를 전달한다.</summary>
    public class ShopProductCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button cardButton;
        [SerializeField] private RectTransform flipRoot;

        [Header("Front")]
        [SerializeField] private GameObject frontRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private TMP_Text frontPriceText;
        [SerializeField] private Image iconImage;

        [Header("Back")]
        [SerializeField] private GameObject backRoot;
        [SerializeField] private Button priceButton;
        [SerializeField] private TMP_Text backPriceText;

        [Header("Flip")]
        [SerializeField] private float flipDurationSeconds = 0.18f;

        private int boundSlotIndex = -1;
        private string boundDisplayName = string.Empty;
        private string boundDescription = string.Empty;
        private string boundTierLabel = string.Empty;
        private bool isFlipped;
        private bool isAnimating;
        private CancellationTokenSource flipCancellation;

        /// <summary>카드 앞면 선택 요청 이벤트.</summary>
        public event Action<int> BuyRequested;

        /// <summary>카드 뒷면 가격 버튼 클릭 이벤트.</summary>
        public event Action<int> PurchaseConfirmed;

        /// <summary>툴팁 표시 요청 이벤트.</summary>
        public event Action<string, string, string, Vector2> TooltipRequested;

        /// <summary>툴팁 숨김 요청 이벤트.</summary>
        public event Action TooltipHidden;

        /// <summary>버튼 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (cardButton != null)
                cardButton.onClick.AddListener(HandleCardClicked);

            if (priceButton != null)
                priceButton.onClick.AddListener(HandlePriceClicked);
        }

        /// <summary>버튼 이벤트와 비동기 연출을 정리한다.</summary>
        private void OnDisable()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveListener(HandleCardClicked);

            if (priceButton != null)
                priceButton.onClick.RemoveListener(HandlePriceClicked);

            CancelFlipAnimation();
        }

        /// <summary>파괴 시 비동기 연출을 정리한다.</summary>
        private void OnDestroy()
        {
            CancelFlipAnimation();
        }

        /// <summary>상품 슬롯 상태를 카드 UI에 반영한다.</summary>
        public void Bind(ShopInventorySlot slot, TesseraRunSession runSession)
        {
            boundSlotIndex = slot != null ? slot.SlotIndex : -1;

            ShopProductDefinitionSO product = slot != null ? slot.ProductDefinition : null;

            boundDisplayName = product != null ? product.DisplayName : string.Empty;
            boundDescription = product != null ? product.Description : string.Empty;
            boundTierLabel = product != null ? $"Tier {product.Tier}" : string.Empty;

            SetVisible(slot != null && !slot.IsSoldOut);

            if (slot == null || slot.IsSoldOut)
                return;

            SetText(nameText, !string.IsNullOrWhiteSpace(boundDisplayName) ? boundDisplayName : "Empty");
            SetText(tierText, boundTierLabel);

            string priceText = BuildPriceText(slot);
            SetText(frontPriceText, priceText);
            SetText(backPriceText, priceText);

            RefreshIcon(product);
            RefreshInteractable(slot, runSession);
        }

        /// <summary>카드 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>카드 앞면/뒷면 표시 상태를 변경한다.</summary>
        public void SetFlipped(bool flipped)
        {
            if (isFlipped == flipped && !isAnimating)
                return;

            PlayFlipAsync(flipped).Forget();
        }

        /// <summary>가격 표시 문자열을 만든다.</summary>
        private static string BuildPriceText(ShopInventorySlot slot)
        {
            if (slot == null)
                return string.Empty;

            if (slot.MoneyPrice > 0 && slot.OverchargePrice > 0)
                return $"${slot.MoneyPrice} / OC {slot.OverchargePrice}";

            if (slot.OverchargePrice > 0)
                return $"OC {slot.OverchargePrice}";

            return $"${slot.MoneyPrice}";
        }

        /// <summary>상품 아이콘을 갱신한다.</summary>
        private void RefreshIcon(ShopProductDefinitionSO product)
        {
            if (iconImage == null)
                return;

            Sprite icon = product != null ? product.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        /// <summary>카드/가격 버튼 상호작용 상태를 갱신한다.</summary>
        private void RefreshInteractable(ShopInventorySlot slot, TesseraRunSession runSession)
        {
            bool canSelect =
                slot != null
                && !slot.IsSoldOut
                && slot.ProductDefinition != null
                && slot.ProductDefinition.IsPurchasableInCurrentBuild();

            if (cardButton != null)
                cardButton.interactable = canSelect && !isFlipped && !isAnimating;

            if (priceButton != null)
                priceButton.interactable = canSelect && isFlipped && !isAnimating;
        }

        /// <summary>카드 뒤집기 연출을 재생한다.</summary>
        private async UniTaskVoid PlayFlipAsync(bool targetFlipped)
        {
            CancelFlipAnimation();

            flipCancellation = new CancellationTokenSource();
            CancellationToken token = flipCancellation.Token;

            isAnimating = true;

            if (cardButton != null)
                cardButton.interactable = false;

            if (priceButton != null)
                priceButton.interactable = false;

            RectTransform targetRoot = flipRoot != null ? flipRoot : transform as RectTransform;

            if (targetRoot == null)
            {
                ApplyFlipState(targetFlipped);
                isAnimating = false;
                return;
            }

            float halfDuration = Mathf.Max(0.01f, flipDurationSeconds * 0.5f);

            try
            {
                await ScaleXAsync(targetRoot, 1f, 0f, halfDuration, token);
                ApplyFlipState(targetFlipped);
                await ScaleXAsync(targetRoot, 0f, 1f, halfDuration, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (targetRoot != null)
                    targetRoot.localScale = Vector3.one;

                isAnimating = false;
                RefreshButtonStateAfterFlip();
            }
        }

        /// <summary>카드 X 스케일을 보간한다.</summary>
        private static async UniTask ScaleXAsync(
            RectTransform targetRoot,
            float from,
            float to,
            float duration,
            CancellationToken token)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(from, to, t);

                Vector3 scale = targetRoot.localScale;
                scale.x = value;
                targetRoot.localScale = scale;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            Vector3 finalScale = targetRoot.localScale;
            finalScale.x = to;
            targetRoot.localScale = finalScale;
        }

        /// <summary>실제 앞면/뒷면 활성 상태를 적용한다.</summary>
        private void ApplyFlipState(bool flipped)
        {
            isFlipped = flipped;

            if (frontRoot != null)
                frontRoot.SetActive(!isFlipped);

            if (backRoot != null)
                backRoot.SetActive(isFlipped);
        }

        /// <summary>뒤집기 연출 후 버튼 상태를 갱신한다.</summary>
        private void RefreshButtonStateAfterFlip()
        {
            if (cardButton != null)
                cardButton.interactable = !isFlipped;

            if (priceButton != null)
                priceButton.interactable = isFlipped;
        }

        /// <summary>진행 중인 뒤집기 연출을 취소한다.</summary>
        private void CancelFlipAnimation()
        {
            if (flipCancellation == null)
                return;

            flipCancellation.Cancel();
            flipCancellation.Dispose();
            flipCancellation = null;
        }

        /// <summary>카드 앞면 클릭을 외부로 전달한다.</summary>
        private void HandleCardClicked()
        {
            if (boundSlotIndex < 0 || isFlipped || isAnimating)
                return;

            BuyRequested?.Invoke(boundSlotIndex);
        }

        /// <summary>카드 뒷면 가격 버튼 클릭을 외부로 전달한다.</summary>
        private void HandlePriceClicked()
        {
            if (boundSlotIndex < 0 || !isFlipped || isAnimating)
                return;

            PurchaseConfirmed?.Invoke(boundSlotIndex);
        }

        /// <summary>마우스 진입 시 설명 툴팁 표시를 요청한다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(boundDescription))
                return;

            TooltipRequested?.Invoke(boundDisplayName, boundDescription, boundTierLabel, eventData.position);
        }

        /// <summary>마우스 이탈 시 설명 툴팁 숨김을 요청한다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipHidden?.Invoke();
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
