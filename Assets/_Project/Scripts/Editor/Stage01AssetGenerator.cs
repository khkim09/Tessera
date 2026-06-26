using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>
    /// Stage 1 (Broken Cast) 테스트용 모든 에셋을 한 번에 생성/업데이트하는 Editor 유틸리티.
    /// Tools/Tessera/Generate Stage 1 All Assets 메뉴에서 실행한다.
    /// 내부적으로 Round/Device, EnemyIntent, EnemyDiceLoadout 각 전용 생성기를 순차 호출한다.
    /// </summary>
    public static class Stage01AssetGenerator
    {
        /// <summary>Tools/Tessera/Generate Stage 1 All Assets 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Generate Stage 1 All Assets")]
        private static void GenerateAll()
        {
            Debug.Log("[Stage01AssetGenerator] Starting Stage 1 asset generation...");

            // 1. Round 및 Device 에셋 생성
            Debug.Log("[Stage01AssetGenerator] Phase 1: Round and Device assets...");
            Stage01RoundAndDeviceAssetGenerator.InvokeGenerate();

            // 2. Enemy Intent 에셋 및 Profile 생성
            Debug.Log("[Stage01AssetGenerator] Phase 2: Enemy Intent and Profile assets...");
            Stage01EnemyIntentAssetGenerator.InvokeGenerate();

            // 3. Enemy Dice Loadout 에셋 생성
            Debug.Log("[Stage01AssetGenerator] Phase 3: Enemy Dice Loadout assets...");
            Stage01EnemyDiceLoadoutAssetGenerator.InvokeGenerate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Stage01AssetGenerator] Stage 1 asset generation complete.");
        }
    }
}
