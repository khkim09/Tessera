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
        [SerializeField] private Transform productCardAnchorRoot;
        [SerializeField] private ShopProductCardView productCardPrefab;
        [SerializeField] private RectTransform[] productCardAnchors;

        private TesseraRunSession currentRunSession;
        private IReadOnlyList<ShopInventorySlot> currentProductSlots;
        private readonly List<ShopProductCardView> productCards = new List<ShopProductCardView>();

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
            // 중복 구독 방지를 위해 제거 후 다시 연결한다.
            if (repairButton != null)
            {
                repairButton.onClick.RemoveListener(HandleRepairClicked);
                repairButton.onClick.AddListener(HandleRepairClicked);
            }

            if (upgradeTierButton != null)
            {
                upgradeTierButton.onClick.RemoveListener(HandleUpgradeTierClicked);
                upgradeTierButton.onClick.AddListener(HandleUpgradeTierClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
                continueButton.onClick.AddListener(HandleContinueClicked);
            }
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            // View 비활성화 시 Shell 버튼 이벤트만 정리한다.
            if (repairButton != null)
                repairButton.onClick.RemoveListener(HandleRepairClicked);

            if (upgradeTierButton != null)
                upgradeTierButton.onClick.RemoveListener(HandleUpgradeTierClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        /// <summary>View 파괴 시 생성된 상품 카드 이벤트 연결을 정리한다.</summary>
        private void OnDestroy()
        {
            // 실제 파괴 시점에만 ProductCard 이벤트 구독을 해제한다.
            UnbindProductCards();
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
            // 현재 Shop 상태를 저장한다.
            SetVisible(true);

            currentRunSession = runSession;
            currentProductSlots = productSlots;

            if (titleText != null)
                titleText.text = ResolveTitle(reasonType);

            // Shell 표시 정보를 최신 상태로 갱신한다.
            RefreshMessage(message);
            RefreshResourceText(runSession, boardState);
            RefreshButtons(runSession, reasonType, workshopRules);
            RefreshProductCards(productSlots, runSession);
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
            // 현재 상품 슬롯 수만큼 카드 생성 또는 재사용한다.
            int slotCount = productSlots != null ? productSlots.Count : 0;

            for (int i = 0; i < slotCount; i++)
            {
                ShopProductCardView card = GetOrCreateProductCard(i);
                ShopInventorySlot slot = productSlots[i];

                if (card == null)
                    continue;

                // 재사용 카드도 이벤트 구독 상태를 매번 보정한다.
                BindProductCardEvents(card);
                card.Bind(slot, runSession);

                if (slot == null || slot.IsSoldOut)
                    card.SetVisible(false);
            }

            // 남는 카드 인스턴스는 숨긴다.
            for (int i = slotCount; i < productCards.Count; i++)
            {
                if (productCards[i] == null)
                    continue;

                productCards[i].SetVisible(false);
            }
        }

        /// <summary>지정 인덱스의 상품 카드 View를 반환하거나 생성한다.</summary>
        private ShopProductCardView GetOrCreateProductCard(int index)
        {
            // 요청 인덱스까지 카드 인스턴스를 생성한다.
            while (productCards.Count <= index)
            {
                ShopProductCardView card = null;

                if (productCardPrefab != null && productCardAnchorRoot != null)
                {
                    Transform parent = ResolveProductCardParent(productCards.Count);
                    card = Instantiate(productCardPrefab, parent);
                    ApplyProductCardAnchor(card, productCards.Count);
                }

                if (card != null)
                    BindProductCardEvents(card);

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

            return productCardAnchorRoot;
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

        /// <summary>상품 카드 이벤트를 중복 없이 연결한다.</summary>
        private void BindProductCardEvents(ShopProductCardView card)
        {
            if (card == null)
                return;

            // ProductCard는 구매 가능 여부를 자체 판단한 뒤 성공/실패 이벤트를 전달한다.
            card.PurchaseConfirmed -= HandleProductCardPurchaseConfirmed;
            card.PurchaseConfirmed += HandleProductCardPurchaseConfirmed;

            card.PurchaseBlocked -= HandleProductCardPurchaseBlocked;
            card.PurchaseBlocked += HandleProductCardPurchaseBlocked;
        }

        /// <summary>상품 카드 이벤트 연결을 해제한다.</summary>
        private void UnbindProductCards()
        {
            // View 파괴 시 카드 구매 이벤트만 해제한다.
            for (int i = 0; i < productCards.Count; i++)
            {
                ShopProductCardView card = productCards[i];

                if (card == null) continue;

                card.PurchaseConfirmed -= HandleProductCardPurchaseConfirmed;
                card.PurchaseBlocked -= HandleProductCardPurchaseBlocked;
            }
        }

        /// <summary>상품 카드 클릭 시 구매 확정을 외부로 전달한다.</summary>
        private void HandleProductCardPurchaseConfirmed(int productSlotIndex)
        {
            if (!TryConfirmProductPurchase(productSlotIndex, out string failureMessage))
            {
                SetShopMessage(failureMessage);

                ShopProductCardView card = FindProductCardBySlotIndex(productSlotIndex);

                if (card != null)
                    card.PlayPurchaseBlockedFeedback(failureMessage);

                return;
            }

            ProductBuyConfirmed?.Invoke(productSlotIndex);
        }

        /// <summary>상품 구매 확정 가능 여부를 검사하고 실패 메시지를 반환한다.</summary>
        private bool TryConfirmProductPurchase(int productSlotIndex, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (currentRunSession == null)
            {
                failureMessage = "RunSession is missing.";
                return false;
            }

            ShopInventorySlot slot = FindCurrentProductSlot(productSlotIndex);

            if (slot == null || slot.ProductDefinition == null)
            {
                failureMessage = "Invalid shop product.";
                return false;
            }

            if (slot.IsSoldOut)
            {
                failureMessage = "This product is already sold out.";
                return false;
            }

            if (!slot.ProductDefinition.IsPurchasableInCurrentBuild())
            {
                failureMessage = "This product type is not implemented yet.";
                return false;
            }

            if (slot.ProductDefinition.ProductType == ShopProductType.Device && !currentRunSession.HasEmptyDeviceSlot())
            {
                failureMessage = "Device slots are full. Sell an equipped Device before buying a new one.";
                return false;
            }

            if (currentRunSession.Money < slot.MoneyPrice)
            {
                failureMessage = "Not enough Money.";
                return false;
            }

            if (currentRunSession.Overcharge < slot.OverchargePrice)
            {
                failureMessage = "Not enough Overcharge.";
                return false;
            }

            return true;
        }

        /// <summary>상품 카드 구매 불가 메시지를 Shop 메시지로 표시한다.</summary>
        private void HandleProductCardPurchaseBlocked(string message)
        {
            SetShopMessage(message);
        }

        /// <summary>Shop 슬롯 인덱스에 해당하는 ProductCard View를 찾는다.</summary>
        private ShopProductCardView FindProductCardBySlotIndex(int productSlotIndex)
        {
            for (int i = 0; i < productCards.Count; i++)
            {
                ShopProductCardView card = productCards[i];

                if (card == null)
                    continue;

                if (card.BoundSlotIndex == productSlotIndex)
                    return card;
            }

            return null;
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
            // StageFlow 쪽에서 실제 Repair 처리를 수행한다.
            RepairRequested?.Invoke();
        }

        /// <summary>Upgrade Tier 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleUpgradeTierClicked()
        {
            // StageFlow 쪽에서 Tier 상승과 상품 Refresh를 수행한다.
            UpgradeTierRequested?.Invoke();
        }

        /// <summary>Continue 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleContinueClicked()
        {
            // Workshop 종료 요청을 외부로 전달한다.
            ContinueRequested?.Invoke();
        }
    }
}
