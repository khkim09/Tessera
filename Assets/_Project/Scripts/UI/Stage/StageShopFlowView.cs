using System;
using System.Collections.Generic;
using Tessera.Data;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Stage Economy v1 Workshop Shell View다.</summary>
    public class StageShopFlowView : MonoBehaviour
    {
        private const int RepairCostMoney = 8;
        private const int RepairHealAmount = 10;
        private const int UpgradeTierOverchargeCost = 1;

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text resourceText;

        [Header("Buttons")]
        [SerializeField] private Button repairButton;
        [SerializeField] private TMP_Text repairButtonText;
        [SerializeField] private Button upgradeTierButton;
        [SerializeField] private TMP_Text upgradeTierButtonText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;

        [Header("Shop Products")]
        [SerializeField] private Transform productCardRoot;
        [SerializeField] private ShopProductCardView productCardPrefab;
        [SerializeField] private RectTransform[] productCardAnchors;

        [Header("Shop Card Cancel Area")]
        [SerializeField] private Button cardSelectionCancelButton;

        [Header("Shop Tooltip")]
        [SerializeField] private ShopProductTooltipView tooltipViewPrefab;
        [SerializeField] private RectTransform tooltipParent;

        private TesseraRunSession currentRunSession;
        private IReadOnlyList<ShopInventorySlot> currentProductSlots;
        private int flippedProductSlotIndex = -1;
        private readonly List<ShopProductCardView> productCards = new List<ShopProductCardView>();

        private ShopProductTooltipView tooltipViewInstance;

        #region Events

        /// <summary>Continue 버튼 클릭 이벤트.</summary>
        public event Action ContinueRequested;

        /// <summary>Repair 버튼 클릭 이벤트.</summary>
        public event Action RepairRequested;

        /// <summary>Upgrade Tier 버튼 클릭 이벤트.</summary>
        public event Action UpgradeTierRequested;

        /// <summary>Shop 상품 구매 확정 이벤트.</summary>
        public event Action<int> ProductBuyConfirmed;

        #endregion

        /// <summary>버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (repairButton != null)
                repairButton.onClick.AddListener(HandleRepairClicked);

            if (upgradeTierButton != null)
                upgradeTierButton.onClick.AddListener(HandleUpgradeTierClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinueClicked);

            if (cardSelectionCancelButton != null)
                cardSelectionCancelButton.onClick.AddListener(HandleCardSelectionCanceled);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (repairButton != null)
                repairButton.onClick.RemoveListener(HandleRepairClicked);

            if (upgradeTierButton != null)
                upgradeTierButton.onClick.RemoveListener(HandleUpgradeTierClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);

            if (cardSelectionCancelButton != null)
                cardSelectionCancelButton.onClick.RemoveListener(HandleCardSelectionCanceled);

            UnbindProductCards();
            HideTooltip();
        }

        /// <summary>Shop Shell을 표시한다.</summary>
        public void Show(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            StageShopReasonType reasonType,
            string message,
            IReadOnlyList<ShopInventorySlot> productSlots,
            StageWorkshopRulesSO workshopRules)
        {
            SetVisible(true);

            currentRunSession = runSession;
            currentProductSlots = productSlots;
            flippedProductSlotIndex = -1;

            if (titleText != null)
                titleText.text = ResolveTitle(reasonType);

            RefreshMessage(message);
            RefreshResourceText(runSession, boardState);
            RefreshButtons(runSession, reasonType, workshopRules);
            RefreshProductCards(productSlots, runSession);
            HideTooltip();
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>자원 표시 텍스트를 갱신한다.</summary>
        private void RefreshResourceText(TesseraRunSession runSession, StageBountyBoardState boardState)
        {
            if (resourceText == null)
                return;

            int money = runSession != null ? runSession.Money : 0;
            int hp = runSession != null ? runSession.PlayerCurrentHP : 0;
            int maxHP = runSession != null ? runSession.PlayerMaxHP : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int workshopTier = runSession != null ? runSession.CurrentWorkshopTier : 1;
            int runChain = runSession != null ? runSession.RunChainCount : 0;
            int stageChain = boardState != null ? boardState.ChainCount : 0;
            int stageThreat = boardState != null ? boardState.StageThreatLevel : 0;
            int pendingMoney = boardState != null ? boardState.PendingMoneyReward : 0;
            bool retreatRecovery = boardState != null && boardState.IsRetreatRecoveryActive;
            bool enraged = boardState != null && boardState.IsEnraged;

            resourceText.text =
                $"HP {hp}/{maxHP}\n" +
                $"Money {money}\n" +
                $"Overcharge {overcharge}\n" +
                $"Workshop Tier {workshopTier}\n" +
                $"Run Chain {runChain}\n" +
                $"Stage Chain {stageChain}\n" +
                $"StageThreat {stageThreat}" + (enraged ? " / Enraged" : string.Empty) + "\n" +
                $"Pending Money {pendingMoney}\n" +
                $"Retreat Recovery: {retreatRecovery}";
        }

        /// <summary>버튼 텍스트와 상호작용 상태를 갱신한다.</summary>
        private void RefreshButtons(
            TesseraRunSession runSession,
            StageShopReasonType reasonType,
            StageWorkshopRulesSO workshopRules)
        {
            int money = runSession != null ? runSession.Money : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int hp = runSession != null ? runSession.PlayerCurrentHP : 0;
            int maxHP = runSession != null ? runSession.PlayerMaxHP : 0;

            int upgradeCost = workshopRules != null
                ? workshopRules.TierUpgradeOverchargeCost
                : UpgradeTierOverchargeCost;

            if (repairButtonText != null)
                repairButtonText.text = $"Repair +{RepairHealAmount} / Money {RepairCostMoney}";

            if (upgradeTierButtonText != null)
                upgradeTierButtonText.text = $"Upgrade Tier / Overcharge {upgradeCost}";

            if (continueButtonText != null)
                continueButtonText.text = reasonType == StageShopReasonType.StageClear
                    ? "Continue to Next Stage"
                    : "Continue";

            if (repairButton != null)
                repairButton.interactable = runSession != null && money >= RepairCostMoney && hp < maxHP;

            if (upgradeTierButton != null)
                upgradeTierButton.interactable = runSession != null && overcharge >= upgradeCost;

            if (continueButton != null)
                continueButton.interactable = true;
        }

        /// <summary>Shop 상품 카드 목록을 갱신한다.</summary>
        private void RefreshProductCards(
            IReadOnlyList<ShopInventorySlot> productSlots,
            TesseraRunSession runSession)
        {
            int slotCount = productSlots != null ? productSlots.Count : 0;

            for (int i = 0; i < slotCount; i++)
            {
                ShopProductCardView card = GetOrCreateProductCard(i);
                ShopInventorySlot slot = productSlots[i];

                if (card == null)
                    continue;

                card.Bind(slot, runSession);

                if (slot == null || slot.IsSoldOut)
                {
                    card.SetFlipped(false);
                    card.SetVisible(false);
                    continue;
                }

                card.SetFlipped(slot.SlotIndex == flippedProductSlotIndex);
            }

            for (int i = slotCount; i < productCards.Count; i++)
            {
                if (productCards[i] == null)
                    continue;

                productCards[i].SetVisible(false);
                productCards[i].SetFlipped(false);
            }
        }

        /// <summary>지정 인덱스의 상품 카드 View를 반환하거나 생성한다.</summary>
        private ShopProductCardView GetOrCreateProductCard(int index)
        {
            while (productCards.Count <= index)
            {
                ShopProductCardView card = null;

                if (productCardPrefab != null && productCardRoot != null)
                {
                    Transform parent = ResolveProductCardParent(productCards.Count);
                    card = Instantiate(productCardPrefab, parent);
                    ApplyProductCardAnchor(card, productCards.Count);
                }

                if (card != null)
                {
                    card.BuyRequested += HandleProductCardSelected;
                    card.PurchaseConfirmed += HandleProductCardPurchaseConfirmed;
                    card.TooltipRequested += HandleProductTooltipRequested;
                    card.TooltipHidden += HandleProductTooltipHidden;
                }

                productCards.Add(card);
            }

            return productCards[index];
        }

        /// <summary>상품 카드 인덱스에 맞는 부모 Transform을 반환한다.</summary>
        private Transform ResolveProductCardParent(int cardIndex)
        {
            if (productCardAnchors != null
                && cardIndex >= 0
                && cardIndex < productCardAnchors.Length
                && productCardAnchors[cardIndex] != null)
                return productCardAnchors[cardIndex];

            return productCardRoot;
        }

        /// <summary>상품 카드 RectTransform을 수동 배치 Anchor에 맞춘다.</summary>
        private void ApplyProductCardAnchor(ShopProductCardView card, int cardIndex)
        {
            if (card == null)
                return;

            RectTransform cardRect = card.transform as RectTransform;

            if (cardRect == null)
                return;

            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;

            if (productCardAnchors != null
                && cardIndex >= 0
                && cardIndex < productCardAnchors.Length
                && productCardAnchors[cardIndex] != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>상품 카드 이벤트 연결을 해제한다.</summary>
        private void UnbindProductCards()
        {
            for (int i = 0; i < productCards.Count; i++)
            {
                if (productCards[i] == null)
                    continue;

                productCards[i].BuyRequested -= HandleProductCardSelected;
                productCards[i].PurchaseConfirmed -= HandleProductCardPurchaseConfirmed;
                productCards[i].TooltipRequested -= HandleProductTooltipRequested;
                productCards[i].TooltipHidden -= HandleProductTooltipHidden;
            }
        }

        /// <summary>상품 카드 앞면 클릭 시 해당 카드만 뒤집는다.</summary>
        private void HandleProductCardSelected(int productSlotIndex)
        {
            SetFlippedProductCard(productSlotIndex);
        }

        /// <summary>상품 카드 뒷면 가격 버튼 클릭 시 구매 확정을 외부로 전달한다.</summary>
        private void HandleProductCardPurchaseConfirmed(int productSlotIndex)
        {
            if (!CanConfirmProductPurchase(productSlotIndex))
                return;

            ProductBuyConfirmed?.Invoke(productSlotIndex);
            SetFlippedProductCard(-1);
        }

        /// <summary>카드 외부 클릭 시 현재 뒤집힌 카드를 원복한다.</summary>
        private void HandleCardSelectionCanceled()
        {
            SetFlippedProductCard(-1);
        }

        /// <summary>현재 뒤집힌 상품 카드를 설정한다.</summary>
        private void SetFlippedProductCard(int productSlotIndex)
        {
            flippedProductSlotIndex = productSlotIndex;

            for (int i = 0; i < productCards.Count; i++)
            {
                ShopProductCardView card = productCards[i];

                if (card == null)
                    continue;

                ShopInventorySlot slot = GetProductSlotByCardIndex(i);
                bool flipped = slot != null && slot.SlotIndex == flippedProductSlotIndex;
                card.SetFlipped(flipped);
            }
        }

        /// <summary>Tooltip View 인스턴스를 반환한다.</summary>
        private ShopProductTooltipView GetOrCreateTooltipView()
        {
            if (tooltipViewInstance != null)
                return tooltipViewInstance;

            if (tooltipViewPrefab == null)
                return null;

            Transform parent = tooltipParent != null ? tooltipParent : root != null ? root.transform : transform;
            tooltipViewInstance = Instantiate(tooltipViewPrefab, parent);
            tooltipViewInstance.Hide();

            return tooltipViewInstance;
        }

        /// <summary>상품 구매 확정 가능 여부를 검사하고 실패 메시지를 표시한다.</summary>
        private bool CanConfirmProductPurchase(int productSlotIndex)
        {
            if (currentRunSession == null)
            {
                SetShopMessage("RunSession is missing.");
                return false;
            }

            ShopInventorySlot slot = FindCurrentProductSlot(productSlotIndex);

            if (slot == null || slot.ProductDefinition == null)
            {
                SetShopMessage("Invalid shop product.");
                return false;
            }

            if (slot.IsSoldOut)
            {
                SetShopMessage("This product is already sold out.");
                return false;
            }

            if (!slot.ProductDefinition.IsPurchasableInCurrentBuild())
            {
                SetShopMessage("This product type is not implemented yet.");
                return false;
            }

            if (slot.ProductDefinition.ProductType == ShopProductType.Device && !currentRunSession.HasEmptyDeviceSlot())
            {
                SetShopMessage("Device slots are full. Sell an equipped Device before buying a new one.");
                return false;
            }

            if (currentRunSession.Money < slot.MoneyPrice)
            {
                SetShopMessage("Not enough Money.");
                return false;
            }

            if (currentRunSession.Overcharge < slot.OverchargePrice)
            {
                SetShopMessage("Not enough Overcharge.");
                return false;
            }

            return true;
        }

        /// <summary>현재 표시 중인 상품 슬롯을 찾는다.</summary>
        private ShopInventorySlot FindCurrentProductSlot(int productSlotIndex)
        {
            if (currentProductSlots == null)
                return null;

            for (int i = 0; i < currentProductSlots.Count; i++)
            {
                ShopInventorySlot slot = currentProductSlots[i];

                if (slot != null && slot.SlotIndex == productSlotIndex)
                    return slot;
            }

            return null;
        }

        /// <summary>카드 인덱스에 해당하는 상품 슬롯을 반환한다.</summary>
        private ShopInventorySlot GetProductSlotByCardIndex(int cardIndex)
        {
            if (currentProductSlots == null)
                return null;

            if (cardIndex < 0 || cardIndex >= currentProductSlots.Count)
                return null;

            return currentProductSlots[cardIndex];
        }

        /// <summary>상품 설명 툴팁을 표시한다.</summary>
        private void HandleProductTooltipRequested(
            string displayName,
            string description,
            string tierLabel,
            Vector2 screenPosition)
        {
            ShopProductTooltipView tooltipView = GetOrCreateTooltipView();

            if (tooltipView == null)
                return;

            tooltipView.Show(displayName, description, tierLabel);
        }

        /// <summary>상품 설명 툴팁을 숨긴다.</summary>
        private void HideTooltip()
        {
            if (tooltipViewInstance != null)
                tooltipViewInstance.Hide();
        }

        /// <summary>상품 설명 툴팁을 숨긴다.</summary>
        private void HandleProductTooltipHidden()
        {
            HideTooltip();
        }

        /// <summary>Shop 메시지 텍스트를 갱신한다.</summary>
        private void SetShopMessage(string message)
        {
            if (messageText == null)
                return;

            messageText.text = message ?? string.Empty;
        }

        /// <summary>Shop 메시지를 갱신한다.</summary>
        private void RefreshMessage(string message)
        {
            if (messageText == null)
                return;

            messageText.text = message ?? string.Empty;
        }

        /// <summary>Workshop 진입 사유에 맞는 타이틀을 반환한다.</summary>
        private static string ResolveTitle(StageShopReasonType reasonType)
        {
            if (reasonType == StageShopReasonType.StageClear)
                return "Workshop - Stage Clear";

            if (reasonType == StageShopReasonType.CashOut)
                return "Workshop - Cash Out";

            if (reasonType == StageShopReasonType.Retreat)
                return "Workshop - Emergency Retreat";

            if (reasonType == StageShopReasonType.Tutorial)
                return "Workshop - Tutorial";

            return "Workshop";
        }

        /// <summary>Repair 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleRepairClicked()
        {
            SetFlippedProductCard(-1);
            RepairRequested?.Invoke();
        }

        /// <summary>Upgrade Tier 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleUpgradeTierClicked()
        {
            SetFlippedProductCard(-1);
            UpgradeTierRequested?.Invoke();
        }

        /// <summary>Continue 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleContinueClicked()
        {
            SetFlippedProductCard(-1);
            ContinueRequested?.Invoke();
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
