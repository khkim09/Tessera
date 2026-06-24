namespace Tessera.Core
{
    /// <summary>상대가 가능한 Cast 후보 중 최종 제출 Cast를 고르는 정책이다.</summary>
    public enum OpponentCastSelectionPolicy
    {
        /// <summary>출시용 기본 효용 점수 최고 Cast를 선택한다.</summary>
        UtilityBest = 0, // 출시용 기본 정책

        /// <summary>Intent 목표 구간에 먼저 도달한 합리적 Cast를 우선 선택한다.</summary>
        TargetBandFirst = 1, // 튜토리얼/저난도 정책

        /// <summary>Intent가 선호하는 Cast를 합리적 후보 안에서 우선 선택한다.</summary>
        PreferredCastFirst = 2, // 성격 있는 적 전용

        /// <summary>디버그용 첫 번째 유효 Cast를 선택한다.</summary>
        DebugFirstValid = 3 // 디버그 전용
    }
}
