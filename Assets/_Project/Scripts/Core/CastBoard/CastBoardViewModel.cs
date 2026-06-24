using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>현재 Round의 Cast Board 전체 표시 데이터를 담는다.</summary>
    public class CastBoardViewModel
    {
        private readonly List<CastBoardEntryModel> entries;

        /// <summary>Cast Board 행 목록이다.</summary>
        public IReadOnlyList<CastBoardEntryModel> Entries => entries;

        /// <summary>추천 Cast 카테고리다.</summary>
        public RollPatternType RecommendedPatternType { get; }

        /// <summary>추천 CastPower 값이다.</summary>
        public int RecommendedCastPower { get; }

        /// <summary>Cast Board 전체 표시 데이터를 생성한다.</summary>
        public CastBoardViewModel(
            IReadOnlyList<CastBoardEntryModel> entries,
            RollPatternType recommendedPatternType,
            int recommendedCastPower)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            this.entries = new List<CastBoardEntryModel>(entries);
            RecommendedPatternType = recommendedPatternType;
            RecommendedCastPower = recommendedCastPower;
        }

        /// <summary>지정한 Cast 카테고리의 행 데이터를 찾는다.</summary>
        public bool TryGetEntry(RollPatternType patternType, out CastBoardEntryModel entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PatternType != patternType)
                    continue;

                entry = entries[i];
                return true;
            }

            entry = null;
            return false;
        }
    }
}
