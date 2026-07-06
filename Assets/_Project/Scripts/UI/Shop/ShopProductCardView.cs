using System;
using Tessera.Data;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Tessera.UI
{
    /// <summary>Shop 상품 카드 UI 하나를 표시하고 카드 클릭 구매와 prefab 내부 툴팁을 처리한다.</summary>
    public class ShopProductCardView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button cardButton;
        [SerializeField] private Image backgroundImage;

        [Header("Content")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text priceText;

        [Header("Tooltip")]
        [SerializeField] private ShopProductTooltipView tooltipView;

        [Header("Hover Feedback")]
        [SerializeField] private RectTransform scaleRoot;
        [SerializeField] private Image highlightImage;
        [SerializeField] private float hoverScaleMultiplier = 1.2f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.86f, 0.18f, 1f);
        [SerializeField] private Vector2 highlightDistance = new Vector2(4f, -4f);

        [Header("Purchase Blocked Feedback")]
        [SerializeField] private GameObject blockedOverlay; // 구매 불가 피드백 오버레이
        [SerializeField] private RectTransform shakeRoot;
        [SerializeField] private float blockedFeedbackSeconds = 0.38f;
        [SerializeField] private float blockedShakeDistance = 10f;
        [SerializeField] private float blockedShakeFrequency = 38f;

        private int boundSlotIndex = -1; // Shop 슬롯 인덱스
        private bool isPurchasable; // 클릭 가능 여부 (구매 가능 여부)
        private string boundDisplayName = string.Empty;
        private string boundDescription = string.Empty;
        private string boundTierLabel = string.Empty;
        private Vector3 normalScale = Vector3.one;
        private Outline hoverOutline;
        private bool isHovering;
        private ShopInventorySlot boundSlot;
        private TesseraRunSession boundRunSession;
        private Vector2 normalAnchoredPosition;
        private bool hasNormalAnchoredPosition;
        private int blockedFeedbackVersion;

        public event Action<int> PurchaseConfirmed;
        public event Action<string> PurchaseBlocked;

        public int BoundSlotIndex => boundSlotIndex;

        /// <summary>컴포넌트 추가 시 기본 참조와 raycast 설정을 보정한다.</summary>
        private void Reset()
        {
            // Prefab 편집 중 필드 연결 누락을 줄이기 위해 자동 참조를 보정한다.
            AssignReferencesIfMissing();
            AssignHoverReferencesIfMissing();
            AssignBlockedFeedbackReferencesIfMissing();
            ConfigureRaycastTargets();
            CacheNormalScale();
            CacheNormalAnchoredPosition();
            SetHoverFeedback(false);
            SetBlockedOverlay(false);
        }

        /// <summary>초기화 시 참조, raycast, tooltip 표시 상태를 정리한다.</summary>
        private void Awake()
        {
            // TooltipRoot가 prefab에서 비활성화되어 있어도 참조 가능하도록 inactive 포함 검색을 사용한다.
            AssignReferencesIfMissing();
            AssignHoverReferencesIfMissing();
            AssignBlockedFeedbackReferencesIfMissing();
            ConfigureRaycastTargets();
            CacheNormalScale();
            CacheNormalAnchoredPosition();
            HideTooltip();
            SetHoverFeedback(false);
            SetBlockedOverlay(false);
        }

        /// <summary>활성화 시 카드 버튼 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            // 재활성화 시 중복 구독을 방지한다.
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(HandleCardClicked);
                cardButton.onClick.AddListener(HandleCardClicked);
            }
        }

        /// <summary>비활성화 시 카드 버튼 이벤트와 Tooltip 상태를 정리한다.</summary>
        private void OnDisable()
        {
            // 비활성 카드가 구매 이벤트를 발생시키지 않도록 구독을 해제한다.
            if (cardButton != null)
                cardButton.onClick.RemoveListener(HandleCardClicked);

            blockedFeedbackVersion++;

            HideTooltip();
            SetHoverFeedback(false);
            RestoreBlockedFeedbackState();
        }

        /// <summary>파괴 시 외부 이벤트 참조를 정리한다.</summary>
        private void OnDestroy()
        {
            // StageShopFlowView 쪽에 파괴된 카드 참조가 남지 않도록 이벤트를 정리한다.
            PurchaseConfirmed = null;
            PurchaseBlocked = null;
        }

        /// <summary>상품 슬롯과 현재 RunSession 기준으로 카드 표시 정보를 갱신한다.</summary>
        public void Bind(ShopInventorySlot slot, TesseraRunSession runSession)
        {
            // 재바인딩 시 이전 tooltip이 남지 않도록 먼저 닫는다.
            HideTooltip();
            SetHoverFeedback(false);

            AssignReferencesIfMissing();
            AssignBlockedFeedbackReferencesIfMissing();
            ConfigureRaycastTargets();
            CacheNormalAnchoredPosition();
            SetBlockedOverlay(false);

            boundSlot = slot;
            boundRunSession = runSession;
            boundSlotIndex = slot != null ? slot.SlotIndex : -1;

            ShopProductDefinitionSO product = slot != null ? slot.ProductDefinition : null;

            boundDisplayName = product != null ? product.DisplayName : string.Empty;
            boundDescription = product != null ? product.Description : string.Empty;
            boundTierLabel = product != null ? $"Tier {product.Tier}" : string.Empty;

            bool visible = slot != null && !slot.IsSoldOut;
            SetVisible(visible);

            if (!visible)
            {
                isPurchasable = false;
                RefreshInteractable(false);
                return;
            }

            SetText(priceText, BuildPriceText(slot));
            RefreshIcon(product);
            RefreshBackground(product);

            isPurchasable = CanClickProduct(slot, runSession);
            RefreshInteractable(isPurchasable);
        }

        /// <summary>카드 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (!visible)
                RestoreBlockedFeedbackState();

            // ProductCard 루트가 연결되어 있으면 루트 전체를 켜고 끈다.
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>가격 표시 문자열을 만든다.</summary>
        private static string BuildPriceText(ShopInventorySlot slot)
        {
            // Money와 Overcharge 복합 가격까지 동일한 위치에 표시한다.
            if (slot == null)
                return string.Empty;

            if (slot.MoneyPrice > 0 && slot.OverchargePrice > 0)
                return $"${slot.MoneyPrice} / OC {slot.OverchargePrice}";

            if (slot.OverchargePrice > 0)
                return $"OC {slot.OverchargePrice}";

            return $"${slot.MoneyPrice}";
        }

        /// <summary>카드 클릭 가능 여부를 검사한다.</summary>
        private static bool CanClickProduct(ShopInventorySlot slot, TesseraRunSession runSession)
        {
            // 세부 구매 실패 사유는 StageShopFlowView.CanConfirmProductPurchase에서 처리한다.
            if (runSession == null)
                return false;

            if (slot == null)
                return false;

            if (slot.IsSoldOut)
                return false;

            if (slot.ProductDefinition == null)
                return false;

            return true;
        }

        /// <summary>상품 아이콘 이미지를 갱신한다.</summary>
        private void RefreshIcon(ShopProductDefinitionSO product)
        {
            // 아이콘이 없는 상품은 아이콘 이미지만 숨긴다.
            if (iconImage == null)
                return;

            Sprite icon = product != null ? product.Icon : null;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.raycastTarget = false;
        }

        /// <summary>상품 배경 이미지를 갱신한다.</summary>
        private void RefreshBackground(ShopProductDefinitionSO product)
        {
            if (backgroundImage == null)
                return;

            Sprite cardBackground = product != null ? product.CardBackgroundSprite : null;
            if (cardBackground != null)
                backgroundImage.sprite = cardBackground;

            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true;
        }

        /// <summary>카드 버튼 상호작용 상태를 갱신한다.</summary>
        private void RefreshInteractable(bool interactable)
        {
            // 구매 불가능한 구현 미완성 상품은 클릭만 막는다.
            if (cardButton != null)
                cardButton.interactable = interactable;
        }

        /// <summary>카드 클릭 시 구매 가능 여부를 검사하고 구매 또는 구매 불가 피드백을 전달한다.</summary>
        private void HandleCardClicked()
        {
            if (boundSlotIndex < 0)
                return;

            if (!isPurchasable)
            {
                string invalidMessage = "Invalid shop product.";
                PlayPurchaseBlockedFeedback(invalidMessage);
                PurchaseBlocked?.Invoke(invalidMessage);
                return;
            }

            if (!CanConfirmPurchase(out string failureMessage))
            {
                PlayPurchaseBlockedFeedback(failureMessage);
                PurchaseBlocked?.Invoke(failureMessage);
                return;
            }

            PurchaseConfirmed?.Invoke(boundSlotIndex);
        }

        /// <summary>포인터 진입 시 Hover 피드백과 prefab 내부 Tooltip을 표시한다.</summary>
        public void HandlePointerEntered(PointerEventData eventData)
        {
            // 구매 가능 여부와 무관하게 상품 정보 확인은 가능하므로 hover 피드백은 슬롯 유효성만 본다.
            if (boundSlotIndex < 0)
                return;

            SetHoverFeedback(true);

            if (tooltipView == null) return;
            if (string.IsNullOrWhiteSpace(boundDescription)) return;

            tooltipView.Show(boundDisplayName, boundDescription, boundTierLabel);
        }

        /// <summary>포인터 이탈 시 Hover 피드백과 prefab 내부 Tooltip을 숨긴다.</summary>
        public void HandlePointerExited(PointerEventData eventData)
        {
            // 카드 영역 밖으로 나가면 hover 상태와 tooltip을 닫는다.
            SetHoverFeedback(false);
            HideTooltip();
        }

        /// <summary>현재 카드 기준 구매 확정 가능 여부와 실패 사유를 반환한다.</summary>
        public bool CanConfirmPurchase(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (boundRunSession == null)
            {
                failureMessage = "RunSession is missing.";
                return false;
            }

            if (boundSlot == null || boundSlot.ProductDefinition == null)
            {
                failureMessage = "Invalid shop product.";
                return false;
            }

            if (boundSlot.IsSoldOut)
            {
                failureMessage = "This product is already sold out.";
                return false;
            }

            ShopProductDefinitionSO product = boundSlot.ProductDefinition;

            if (!product.IsPurchasableInCurrentBuild())
            {
                failureMessage = "This product type is not implemented yet.";
                return false;
            }

            if (product.ProductType == ShopProductType.Device && !boundRunSession.HasEmptyDeviceSlot())
            {
                failureMessage = "Device slots are full. Sell an equipped Device before buying a new one.";
                return false;
            }

            if (boundRunSession.Money < boundSlot.MoneyPrice)
            {
                failureMessage = "Not enough Money.";
                return false;
            }

            if (boundRunSession.Overcharge < boundSlot.OverchargePrice)
            {
                failureMessage = "Not enough Overcharge.";
                return false;
            }

            return true;
        }

        /// <summary>구매 불가 Overlay와 Shake 피드백을 재생한다.</summary>
        public void PlayPurchaseBlockedFeedback(string reason)
        {
            AssignBlockedFeedbackReferencesIfMissing();
            CacheNormalAnchoredPosition();

            PlayPurchaseBlockedFeedbackAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        #region Helper

        /// <summary>구매 불가 Overlay와 Shake 피드백을 비동기로 재생한다.</summary>
        private async UniTaskVoid PlayPurchaseBlockedFeedbackAsync(CancellationToken cancellationToken)
        {
            int version = ++blockedFeedbackVersion;

            SetBlockedOverlay(true);

            RectTransform targetRoot = shakeRoot != null
                ? shakeRoot
                : transform as RectTransform;

            Vector2 basePosition = targetRoot != null
                ? targetRoot.anchoredPosition
                : Vector2.zero;

            float duration = Mathf.Max(0.01f, blockedFeedbackSeconds);
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    elapsed += Time.unscaledDeltaTime;

                    float normalized = Mathf.Clamp01(elapsed / duration);
                    float damping = 1f - normalized;
                    float offsetX = Mathf.Sin(elapsed * blockedShakeFrequency) * blockedShakeDistance * damping;

                    if (targetRoot != null)
                        targetRoot.anchoredPosition = basePosition + new Vector2(offsetX, 0f);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version != blockedFeedbackVersion)
                return;

            if (targetRoot != null)
                targetRoot.anchoredPosition = basePosition;

            SetBlockedOverlay(false);
        }

        /// <summary>구매 불가 피드백 상태를 초기값으로 복구한다.</summary>
        private void RestoreBlockedFeedbackState()
        {
            SetBlockedOverlay(false);

            if (shakeRoot != null && hasNormalAnchoredPosition)
                shakeRoot.anchoredPosition = normalAnchoredPosition;
        }

        /// <summary>구매 불가 Overlay 표시 상태를 변경한다.</summary>
        private void SetBlockedOverlay(bool active)
        {
            if (blockedOverlay != null)
                blockedOverlay.SetActive(active);
        }

        /// <summary>구매 불가 피드백 참조를 자동 보정한다.</summary>
        private void AssignBlockedFeedbackReferencesIfMissing()
        {
            if (shakeRoot == null && scaleRoot != null)
                shakeRoot = scaleRoot;

            if (shakeRoot == null)
                shakeRoot = transform as RectTransform;

            if (blockedOverlay != null)
                return;

            Transform searchRoot = root != null ? root.transform : transform;
            Transform found = FindChildByName(searchRoot, "BlockedOverlay");

            if (found != null)
                blockedOverlay = found.gameObject;
        }

        /// <summary>Shake 기준 AnchoredPosition을 캐싱한다.</summary>
        private void CacheNormalAnchoredPosition()
        {
            if (shakeRoot == null)
                return;

            normalAnchoredPosition = shakeRoot.anchoredPosition;
            hasNormalAnchoredPosition = true;
        }

        /// <summary>지정 이름의 자식 Transform을 비활성 포함 재귀 검색한다.</summary>
        private static Transform FindChildByName(Transform parent, string targetName)
        {
            if (parent == null)
                return null;

            if (parent.name == targetName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildByName(parent.GetChild(i), targetName);

                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Tooltip을 숨긴다.</summary>
        private void HideTooltip()
        {
            // TooltipRoot가 비활성 상태여도 View 참조만 있으면 안전하게 Hide 가능하다.
            if (tooltipView != null)
                tooltipView.Hide();
        }

        /// <summary>Hover 피드백용 참조를 자동 보정한다.</summary>
        private void AssignHoverReferencesIfMissing()
        {
            // TooltipRoot가 scale되지 않도록 ContentRoot/Button 쪽 RectTransform을 우선 사용한다.
            if (scaleRoot == null && cardButton != null)
                scaleRoot = cardButton.transform as RectTransform;

            if (scaleRoot == null)
                scaleRoot = transform as RectTransform;

            // 외곽선은 카드 배경 Image에 적용한다.
            if (highlightImage == null)
                highlightImage = backgroundImage;

            if (highlightImage == null)
                return;

            hoverOutline = highlightImage.GetComponent<Outline>();

            if (hoverOutline == null)
                hoverOutline = highlightImage.gameObject.AddComponent<Outline>();

            hoverOutline.effectColor = highlightColor;
            hoverOutline.effectDistance = highlightDistance;
            hoverOutline.useGraphicAlpha = false;
            hoverOutline.enabled = false;
        }

        /// <summary>기본 Scale 값을 캐싱한다.</summary>
        private void CacheNormalScale()
        {
            if (scaleRoot == null)
                return;

            normalScale = scaleRoot.localScale;
        }

        /// <summary>Hover 피드백 표시 상태를 적용한다.</summary>
        private void SetHoverFeedback(bool active)
        {
            if (isHovering == active)
                return;

            isHovering = active;

            if (scaleRoot != null)
                scaleRoot.localScale = active ? normalScale * hoverScaleMultiplier : normalScale;

            if (hoverOutline != null)
                hoverOutline.enabled = active;
        }

        /// <summary>누락된 UI 참조를 자동 보정한다.</summary>
        private void AssignReferencesIfMissing()
        {
            // A안 기준: ShopProductCardView는 ContentRoot에 붙고, 부모가 ProductCard 루트다.
            if (root == null)
                root = ResolveRootObject();

            if (cardButton == null)
                cardButton = GetComponent<Button>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (iconImage == null)
                iconImage = FindImageByName("IconImage");

            if (priceText == null)
                priceText = FindTextByName("PriceText");

            if (tooltipView == null && root != null)
                tooltipView = root.GetComponentInChildren<ShopProductTooltipView>(true);

            AssignHoverReferencesIfMissing();
        }

        /// <summary>ProductCard 루트 오브젝트를 반환한다.</summary>
        private GameObject ResolveRootObject()
        {
            // ContentRoot의 부모가 ProductCard 루트인 구조를 우선 사용한다.
            if (transform.parent != null)
                return transform.parent.gameObject;

            return gameObject;
        }

        /// <summary>카드 버튼 구조에 맞게 Graphic RaycastTarget을 보정한다.</summary>
        private void ConfigureRaycastTargets()
        {
            // ContentRoot 배경 이미지만 실제 클릭/hover 판정 대상으로 둔다.
            if (backgroundImage != null)
                backgroundImage.raycastTarget = true;

            if (root == null)
                return;

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];

                if (graphic == null)
                    continue;

                if (graphic == backgroundImage)
                    continue;

                graphic.raycastTarget = false;
            }
        }

        /// <summary>지정 이름의 TMP_Text 자식 참조를 찾는다.</summary>
        private TMP_Text FindTextByName(string targetName)
        {
            // 비활성 TooltipRoot까지 포함될 수 있으므로 이름 기반으로 정확히 찾는다.
            Transform searchRoot = root != null ? root.transform : transform;
            TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text == null)
                    continue;

                if (text.name == targetName)
                    return text;
            }

            return null;
        }

        /// <summary>지정 이름의 Image 자식 참조를 찾는다.</summary>
        private Image FindImageByName(string targetName)
        {
            // 비활성 자식까지 포함해 IconImage를 찾는다.
            Transform searchRoot = root != null ? root.transform : transform;
            Image[] images = searchRoot.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null)
                    continue;

                if (image.name == targetName)
                    return image;
            }

            return null;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            // 참조가 없는 텍스트는 조용히 생략한다.
            if (targetText == null)
                return;

            targetText.text = value ?? string.Empty;
        }

        #endregion
    }
}
