using System.Collections.Generic;
using Tessera.Core;

namespace Tessera.UI
{
    /// <summary>RunInfo Cast Tooltip에 표시할 설명과 미리보기 데이터를 담는다.</summary>
    public class RunInfoCastBookTooltipContent
    {
        /// <summary>설명 대상 Cast 타입이다.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>한국어 설명 텍스트다.</summary>
        public string DescriptionKr { get; }

        /// <summary>영어 설명 텍스트다.</summary>
        public string DescriptionEn { get; }

        /// <summary>미리보기 슬롯에 표시할 주사위 눈금 목록이다.</summary>
        public IReadOnlyList<int> PreviewDiceValues { get; }

        /// <summary>미리보기에서 강조할 슬롯 인덱스 목록이다.</summary>
        public IReadOnlyList<int> EmphasizedIndexes { get; }

        /// <summary>RunInfo Cast Tooltip 표시 데이터를 생성한다.</summary>
        public RunInfoCastBookTooltipContent(
            RollPatternType patternType,
            string descriptionKr,
            string descriptionEn,
            IReadOnlyList<int> previewDiceValues,
            IReadOnlyList<int> emphasizedIndexes)
        {
            PatternType = patternType;
            DescriptionKr = descriptionKr ?? string.Empty;
            DescriptionEn = descriptionEn ?? string.Empty;
            PreviewDiceValues = previewDiceValues ?? new int[0];
            EmphasizedIndexes = emphasizedIndexes ?? new int[0];
        }
    }
}
