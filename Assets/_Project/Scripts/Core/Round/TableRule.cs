using System;

namespace Tessera.Core
{
    /// <summary>Round 또는 Boss Round에서 적용되는 단일 테이블 규칙을 표현한다.</summary>
    public class TableRule
    {
        /// <summary>테이블 규칙 종류.</summary>
        public TableRuleType RuleType { get; }

        /// <summary>규칙에서 사용하는 정수 값.</summary>
        public int Value { get; }

        /// <summary>디버그 및 UI 표시용 설명.</summary>
        public string Description { get; }

        /// <summary>테이블 규칙을 생성한다.</summary>
        public TableRule(TableRuleType ruleType, int value, string description)
        {
            RuleType = ruleType;
            Value = value;
            Description = description ?? string.Empty;
        }

        /// <summary>Aces가 아닌 Cast의 피해를 지정 비율로 줄이는 규칙을 생성한다.</summary>
        public static TableRule NonAcesDamagePercent(int percent)
        {
            if (percent < 0)
                throw new ArgumentOutOfRangeException(nameof(percent), "피해 비율은 음수가 될 수 없습니다.");

            return new TableRule(
                TableRuleType.NonAcesDamagePercent,
                percent,
                $"Non-Aces Cast damage becomes {percent}%.");
        }

        /// <summary>Chance Cast를 사용할 수 없게 만드는 규칙을 생성한다.</summary>
        public static TableRule DisableChance()
        {
            return new TableRule(
                TableRuleType.DisableChance,
                0,
                "Chance is disabled.");
        }

        /// <summary>Broken Cast 보상을 비활성화하는 규칙을 생성한다.</summary>
        public static TableRule DisableBrokenCastReward()
        {
            return new TableRule(
                TableRuleType.DisableBrokenCastReward,
                0,
                "Broken Cast reward is disabled.");
        }
    }
}
