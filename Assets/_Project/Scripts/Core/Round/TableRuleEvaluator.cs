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
            if (patternResult == null)
                return new TableRuleEvaluationResult(false, 0, 0, false, "No pattern result.");

            return Evaluate(ruleContext, patternResult.PatternType, patternResult.FinalDamage);
        }

        /// <summary>Round 규칙과 별도 피해값을 이용해 테이블 규칙 적용 결과를 계산한다.</summary>
        public static TableRuleEvaluationResult Evaluate(
            RoundRuleContext ruleContext,
            RollPatternType patternType,
            int damageBeforeTableRules)
        {
            if (ruleContext == null)
                return new TableRuleEvaluationResult(false, damageBeforeTableRules, damageBeforeTableRules, false, "No round rule context.");

            IReadOnlyList<TableRule> tableRules = ruleContext.TableRules;
            int modifiedDamage = damageBeforeTableRules;
            bool isBlocked = false;
            bool suppressBrokenReward = false;
            StringBuilder messageBuilder = new StringBuilder();

            for (int i = 0; i < tableRules.Count; i++)
            {
                TableRule rule = tableRules[i];

                if (rule.RuleType == TableRuleType.NonAcesDamagePercent)
                {
                    if (patternType != RollPatternType.Aces && patternType != RollPatternType.BrokenCast)
                    {
                        modifiedDamage = modifiedDamage * rule.Value / 100;
                        AppendRuleMessage(messageBuilder, rule.Description);
                    }
                }

                if (rule.RuleType == TableRuleType.DisableChance)
                {
                    if (patternType == RollPatternType.Chance)
                    {
                        isBlocked = true;
                        AppendRuleMessage(messageBuilder, rule.Description);
                    }
                }

                if (rule.RuleType == TableRuleType.DisableBrokenCastReward)
                {
                    if (patternType == RollPatternType.BrokenCast)
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
                damageBeforeTableRules,
                modifiedDamage,
                suppressBrokenReward,
                message);
        }

        /// <summary>테이블 규칙 메시지를 공백 구분으로 이어 붙인다.</summary>
        private static void AppendRuleMessage(StringBuilder builder, string message)
        {
            if (builder.Length > 0)
                builder.Append(" ");

            builder.Append(message);
        }
    }
}
