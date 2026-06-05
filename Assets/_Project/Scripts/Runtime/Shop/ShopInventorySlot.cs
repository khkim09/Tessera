using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>Workshop 방문 중 생성된 Shop 상품 슬롯 런타임 상태다.</summary>
    public class ShopInventorySlot
    {
        /// <summary>Shop 슬롯 인덱스.</summary>
        public int SlotIndex { get; }

        /// <summary>상품 정의.</summary>
        public ShopProductDefinitionSO ProductDefinition { get; }

        /// <summary>Money 가격.</summary>
        public int MoneyPrice { get; }

        /// <summary>Overcharge 가격.</summary>
        public int OverchargePrice { get; }

        /// <summary>판매 완료 여부.</summary>
        public bool IsSoldOut { get; private set; }

        /// <summary>Shop 상품 슬롯 상태를 생성한다.</summary>
        public ShopInventorySlot(
            int slotIndex,
            ShopProductDefinitionSO productDefinition,
            int moneyPrice,
            int overchargePrice)
        {
            SlotIndex = Mathf.Max(0, slotIndex);
            ProductDefinition = productDefinition;
            MoneyPrice = Mathf.Max(0, moneyPrice);
            OverchargePrice = Mathf.Max(0, overchargePrice);
            IsSoldOut = false;
        }

        /// <summary>상품을 판매 완료 상태로 변경한다.</summary>
        public void MarkSoldOut()
        {
            IsSoldOut = true;
        }
    }
}
