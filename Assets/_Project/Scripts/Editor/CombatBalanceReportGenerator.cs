using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tessera.Core;
using Tessera.Data;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 전투 밸런스 검증용 콘솔 및 파일 리포트를 생성한다.</summary>
    public class CombatBalanceReportGenerator
    {
        /// <summary>전체 리포트 로그에 붙이는 기본 접두사다.</summary>
        private const string ReportPrefix = "[Tessera][BalanceReport]";

        /// <summary>Cast 계산표 로그에 붙이는 접두사다.</summary>
        private const string CastPrefix = "[Tessera][BalanceReport][Cast]";

        /// <summary>RoundDefinition 로그에 붙이는 접두사다.</summary>
        private const string RoundPrefix = "[Tessera][BalanceReport][Round]";

        /// <summary>클리어 가능성 판정 로그에 붙이는 접두사다.</summary>
        private const string VerdictPrefix = "[Tessera][BalanceReport][Verdict]";

        /// <summary>검증 로그 파일을 저장할 프로젝트 상대 폴더다.</summary>
        private const string DebugFolderPath = "Assets/_Project/Debug";

        /// <summary>최근 검증 로그를 덮어쓸 파일 이름이다.</summary>
        private const string LatestReportFileName = "CombatBalanceReport_Latest.txt";

        /// <summary>Shop 리포트 로그에 붙이는 접두사다.</summary>
        private const string ShopPrefix = "[Tessera][ShopReport][Product]";

        /// <summary>Unity 메뉴에서 v4.4 밸런스 리포트를 생성한다.</summary>
        [MenuItem("Tools/Tessera/Combat/Print v4.4 Balance Report")]
        public static void PrintV44BalanceReport()
        {
            string reportText = BuildReportText();
            Debug.Log(reportText);
            WriteReportFiles(reportText);
            AssetDatabase.Refresh();
        }

        /// <summary>콘솔과 txt 파일에 출력할 전체 리포트 문자열을 생성한다.</summary>
        private static string BuildReportText()
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine($"{ReportPrefix} v4.4 Combat Balance Report generatedAtUtc={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"{ReportPrefix} ImpactFormula PowerTier=Floor(CastPower/45),Max7 MarginTier=Floor(Margin/35),Max6 ImpactCap<=0 disables cap");
            AppendCastTable(builder);
            AppendRoundReports(builder);
            AppendShopReport(builder);
            builder.AppendLine($"{ReportPrefix} End of v4.4 Combat Balance Report");
            return builder.ToString();
        }

        /// <summary>Shop 상품 요약을 리포트에 추가한다.</summary>
        private static void AppendShopReport(StringBuilder builder)
        {
            builder.AppendLine($"{ReportPrefix} Section=D Shop product report");

            string[] shopProductGuids = AssetDatabase.FindAssets("t:ShopProductDefinitionSO");
            if (shopProductGuids.Length == 0)
            {
                builder.AppendLine($"{ShopPrefix} No ShopProductDefinitionSO assets found.");
                return;
            }

            for (int i = 0; i < shopProductGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(shopProductGuids[i]);
                ShopProductDefinitionSO product = AssetDatabase.LoadAssetAtPath<ShopProductDefinitionSO>(assetPath);

                if (product == null)
                    continue;

                string productName = product.DisplayName;
                string productType = product.ProductType.ToString();
                int tier = product.Tier;
                int price = product.BaseMoneyPrice;
                string linkedItem = "None";
                string description = product.Description;

                // 연결된 아이템 정보
                if (product.DeviceDefinition != null)
                    linkedItem = product.DeviceDefinition.DeviceId;
                else if (product.DiceTypeDefinition != null)
                    linkedItem = product.DiceTypeDefinition.DiceTypeId;
                else if (product.DiceFaceUpgradeDefinition != null)
                    linkedItem = product.DiceFaceUpgradeDefinition.UpgradeId;

                // 미구현 경고
                string warning = string.Empty;
                if (description.Contains("not implemented", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("on hold", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("Candidate", StringComparison.OrdinalIgnoreCase))
                {
                    warning = " [WARNING: 미구현/보류 상품]";
                }

                builder.AppendLine($"{ShopPrefix} ProductName={productName} ProductType={productType} Tier={tier} Price={price} LinkedItem={linkedItem} Description={description}{warning}");
            }
        }

        /// <summary>Cast별 기본 계산표를 리포트에 추가한다.</summary>
        private static void AppendCastTable(StringBuilder builder)
        {
            IReadOnlyDictionary<RollPatternType, PatternDefinition> definitions = PatternDefinition.CreateDefaultDefinitions();
            IReadOnlyDictionary<RollPatternType, int> exampleScores = CreateExampleRawCastScores();
            RollPatternType[] patternOrder = CreatePatternOrder();

            builder.AppendLine($"{ReportPrefix} Section=A Cast base calculation table");

            for (int i = 0; i < patternOrder.Length; i++)
            {
                RollPatternType patternType = patternOrder[i];

                if (!definitions.TryGetValue(patternType, out PatternDefinition definition))
                    continue;

                int rawCastScore = exampleScores.TryGetValue(patternType, out int exampleScore) ? exampleScore : 0;
                int exampleCastPower = patternType == RollPatternType.BrokenCast
                    ? 0
                    : (rawCastScore + definition.FlatBonus) * definition.BaseForce + definition.TruePower;
                int powerTierBonus = ImpactDamageCalculator.CalculatePowerTierBonusForDebug(exampleCastPower);
                string impactSamples = BuildImpactSampleText(definition.BaseImpact, powerTierBonus, patternType);

                builder.AppendLine($"{CastPrefix} DisplayName={patternType} RawCastScore={rawCastScore} BaseForce={definition.BaseForce} BaseImpact={definition.BaseImpact} ExampleCastPower={exampleCastPower} PowerTierBonus={powerTierBonus} ExpectedImpactDamage[{impactSamples}]");
            }
        }

        /// <summary>예시 Margin별 ExpectedImpactDamage 문자열을 생성한다.</summary>
        private static string BuildImpactSampleText(int baseImpact, int powerTierBonus, RollPatternType patternType)
        {
            int[] margins = new int[] { 0, 35, 70, 105 };
            StringBuilder builder = new StringBuilder(64);

            for (int i = 0; i < margins.Length; i++)
            {
                int margin = margins[i];
                int expectedImpactDamage = patternType == RollPatternType.BrokenCast
                    ? 0
                    : baseImpact + powerTierBonus + ImpactDamageCalculator.CalculateMarginTierBonusForDebug(margin);

                if (i > 0)
                    builder.Append(", ");

                builder.Append("Margin");
                builder.Append(margin);
                builder.Append("=");
                builder.Append(expectedImpactDamage);
            }

            return builder.ToString();
        }

        /// <summary>Stage 1 및 Tutorial RoundDefinition 정보를 리포트에 추가한다.</summary>
        private static void AppendRoundReports(StringBuilder builder)
        {
            List<StageRoundAssetInfo> roundInfos = FindStageRoundAssets();
            int goodCastImpact = CalculateExampleExpectedImpact(RollPatternType.FullHouse, 25, 35);
            int strongCastImpact = CalculateExampleExpectedImpact(RollPatternType.LargeStraight, 40, 70);

            builder.AppendLine($"{ReportPrefix} Section=B Stage 1 RoundDefinition report");
            builder.AppendLine($"{ReportPrefix} Section=C Clearability estimate GoodCast=FullHouse@Margin35:{goodCastImpact} StrongCast=LargeStraight@Margin70:{strongCastImpact}");

            if (roundInfos.Count == 0)
            {
                builder.AppendLine($"{RoundPrefix} No StageRoundDefinitionSO assets found by AssetDatabase.FindAssets(\"t:StageRoundDefinitionSO\")");
                return;
            }

            for (int i = 0; i < roundInfos.Count; i++)
            {
                StageRoundAssetInfo info = roundInfos[i];
                StageRoundDefinitionSO roundDefinition = info.RoundDefinition;

                if (roundDefinition == null)
                    continue;

                EnemyIntentDefinitionSO openingIntent = roundDefinition.OpeningIntent;
                string openingIntentName = openingIntent != null ? openingIntent.DisplayName : "None";
                string intentProfileName = roundDefinition.IntentProfile != null ? roundDefinition.IntentProfile.name : "None";
                string intentStopText = BuildIntentStopText(roundDefinition);

                RoundRuleContext ruleContext = roundDefinition.BuildRuleContext(0);
                int requiredAverageImpact = Mathf.CeilToInt((float)ruleContext.OpponentMaxHP / ruleContext.MaxAttempts);
                string verdict = ResolveClearabilityVerdict(requiredAverageImpact, goodCastImpact, strongCastImpact);
                int baseRollsPerAttempt = ruleContext.BaseRollsPerAttempt;

                builder.AppendLine($"{RoundPrefix} Path={info.AssetPath} RoundId={roundDefinition.RoundId} DisplayName={roundDefinition.DisplayName} OpponentMaxHP={ruleContext.OpponentMaxHP} MaxAttempts={ruleContext.MaxAttempts} BaseRollsPerAttempt={baseRollsPerAttempt} StartingExtraRollCharge=0 ImpactCap={ruleContext.ImpactCap} OpeningIntent={openingIntentName} IntentProfile={intentProfileName} IntentStops={intentStopText}");
                builder.AppendLine($"{VerdictPrefix} RoundId={roundDefinition.RoundId} RequiredAverageImpact={requiredAverageImpact} GoodCastImpact={goodCastImpact} StrongCastImpact={strongCastImpact} BaseRollsPerAttempt={baseRollsPerAttempt} StartingExtraRollCharge=0 Evaluation={verdict}");
            }
        }

        /// <summary>지정 Cast와 Margin의 예시 ExpectedImpactDamage를 계산한다.</summary>
        private static int CalculateExampleExpectedImpact(RollPatternType patternType, int rawCastScore, int margin)
        {
            IReadOnlyDictionary<RollPatternType, PatternDefinition> definitions = PatternDefinition.CreateDefaultDefinitions();

            if (!definitions.TryGetValue(patternType, out PatternDefinition definition))
                return 0;

            int castPower = patternType == RollPatternType.BrokenCast
                ? 0
                : (rawCastScore + definition.FlatBonus) * definition.BaseForce + definition.TruePower;

            return definition.BaseImpact +
                    ImpactDamageCalculator.CalculatePowerTierBonusForDebug(castPower) +
                    ImpactDamageCalculator.CalculateMarginTierBonusForDebug(margin);
        }

        /// <summary>AssetDatabase에서 Stage 1 또는 Tutorial RoundDefinition 에셋을 우선 검색한다.</summary>
        private static List<StageRoundAssetInfo> FindStageRoundAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageRoundDefinitionSO");
            List<StageRoundAssetInfo> preferredInfos = new List<StageRoundAssetInfo>();
            List<StageRoundAssetInfo> fallbackInfos = new List<StageRoundAssetInfo>();

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                StageRoundDefinitionSO roundDefinition = AssetDatabase.LoadAssetAtPath<StageRoundDefinitionSO>(assetPath);

                if (roundDefinition == null)
                    continue;

                StageRoundAssetInfo info = new StageRoundAssetInfo(assetPath, roundDefinition);

                if (IsPreferredStageRoundAsset(assetPath, roundDefinition))
                    preferredInfos.Add(info);
                else
                    fallbackInfos.Add(info);
            }

            preferredInfos.Sort(CompareStageRoundAssetInfo);
            fallbackInfos.Sort(CompareStageRoundAssetInfo);
            return preferredInfos.Count > 0 ? preferredInfos : fallbackInfos;
        }

        /// <summary>Stage 1 또는 Tutorial 이름을 가진 RoundDefinition인지 확인한다.</summary>
        private static bool IsPreferredStageRoundAsset(string assetPath, StageRoundDefinitionSO roundDefinition)
        {
            string haystack = $"{assetPath} {roundDefinition.name} {roundDefinition.RoundId} {roundDefinition.DisplayName}";
            return haystack.IndexOf("Stage01", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    haystack.IndexOf("Stage 1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    haystack.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>RoundDefinition 에셋 정보를 경로 기준으로 정렬한다.</summary>
        private static int CompareStageRoundAssetInfo(StageRoundAssetInfo left, StageRoundAssetInfo right)
        {
            return string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>RoundDefinition의 OpeningIntent와 IntentDeck Stop 기준을 문자열로 만든다.</summary>
        private static string BuildIntentStopText(StageRoundDefinitionSO roundDefinition)
        {
            List<string> parts = new List<string>();
            AppendIntentStopText(parts, "Opening", roundDefinition.OpeningIntent);

            IReadOnlyList<EnemyIntentDefinitionSO> intentDeck = roundDefinition.IntentDeck;
            for (int i = 0; i < intentDeck.Count; i++)
                AppendIntentStopText(parts, $"Deck{i}", intentDeck[i]);

            return parts.Count > 0 ? string.Join(" | ", parts) : "None";
        }

        /// <summary>Intent Stop 기준 문자열을 목록에 추가한다.</summary>
        private static void AppendIntentStopText(List<string> parts, string label, EnemyIntentDefinitionSO intent)
        {
            if (intent == null)
                return;

            parts.Add($"{label}:{intent.DisplayName}(TargetPowerToStop={intent.TargetPowerToStop},TargetImpactToStop={intent.TargetImpactToStop},StopIfBeatsPlayerPower={intent.StopIfBeatsPlayerPower},Policy={intent.CastSelectionPolicy})");
        }

        /// <summary>RequiredAverageImpact와 예시 Impact를 비교해 단순 클리어 가능성 판정을 반환한다.</summary>
        private static string ResolveClearabilityVerdict(int requiredAverageImpact, int goodCastImpact, int strongCastImpact)
        {
            if (requiredAverageImpact <= goodCastImpact)
                return "OK";

            if (requiredAverageImpact <= strongCastImpact)
                return "TIGHT";

            return "TOO_HIGH_HP_OR_TOO_LOW_IMPACT";
        }

        /// <summary>예시 RawCastScore 표를 생성한다.</summary>
        private static IReadOnlyDictionary<RollPatternType, int> CreateExampleRawCastScores()
        {
            return new Dictionary<RollPatternType, int>
            {
                { RollPatternType.Aces, 3 },
                { RollPatternType.Twos, 6 },
                { RollPatternType.Threes, 9 },
                { RollPatternType.Fours, 12 },
                { RollPatternType.Fives, 15 },
                { RollPatternType.Sixes, 18 },
                { RollPatternType.Chance, 18 },
                { RollPatternType.ThreeOfAKind, 20 },
                { RollPatternType.FourOfAKind, 24 },
                { RollPatternType.FullHouse, 25 },
                { RollPatternType.SmallStraight, 30 },
                { RollPatternType.LargeStraight, 40 },
                { RollPatternType.Tessera, 50 },
                { RollPatternType.BrokenCast, 0 }
            };
        }

        /// <summary>리포트에 출력할 Cast 순서를 생성한다.</summary>
        private static RollPatternType[] CreatePatternOrder()
        {
            return new RollPatternType[]
            {
                RollPatternType.Aces,
                RollPatternType.Twos,
                RollPatternType.Threes,
                RollPatternType.Fours,
                RollPatternType.Fives,
                RollPatternType.Sixes,
                RollPatternType.Chance,
                RollPatternType.ThreeOfAKind,
                RollPatternType.FourOfAKind,
                RollPatternType.FullHouse,
                RollPatternType.SmallStraight,
                RollPatternType.LargeStraight,
                RollPatternType.Tessera,
                RollPatternType.BrokenCast
            };
        }

        /// <summary>리포트 문자열을 첨부 가능한 txt 파일로 저장한다.</summary>
        private static void WriteReportFiles(string reportText)
        {
            Directory.CreateDirectory(DebugFolderPath);

            string latestPath = Path.Combine(DebugFolderPath, LatestReportFileName);
            string timestampedPath = Path.Combine(DebugFolderPath, $"CombatBalanceReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(latestPath, reportText, Encoding.UTF8);
            File.WriteAllText(timestampedPath, reportText, Encoding.UTF8);
            Debug.Log($"{ReportPrefix} Saved report files: {latestPath}, {timestampedPath}");
        }

        /// <summary>StageRoundDefinitionSO 에셋 경로와 인스턴스를 함께 보관한다.</summary>
        private class StageRoundAssetInfo
        {
            /// <summary>AssetDatabase 기준 에셋 경로다.</summary>
            public string AssetPath { get; }

            /// <summary>로드된 StageRoundDefinitionSO 에셋이다.</summary>
            public StageRoundDefinitionSO RoundDefinition { get; }

            /// <summary>StageRoundDefinitionSO 에셋 정보를 생성한다.</summary>
            public StageRoundAssetInfo(string assetPath, StageRoundDefinitionSO roundDefinition)
            {
                AssetPath = assetPath ?? string.Empty;
                RoundDefinition = roundDefinition;
            }
        }
    }
}
