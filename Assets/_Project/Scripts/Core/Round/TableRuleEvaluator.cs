using System.Collections.Generic;
using System.Text;

namespace Tessera.Core
{
    /// <summary>Round에 적용된 테이블 규칙이 Cast 피해와 사용 가능 여부에 미치는 영향을 계산한다.</summary>
    public class TableRuleEvaluator
    {
        /// <summary>Round 규칙과 Cast 결과를 이용해 테이블 규칙 적용 결과를 계산한다.</summary>
        public static TableRuleEvaluationResult Evaluate(RoundRuleContext ruleContext, PatternResult patternResult)
        {
            if (ruleContext == null)
                return new TableRuleEvaluationResult(false, patternResult.FinalDamage, patternResult.FinalDamage, false, "No round rule context.");

            IReadOnlyList<TableRule> tableRules = ruleContext.TableRules;
            int originalDamage = patternResult.FinalDamage;
            int modifiedDamage = patternResult.FinalDamage;
            bool isBlocked = false;
            bool suppressBrokenReward = false;
            StringBuilder messageBuilder = new StringBuilder();

            for (int i = 0; i < tableRules.Count; i++)
            {
                TableRule rule = tableRules[i];

                if (rule.RuleType == TableRuleType.NonAcesDamagePercent)
                {
                    if (patternResult.PatternType != RollPatternType.Aces && patternResult.PatternType != RollPatternType.BrokenCast)
                    {
                        modifiedDamage = modifiedDamage * rule.Value / 100;
                        AppendRuleMessage(messageBuilder, rule.Description);
                    }
                }

                if (rule.RuleType == TableRuleType.DisableChance)
                {
                    if (patternResult.PatternType == RollPatternType.Chance)
                    {
                        isBlocked = true;
                        AppendRuleMessage(messageBuilder, rule.Description);
                    }
                }

                if (rule.RuleType == TableRuleType.DisableBrokenCastReward)
                {
                    if (patternResult.PatternType == RollPatternType.BrokenCast)
                    {
                        suppressBrokenReward = true;
                        AppendRuleMessage(messageBuilder, rule.Description);
                    }
                }
            }

            string message = messageBuilder.Length > 0
                ? messageBuilder.ToString()
                : "No table rule effect.";

            return new TableRuleEvaluationResult(
                isBlocked,
                originalDamage,
                modifiedDamage,
                suppressBrokenReward,
                message);
        }

        private static void AppendRuleMessage(StringBuilder builder, string message)
        {
            if (builder.Length > 0)
                builder.Append(" ");

            builder.Append(message);
        }
    }
}
