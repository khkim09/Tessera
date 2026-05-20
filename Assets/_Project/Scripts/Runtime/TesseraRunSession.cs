using Tessera.Data;

namespace Tessera.Runtime
{
    /// <summary>한 Run 동안 유지되는 Stage, Round, 재화, 장착 Device 상태를 관리한다.</summary>
    public class TesseraRunSession
    {
        public const int MaxDeviceSlots = 5;

        private readonly SlotPairDeviceDefinitionSO[] equippedSlotPairDevices = new SlotPairDeviceDefinitionSO[MaxDeviceSlots];

        /// <summary>현재 Stage 인덱스를 반환한다.</summary>
        public int CurrentStageIndex { get; private set; }

        /// <summary>현재 Round 인덱스를 반환한다.</summary>
        public int CurrentRoundIndex { get; private set; }

        /// <summary>현재 보유 Parts를 반환한다.</summary>
        public int Parts { get; private set; }

        /// <summary>현재 장착된 SlotPair Device 배열을 반환한다.</summary>
        public IReadOnlyListWrapper EquippedSlotPairDevices => new IReadOnlyListWrapper(equippedSlotPairDevices);

        /// <summary>RunSession을 초기값으로 생성한다.</summary>
        public TesseraRunSession(int startParts = 0)
        {
            Parts = startParts;
            CurrentStageIndex = 0;
            CurrentRoundIndex = 0;
        }

        /// <summary>보유 Parts를 증가시킨다.</summary>
        public void AddParts(int amount)
        {
            if (amount <= 0)
                return;

            Parts += amount;
        }

        /// <summary>지정 Parts를 지불할 수 있으면 차감한다.</summary>
        public bool TrySpendParts(int amount)
        {
            if (amount < 0)
                return false;

            if (Parts < amount)
                return false;

            Parts -= amount;
            return true;
        }

        /// <summary>현재 장착 Device를 지정 슬롯에 강제로 설정한다.</summary>
        public bool SetEquippedDevice(int slotIndex, SlotPairDeviceDefinitionSO device)
        {
            if (!IsValidDeviceSlotIndex(slotIndex))
                return false;

            equippedSlotPairDevices[slotIndex] = device;
            return true;
        }

        /// <summary>현재 장착 Device 슬롯 두 개를 서로 교체한다.</summary>
        public bool SwapEquippedDevices(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidDeviceSlotIndex(sourceSlotIndex))
                return false;

            if (!IsValidDeviceSlotIndex(targetSlotIndex))
                return false;

            if (sourceSlotIndex == targetSlotIndex)
                return true;

            SlotPairDeviceDefinitionSO temp = equippedSlotPairDevices[sourceSlotIndex];
            equippedSlotPairDevices[sourceSlotIndex] = equippedSlotPairDevices[targetSlotIndex];
            equippedSlotPairDevices[targetSlotIndex] = temp;

            return true;
        }

        /// <summary>첫 번째 빈 Device 슬롯에 Device를 장착한다.</summary>
        public bool TryEquipDeviceToFirstEmptySlot(SlotPairDeviceDefinitionSO device, out int equippedSlotIndex)
        {
            equippedSlotIndex = -1;

            if (device == null)
                return false;

            int emptySlotIndex = FindFirstEmptyDeviceSlotIndex();

            if (emptySlotIndex < 0)
                return false;

            equippedSlotPairDevices[emptySlotIndex] = device;
            equippedSlotIndex = emptySlotIndex;
            return true;
        }

        /// <summary>Shop 상품 구매를 시도하고 성공 시 즉시 장착 또는 적용한다.</summary>
        public bool TryBuyProduct(ShopProductDefinitionSO product, out string resultMessage)
        {
            resultMessage = "Purchase failed.";

            if (product == null || !product.IsValidProduct())
            {
                resultMessage = "Invalid product.";
                return false;
            }

            if (Parts < product.Price)
            {
                resultMessage = "Not enough parts.";
                return false;
            }

            if (product.ProductType == ShopProductType.SlotPairDevice)
                return TryBuySlotPairDevice(product, out resultMessage);

            resultMessage = "This product type is not implemented yet.";
            return false;
        }

        /// <summary>Device 상품 구매를 처리한다.</summary>
        private bool TryBuySlotPairDevice(ShopProductDefinitionSO product, out string resultMessage)
        {
            resultMessage = "Device purchase failed.";

            if (FindFirstEmptyDeviceSlotIndex() < 0)
            {
                resultMessage = "Device slots are full.";
                return false;
            }

            if (!TrySpendParts(product.Price))
            {
                resultMessage = "Not enough parts.";
                return false;
            }

            if (!TryEquipDeviceToFirstEmptySlot(product.SlotPairDevice, out int equippedSlotIndex))
            {
                // 장착 실패 시 Parts를 복구한다.
                AddParts(product.Price);
                resultMessage = "No empty device slot.";
                return false;
            }

            resultMessage = $"Bought {product.DisplayName}. Equipped to slot {equippedSlotIndex + 1}.";
            return true;
        }

        /// <summary>첫 번째 빈 Device 슬롯 인덱스를 찾는다.</summary>
        private int FindFirstEmptyDeviceSlotIndex()
        {
            for (int i = 0; i < equippedSlotPairDevices.Length; i++)
            {
                if (equippedSlotPairDevices[i] == null)
                    return i;
            }

            return -1;
        }

        /// <summary>Device 슬롯 인덱스가 유효한지 확인한다.</summary>
        private static bool IsValidDeviceSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxDeviceSlots;
        }

        /// <summary>배열 노출을 최소화하기 위한 읽기 전용 래퍼다.</summary>
        public readonly struct IReadOnlyListWrapper
        {
            private readonly SlotPairDeviceDefinitionSO[] source;

            /// <summary>래퍼를 생성한다.</summary>
            public IReadOnlyListWrapper(SlotPairDeviceDefinitionSO[] source)
            {
                this.source = source;
            }

            /// <summary>요소 개수를 반환한다.</summary>
            public int Count => source != null ? source.Length : 0;

            /// <summary>지정 인덱스의 Device를 반환한다.</summary>
            public SlotPairDeviceDefinitionSO this[int index]
            {
                get
                {
                    if (source == null)
                        return null;

                    if (index < 0 || index >= source.Length)
                        return null;

                    return source[index];
                }
            }
        }
    }
}
