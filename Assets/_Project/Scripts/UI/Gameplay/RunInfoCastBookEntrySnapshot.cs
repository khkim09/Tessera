using Tessera.Core;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 UI 한 줄에 표시할 계산 스냅샷이다.</summary>
    public class RunInfoCastBookEntrySnapshot
    {
        /// <summary>표시 대상 Cast 타입이다.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>족보 표시 이름이다.</summary>
        public string CastName { get; }

        /// <summary>현재 덱/장비 기준 최고 Score다.</summary>
        public int Score { get; }

        /// <summary>현재 덱/장비 기준 최고 Force 숫자값이다.</summary>
        public float ForceValue { get; }

        /// <summary>Force 표시 문자열이다.</summary>
        public string ForceText { get; }

        /// <summary>정렬 기준으로 사용할 최종 CastPower다.</summary>
        public int CastPower { get; }

        /// <summary>이번 Round에서 남은 사용 횟수다.</summary>
        public int RemainingUses { get; }

        /// <summary>이번 Round에서 허용되는 최대 사용 횟수다.</summary>
        public int MaxUses { get; }

        /// <summary>무제한 사용 Cast인지 여부다.</summary>
        public bool IsUnlimited { get; }

        /// <summary>동점 정렬용 기본 Cast 순서다.</summary>
        public int SortOrder { get; }

        /// <summary>현재 사용 불가 상태인지 여부다.</summary>
        public bool IsUnavailable => !IsUnlimited && RemainingUses <= 0;

        /// <summary>사용 횟수 표시 문자열이다.</summary>
        public string RemainingUseText => IsUnlimited ? "∞" : $"{RemainingUses}/{MaxUses}";

        /// <summary>RunInfo 족보 UI 표시용 스냅샷을 생성한다.</summary>
        public RunInfoCastBookEntrySnapshot(
            RollPatternType patternType,
            string castName,
            int score,
            float forceValue,
            string forceText,
            int castPower,
            int remainingUses,
            int maxUses,
            bool isUnlimited,
            int sortOrder)
        {
            PatternType = patternType;
            CastName = string.IsNullOrWhiteSpace(castName) ? patternType.ToString() : castName;
            Score = score;
            ForceValue = forceValue;
            ForceText = string.IsNullOrWhiteSpace(forceText) ? "0" : forceText;
            CastPower = castPower;
            RemainingUses = remainingUses < 0 ? 0 : remainingUses;
            MaxUses = maxUses <= 0 ? 1 : maxUses;
            IsUnlimited = isUnlimited;
            SortOrder = sortOrder;
        }
    }
}
