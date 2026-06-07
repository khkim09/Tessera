namespace Tessera.Core
{
    /// <summary>상대 Roll AI가 다음 Roll 전에 어떤 주사위를 유지할지 결정하는 전략이다.</summary>
    public enum OpponentRollStrategyType
    {
        /// <summary>현재 최고 피해 Cast 기준으로 필요한 주사위를 유지한다.</summary>
        GreedyBestDamage = 0,

        /// <summary>가장 높은 중복 그룹을 우선 유지한다.</summary>
        KeepHighestGroup = 1,

        /// <summary>스트레이트 완성을 우선 노린다.</summary>
        ChaseStraight = 2,

        /// <summary>중복/스트레이트/고눈금을 상황별로 섞어 판단한다.</summary>
        Balanced = 3
    }
}
