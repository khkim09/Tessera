using System;
using Tessera.Data;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Shop 화면에서 상품 표시, 구매, 장착 Device 표시, 다음 진행 요청을 관리한다.</summary>
    public class TesseraShopPresenter : MonoBehaviour, IDeviceSlotReorderHandler
    {
        [Header("Products")]
        [SerializeField] private ShopProductDefinitionSO[] shopProducts = Array.Empty<ShopProductDefinitionSO>();

        [Header("Entry Views")]
        [SerializeField] private ShopProductEntryView[] productEntryViews = Array.Empty<ShopProductEntryView>();

        [Header("Equipped Device Views")]
        [SerializeField] private DeviceSlotView[] equippedDeviceSlotViews = new DeviceSlotView[5];

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text partsText;
        [SerializeField] private TMP_Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;

        private TesseraRunSession runSession;

        /// <summary>Shop에서 다음 진행을 요청할 때 발생한다.</summary>
        public event Action NextRequested;

        /// <summary>RunSession을 연결하고 Shop 화면을 갱신한다.</summary>
        public void Initialize(TesseraRunSession runSession)
        {
            this.runSession = runSession;

            InitializeDeviceSlotViews();
            RefreshAll("Shop opened.");
        }

        /// <summary>현재 Shop 표시를 전체 갱신한다.</summary>
        public void RefreshAll(string message)
        {
            RefreshTexts(message);
            RefreshProductEntries();
            RefreshEquippedDeviceSlots();
            RefreshButtonStates();
        }

        /// <summary>Shop Device 슬롯 드래그 재정렬 요청을 처리한다.</summary>
        public void RequestDeviceSlotSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!TrySwapEquippedSlotPairDevices(sourceSlotIndex, targetSlotIndex))
                return;

            // 장착 순서가 바뀌었으므로 Shop 장착 슬롯 UI를 즉시 다시 그린다.
            RefreshEquippedDeviceSlots();

            // 상품 구매 가능 여부도 장착 슬롯 상태에 의존하므로 같이 갱신한다.
            RefreshProductEntries();

            RefreshTexts("Device order changed.");
        }

        /// <summary>오브젝트 활성화 시 버튼 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNextClicked);
        }

        /// <summary>오브젝트 비활성화 시 버튼 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        /// <summary>Shop 상단 텍스트를 갱신한다.</summary>
        private void RefreshTexts(string message)
        {
            SetText(titleText, "Shop");
            SetText(partsText, runSession != null ? $"Parts {runSession.Parts}" : "Parts -");

            if (!string.IsNullOrWhiteSpace(message))
                SetText(messageText, message);
        }

        /// <summary>상품 Entry들을 현재 상품 배열 기준으로 갱신한다.</summary>
        private void RefreshProductEntries()
        {
            if (productEntryViews == null)
                return;

            for (int i = 0; i < productEntryViews.Length; i++)
            {
                if (productEntryViews[i] == null)
                    continue;

                // 상품 배열보다 Entry가 많으면 빈 슬롯으로 표시한다.
                ShopProductDefinitionSO product = i < shopProducts.Length ? shopProducts[i] : null;
                bool canBuy = CanBuyProduct(product, out string unavailableReason);

                productEntryViews[i].Bind(product, canBuy, unavailableReason, HandleProductClicked);
            }
        }

        /// <summary>장착 Device 슬롯 표시를 RunSession 기준으로 갱신한다.</summary>
        private void RefreshEquippedDeviceSlots()
        {
            if (equippedDeviceSlotViews == null)
                return;

            for (int i = 0; i < equippedDeviceSlotViews.Length; i++)
            {
                if (equippedDeviceSlotViews[i] == null)
                    continue;

                SlotPairDeviceDefinitionSO device = GetEquippedDeviceOrNull(i);
                equippedDeviceSlotViews[i].SetDevice(device);
            }
        }

        /// <summary>Shop 버튼 상태를 갱신한다.</summary>
        private void RefreshButtonStates()
        {
            if (nextButton != null)
                nextButton.interactable = runSession != null;
        }

        /// <summary>Device 슬롯 View를 Shop 재정렬 가능 상태로 초기화한다.</summary>
        private void InitializeDeviceSlotViews()
        {
            if (equippedDeviceSlotViews == null)
                return;

            for (int i = 0; i < equippedDeviceSlotViews.Length; i++)
            {
                if (equippedDeviceSlotViews[i] == null)
                    continue;

                // Shop에서도 Device 순서를 바꿀 수 있어야 하므로 this를 reorderHandler로 넘긴다.
                equippedDeviceSlotViews[i].Initialize(i, this);
            }
        }

        /// <summary>상품 구매 가능 여부와 불가 사유를 반환한다.</summary>
        private bool CanBuyProduct(ShopProductDefinitionSO product, out string unavailableReason)
        {
            unavailableReason = string.Empty;

            if (runSession == null)
            {
                unavailableReason = "No Session";
                return false;
            }

            if (product == null || !product.IsValidProduct())
            {
                unavailableReason = "Invalid";
                return false;
            }

            if (runSession.Parts < product.Price)
            {
                unavailableReason = "No Parts";
                return false;
            }

            if (product.ProductType == ShopProductType.SlotPairDevice && !HasEmptyDeviceSlot())
            {
                unavailableReason = "Full";
                return false;
            }

            return true;
        }

        /// <summary>빈 Device 슬롯이 있는지 확인한다.</summary>
        private bool HasEmptyDeviceSlot()
        {
            if (runSession == null)
                return false;

            for (int i = 0; i < runSession.EquippedSlotPairDevices.Count; i++)
            {
                if (runSession.EquippedSlotPairDevices[i] == null)
                    return true;
            }

            return false;
        }

        /// <summary>상품 클릭 시 구매를 시도한다.</summary>
        private void HandleProductClicked(ShopProductDefinitionSO product)
        {
            if (runSession == null)
            {
                RefreshAll("No active run session.");
                return;
            }

            bool purchased = runSession.TryBuyProduct(product, out string resultMessage);

            RefreshAll(purchased ? resultMessage : $"Cannot buy. {resultMessage}");
        }

        /// <summary>다음 진행 버튼 클릭을 외부 Root로 전달한다.</summary>
        private void HandleNextClicked()
        {
            NextRequested?.Invoke();
        }

        /// <summary>지정 인덱스의 장착 Device를 반환한다.</summary>
        private SlotPairDeviceDefinitionSO GetEquippedDeviceOrNull(int index)
        {
            if (runSession == null)
                return null;

            if (index < 0 || index >= runSession.EquippedSlotPairDevices.Count)
                return null;

            return runSession.EquippedSlotPairDevices[index];
        }

        /// <summary>RunSession에 저장된 장착 Device 슬롯 두 개를 교환한다.</summary>
        private bool TrySwapEquippedSlotPairDevices(int sourceSlotIndex, int targetSlotIndex)
        {
            if (runSession == null)
                return false;

            // 실제 장착 배열은 RunSession 내부가 소유하므로 Presenter에서는 전용 메서드만 호출한다.
            return runSession.SwapEquippedDevices(sourceSlotIndex, targetSlotIndex);
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
