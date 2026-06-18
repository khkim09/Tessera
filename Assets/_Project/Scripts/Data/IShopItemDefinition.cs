using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Shop 상품으로 판매 가능한 원본 아이템 정의가 제공해야 하는 공통 표시/가격 정보다.</summary>
    public interface IShopItemDefinition
    {
        /// <summary>아이템 고유 ID다.</summary>
        string ItemId { get; }

        /// <summary>아이템 표시 이름이다.</summary>
        string DisplayName { get; }

        /// <summary>아이템 설명이다.</summary>
        string Description { get; }

        /// <summary>아이템 Tier다.</summary>
        int Tier { get; }

        /// <summary>아이템 아이콘이다.</summary>
        Sprite Icon { get; }

        /// <summary>기본 Money 가격이다.</summary>
        int BaseMoneyPrice { get; }

        /// <summary>기본 Overcharge 가격이다.</summary>
        int BaseOverchargePrice { get; }
    }
}
