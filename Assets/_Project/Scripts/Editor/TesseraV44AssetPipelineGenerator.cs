using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>v4.4 전체 SO를 순차적으로 생성/수정하는 통합 파이프라인 Generator.
    /// 개별 Generator의 GenerateForPipeline()을 올바른 순서로 호출한다.</summary>
    public static class TesseraV44AssetPipelineGenerator
    {
        /// <summary>Tools/Tessera/Assets/Generate v4.4 All SO Assets 메뉴 항목.
        /// v4.4 전체 SO를 올바른 순서로 생성/수정한다.</summary>
        [MenuItem("Tools/Tessera/Assets/Generate v4.4 All SO Assets")]
        private static void GenerateAllV44()
        {
            Debug.Log("[TesseraV44AssetPipelineGenerator] === v4.4 All SO Assets 생성 시작 ===");

            // 1. Device SO 26종 (Common 18종 + Rare 8종)
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 1/10: Device SO v4.4");
            DeviceSOV44Generator.GenerateForPipeline();

            // 2. DiceFaceUpgrade SO 11종
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 2/10: DiceFaceUpgrade SO v4.4");
            DiceFaceUpgradeSOV44Generator.GenerateForPipeline();

            // 3. DiceType SO 8종
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 3/10: DiceType SO v4.4");
            DiceTypeSOV44Generator.GenerateForPipeline();

            // 4. DiceSynergy SO 11종
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 4/10: DiceSynergy SO v4.4");
            DiceSynergySOV44Generator.GenerateForPipeline();

            // 5. EnemyIntent / EnemyIntentProfile / EnemyDiceLoadout SO (4종 + 4종 + 4종)
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 5/10: Enemy Intent Profile Loadout SO v4.4");
            EnemyIntentProfileLoadoutSOV44Generator.GenerateForPipeline();

            // 6. StageRound / StageWorkshopRules / StageDefinition SO (4 Round + 1 WorkshopRules + 1 Stage)
            //    WorkshopRules가 먼저 생성되어야 ShopProduct Generator가 productPool을 채울 수 있다.
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 6/10: Stage Round SO v4.4 (WorkshopRules 포함)");
            Stage01StageRoundSOV44Generator.GenerateForPipeline();

            // 7. Device ShopProduct 26종 + WorkshopRules Slot 0~1 productPool 연결
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 7/10: ShopProduct Device SO v4.4");
            ShopProductDeviceSOV44Generator.GenerateForPipeline();

            // 8. DiceFaceUpgrade ShopProduct 11종 + WorkshopRules Slot 4~5 productPool 연결
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 8/10: ShopProduct DiceFaceUpgrade SO v4.4");
            ShopProductDiceFaceUpgradeSOV44Generator.GenerateForPipeline();

            // 9. DiceType ShopProduct 8종 + WorkshopRules Slot 2~3 productPool 연결
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 9/10: ShopProduct DiceType SO v4.4");
            ShopProductDiceTypeSOV44Generator.GenerateForPipeline();

            // 10. CombatBalanceReport (public 진입점 확인)
            Debug.Log("[TesseraV44AssetPipelineGenerator] Step 10/10: CombatBalanceReport");
            TryGenerateCombatBalanceReport();

            Debug.Log("[TesseraV44AssetPipelineGenerator] === v4.4 All SO Assets 생성 완료 ===");
        }

        /// <summary>CombatBalanceReportGenerator의 public 진입점을 시도한다.
        /// public 진입점이 없으면 경고만 출력하고 넘어간다.</summary>
        private static void TryGenerateCombatBalanceReport()
        {
            // CombatBalanceReportGenerator에 public static 진입점이 있는지 확인한다.
            // 리플렉션을 사용해 메서드 존재 여부를 검사한다.
            System.Type reportType = typeof(CombatBalanceReportGenerator);
            System.Reflection.MethodInfo pipelineMethod = reportType.GetMethod("GenerateForPipeline",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (pipelineMethod != null)
            {
                pipelineMethod.Invoke(null, null);
                Debug.Log("[TesseraV44AssetPipelineGenerator] CombatBalanceReportGenerator.GenerateForPipeline() 호출 완료.");
                return;
            }

            System.Reflection.MethodInfo reportMethod = reportType.GetMethod("GenerateReport",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (reportMethod != null)
            {
                reportMethod.Invoke(null, null);
                Debug.Log("[TesseraV44AssetPipelineGenerator] CombatBalanceReportGenerator.GenerateReport() 호출 완료.");
                return;
            }

            System.Reflection.MethodInfo printMethod = reportType.GetMethod("PrintV44BalanceReport",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (printMethod != null)
            {
                printMethod.Invoke(null, null);
                Debug.Log("[TesseraV44AssetPipelineGenerator] CombatBalanceReportGenerator.PrintV44BalanceReport() 호출 완료.");
                return;
            }

            Debug.LogWarning("[TesseraV44AssetPipelineGenerator] CombatBalanceReportGenerator에 public static 진입점(GenerateForPipeline/GenerateReport/PrintV44BalanceReport)이 없습니다. 이번 작업에서는 호출하지 않습니다.");
        }
    }
}
