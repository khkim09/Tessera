using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>현재 Round의 Cast Board 전체 표시 데이터를 담는다.</summary>
    public class CastBoardViewModel
    {
        private readonly List<CastBoardEntryModel> _entries;

        /// <summary>Cast Board 행 목록.</summary>
        public IReadOnlyList<CastBoardEntryModel> Entries => _entries;

        /// <summary>추천 Cast 카테고리.</summary>
        public RollPatternType RecommendedPatternType { get; }

        /// <summary>추천 Cast 예상 피해량.</summary>
        public int RecommendedDamage { get; }

        /// <summary>Cast Board 전체 표시 데이터를 생성한다.</summary>
        public CastBoardViewModel(
            IReadOnlyList<CastBoardEntryModel> entries,
            RollPatternType recommendedPatternType,
            int recommendedDamage)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            _entries = new List<CastBoardEntryModel>(entries);
            RecommendedPatternType = recommendedPatternType;
            RecommendedDamage = recommendedDamage;
        }

        /// <summary>지정한 Cast 카테고리의 행 데이터를 찾는다.</summary>
        public bool TryGetEntry(RollPatternType patternType, out CastBoardEntryModel entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].PatternType != patternType)
                    continue;

                entry = _entries[i];
                return true;
            }

            entry = null;
            return false;
        }
    }
}
