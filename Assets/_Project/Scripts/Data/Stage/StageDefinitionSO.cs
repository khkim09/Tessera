using UnityEngine;

namespace Tessera.Data
{
    /// <summary>하나의 Stage를 구성하는 수배지 목록과 Stage 메타 정보를 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "StageDefinition",
        menuName = "Tessera/Stage/Stage Definition")]
    public class StageDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int stageNumber = 1;
        [SerializeField] private string displayName = "Stage 1";
        [SerializeField] private string descentLabel = "Depth 1";
        [SerializeField] private string stageDescription;

        [Header("Stage Rules")]
        [SerializeField] private bool tutorialStage;
        [SerializeField] private bool shopEntryRequiresOverchargeAfterStageClear;
        [SerializeField] private bool keepChainAfterStageClear = true;

        [Header("Bounties")]
        [SerializeField] private StageRoundDefinitionSO[] roundDefinitions;

        /// <summary>Stage 번호.</summary>
        public int StageNumber => Mathf.Max(1, stageNumber);

        /// <summary>표시 이름.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"Stage {StageNumber}" : displayName;

        /// <summary>하강형 진행 표시.</summary>
        public string DescentLabel => descentLabel ?? string.Empty;

        /// <summary>Stage 설명.</summary>
        public string StageDescription => stageDescription ?? string.Empty;

        /// <summary>튜토리얼 Stage 여부.</summary>
        public bool TutorialStage => tutorialStage;

        /// <summary>Stage Clear Shop 입장에 Overcharge가 필요한지 여부.</summary>
        public bool ShopEntryRequiresOverchargeAfterStageClear => shopEntryRequiresOverchargeAfterStageClear;

        /// <summary>Stage Clear 후 Chain 누적 유지 여부.</summary>
        public bool KeepChainAfterStageClear => keepChainAfterStageClear;

        /// <summary>Round 정의 목록.</summary>
        public StageRoundDefinitionSO[] RoundDefinitions => roundDefinitions;

        /// <summary>정의가 최소 조건을 만족하는지 확인한다.</summary>
        public bool IsValidDefinition()
        {
            if (roundDefinitions == null || roundDefinitions.Length == 0)
                return false;

            bool hasBoss = false;

            for (int i = 0; i < roundDefinitions.Length; i++)
            {
                if (roundDefinitions[i] == null)
                    continue;

                if (roundDefinitions[i].RoundType == Core.StageRoundType.Boss)
                    hasBoss = true;
            }

            return hasBoss;
        }
    }
}
