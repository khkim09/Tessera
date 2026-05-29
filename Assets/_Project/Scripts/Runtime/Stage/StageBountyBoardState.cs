using System;
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;

namespace Tessera.Runtime
{
    /// <summary>하나의 Stage 안에서 Bounty Board, Chain, Pressure, 보류 보상을 관리한다.</summary>
    public class StageBountyBoardState
    {
        private readonly List<StageBountyNodeState> bountyNodes = new List<StageBountyNodeState>();

        /// <summary>Stage 정의.</summary>
        public StageDefinitionSO StageDefinition { get; }

        /// <summary>현재 선택된 수배지.</summary>
        public StageBountyNodeState CurrentNode { get; private set; }

        /// <summary>수배지 목록.</summary>
        public IReadOnlyList<StageBountyNodeState> BountyNodes => bountyNodes;

        /// <summary>현재 Chain 누적 수.</summary>
        public int ChainCount { get; private set; }

        /// <summary>현재 Pressure 단계.</summary>
        public int PressureLevel { get; private set; }

        /// <summary>보류 중인 Parts 보상.</summary>
        public int PendingPartsReward { get; private set; }

        /// <summary>보류 중인 Overcharge 보상.</summary>
        public int PendingOverchargeReward { get; private set; }

        /// <summary>보스 클리어로 Stage가 끝났는지 여부.</summary>
        public bool IsStageCleared { get; private set; }

        /// <summary>Cash Out 이후 보스 강제 상태인지 여부.</summary>
        public bool IsBossForcedAfterCashOut { get; private set; }

        /// <summary>Stage 진행 상태를 생성한다.</summary>
        public StageBountyBoardState(StageDefinitionSO stageDefinition)
        {
            StageDefinition = stageDefinition != null ? stageDefinition : throw new ArgumentNullException(nameof(stageDefinition));
            BuildNodes(stageDefinition);
        }

        /// <summary>강제 튜토리얼 라운드를 찾는다.</summary>
        public StageBountyNodeState FindTutorialForcedNode()
        {
            for (int i = 0; i < bountyNodes.Count; i++)
            {
                if (bountyNodes[i].Definition != null && bountyNodes[i].Definition.TutorialForcedRound)
                    return bountyNodes[i];
            }

            return null;
        }

        /// <summary>첫 번째 보스 수배지를 찾는다.</summary>
        public StageBountyNodeState FindBossNode()
        {
            for (int i = 0; i < bountyNodes.Count; i++)
            {
                if (bountyNodes[i].IsBoss)
                    return bountyNodes[i];
            }

            return null;
        }

        /// <summary>라운드 선택을 시도한다.</summary>
        public bool TrySelectNode(StageBountyNodeState node)
        {
            if (node == null)
                return false;

            if (!node.IsAvailable)
                return false;

            if (node.IsCleared || node.IsDiscarded)
                return false;

            CurrentNode = node;
            return true;
        }

        /// <summary>현재 수배지를 승리 처리하고 보류 보상에 추가한다.</summary>
        public void CompleteCurrentNode()
        {
            if (CurrentNode == null)
                throw new InvalidOperationException("완료할 현재 수배지가 없습니다.");

            CurrentNode.MarkCleared();
            PendingPartsReward += CurrentNode.Definition.RewardParts;
            PendingOverchargeReward += CurrentNode.Definition.RewardOvercharge;

            if (CurrentNode.IsBoss)
                IsStageCleared = true;

            CurrentNode = null;
        }

        /// <summary>Round 패배 후 Retreat 선택을 적용한다.</summary>
        public void ApplyFailureRetreat()
        {
            if (CurrentNode != null)
                CurrentNode.MarkDiscarded();

            CurrentNode = null;
            PendingPartsReward = 0;
            PendingOverchargeReward = 0;
            ChainCount = 0;
            PressureLevel = 0;
            IsBossForcedAfterCashOut = false;

            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>Chain Rush를 선택하고 압력을 상승시킨다.</summary>
        public void ApplyChainRush()
        {
            ChainCount++;
            PressureLevel++;

            for (int i = 0; i < bountyNodes.Count; i++)
            {
                if (!bountyNodes[i].IsCleared && !bountyNodes[i].IsDiscarded)
                    bountyNodes[i].IncreaseEnrage();
            }

            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>Cash Out을 선택하고 남은 일반 수배지를 폐기한 뒤 보스를 강제한다.</summary>
        public void ApplyCashOutAndForceBoss()
        {
            IsBossForcedAfterCashOut = true;

            for (int i = 0; i < bountyNodes.Count; i++)
            {
                if (bountyNodes[i].IsNormal && !bountyNodes[i].IsCleared)
                    bountyNodes[i].MarkDiscarded();
            }

            StageBountyNodeState bossNode = FindBossNode();

            if (bossNode != null)
                bossNode.SetAvailable(true);
        }

        /// <summary>보류 중인 Parts 보상을 비운다.</summary>
        public int DrainPendingPartsReward()
        {
            int value = PendingPartsReward;
            PendingPartsReward = 0;
            return value;
        }

        /// <summary>보류 중인 Overcharge 보상을 비운다.</summary>
        public int DrainPendingOverchargeReward()
        {
            int value = PendingOverchargeReward;
            PendingOverchargeReward = 0;
            return value;
        }

        /// <summary>보스 전투 진입 가능 상태로 만든다.</summary>
        public void UnlockBoss()
        {
            StageBountyNodeState bossNode = FindBossNode();

            if (bossNode != null)
                bossNode.SetAvailable(true);
        }

        /// <summary>일반 승리 이후 선택 가능 상태를 갱신한다.</summary>
        public void RefreshAvailabilityAfterNormalDecision()
        {
            for (int i = 0; i < bountyNodes.Count; i++)
            {
                StageBountyNodeState node = bountyNodes[i];

                if (node.IsCleared || node.IsDiscarded)
                    continue;

                if (IsBossForcedAfterCashOut)
                {
                    node.SetAvailable(node.IsBoss);
                    continue;
                }

                node.SetAvailable(true);
            }
        }

        private void BuildNodes(StageDefinitionSO stageDefinition)
        {
            bountyNodes.Clear();

            StageRoundDefinitionSO[] roundDefinitions = stageDefinition.RoundDefinitions;

            if (roundDefinitions == null)
                return;

            for (int i = 0; i < roundDefinitions.Length; i++)
            {
                if (roundDefinitions[i] == null)
                    continue;

                bountyNodes.Add(new StageBountyNodeState(roundDefinitions[i]));
            }
        }
    }
}
