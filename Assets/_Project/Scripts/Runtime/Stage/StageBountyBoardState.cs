using System;
using System.Collections.Generic;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>하나의 Stage 안에서 Bounty Board, Chain, StageThreat, PendingMoneyReward를 관리한다.</summary>
    public class StageBountyBoardState
    {
        public const int EnragedStageThreatThreshold = 3;

        private readonly List<StageBountyNodeState> bountyNodes = new List<StageBountyNodeState>();

        /// <summary>Stage 정의.</summary>
        public StageDefinitionSO StageDefinition { get; }

        /// <summary>현재 선택된 수배지.</summary>
        public StageBountyNodeState CurrentNode { get; private set; }

        /// <summary>Retreat로 폐기된 최근 수배지.</summary>
        public StageBountyNodeState LastRetreatedNode { get; private set; }

        /// <summary>수배지 목록.</summary>
        public IReadOnlyList<StageBountyNodeState> BountyNodes => bountyNodes;

        /// <summary>현재 Chain 누적 수.</summary>
        public int ChainCount { get; private set; }

        /// <summary>현재 StageThreatLevel.</summary>
        public int StageThreatLevel { get; private set; }

        /// <summary>기존 Pressure 기반 코드 호환용 접근자다. 신규 코드는 StageThreatLevel을 사용한다.</summary>
        public int PressureLevel => StageThreatLevel;

        /// <summary>보류 중인 Money 보상.</summary>
        public int PendingMoneyReward { get; private set; }

        /// <summary>기존 Parts 기반 코드 호환용 접근자다. 신규 코드는 PendingMoneyReward를 사용한다.</summary>
        public int PendingPartsReward => PendingMoneyReward;

        /// <summary>기존 Pending Overcharge 코드 호환용 접근자다. 신규 구조에서는 항상 0이다.</summary>
        public int PendingOverchargeReward => 0;

        /// <summary>보스 클리어로 Stage가 끝났는지 여부.</summary>
        public bool IsStageCleared { get; private set; }

        /// <summary>CashOut 이후 보스 강제 상태인지 여부. 신규 구조에서는 항상 false다.</summary>
        public bool IsBossForcedAfterCashOut => false;

        /// <summary>Retreat Recovery 선택 제한이 활성화되었는지 여부.</summary>
        public bool IsRetreatRecoveryActive { get; private set; }

        /// <summary>Retreat Recovery 중 허용되는 최소 BountyRank.</summary>
        public int RetreatRecoveryMinimumBountyRank { get; private set; }

        /// <summary>StageThreat가 Enraged 임계값 이상인지 확인한다.</summary>
        public bool IsEnraged => StageThreatLevel >= EnragedStageThreatThreshold;

        /// <summary>마지막 클리어 수배지의 최종 보상 Money.</summary>
        public int LastCompletedRewardMoney { get; private set; }

        /// <summary>마지막 클리어 수배지의 기본 보상 Money.</summary>
        public int LastBaseRewardMoney { get; private set; }

        /// <summary>마지막 클리어 수배지의 Chain 보너스.</summary>
        public int LastChainBonusMoney { get; private set; }

        /// <summary>마지막 클리어 수배지의 BountyRank 보너스.</summary>
        public int LastBountyRankBonusMoney { get; private set; }

        /// <summary>마지막 클리어 수배지의 StageThreat 보너스.</summary>
        public int LastStageThreatBonusMoney { get; private set; }

        /// <summary>마지막 클리어 수배지의 남은 Attempt 보너스.</summary>
        public int LastRemainingAttemptBonusMoney { get; private set; }

        /// <summary>Stage 진행 상태를 생성한다.</summary>
        public StageBountyBoardState(StageDefinitionSO stageDefinition)
        {
            StageDefinition = stageDefinition != null ? stageDefinition : throw new ArgumentNullException(nameof(stageDefinition));
            ChainCount = 0;
            StageThreatLevel = 0;
            PendingMoneyReward = 0;
            IsStageCleared = false;
            IsRetreatRecoveryActive = false;
            RetreatRecoveryMinimumBountyRank = 0;
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

        /// <summary>현재 수배지를 승리 처리하고 PendingMoneyReward에 누적한다.</summary>
        public void CompleteCurrentNode(int remainingAttempts)
        {
            if (CurrentNode == null)
                throw new InvalidOperationException("완료할 현재 수배지가 없습니다.");

            LastBaseRewardMoney = CurrentNode.Definition.BaseRewardMoney;
            LastChainBonusMoney = ChainCount * (ChainCount + 1);
            LastBountyRankBonusMoney = CurrentNode.Definition.BountyRank * 2;
            LastStageThreatBonusMoney = StageThreatLevel * 2;
            LastRemainingAttemptBonusMoney = Mathf.Max(0, remainingAttempts) * 2;
            LastCompletedRewardMoney =
                LastBaseRewardMoney +
                LastChainBonusMoney +
                LastBountyRankBonusMoney +
                LastStageThreatBonusMoney +
                LastRemainingAttemptBonusMoney;

            CurrentNode.MarkCleared();
            PendingMoneyReward += Mathf.Max(0, LastCompletedRewardMoney);

            if (CurrentNode.IsBoss)
                IsStageCleared = true;

            if (IsRetreatRecoveryActive)
                ClearRetreatRecovery();

            CurrentNode = null;
            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>Round 패배 후 Retreat 선택을 적용하고 지급할 Money를 반환한다.</summary>
        public int ApplyFailureRetreat(float payoutPercent, int emergencyRetreatMoney, bool increaseStageThreat)
        {
            bool wasRetreatRecoveryActive = IsRetreatRecoveryActive;
            int payout = CalculateRetreatPayout(payoutPercent, emergencyRetreatMoney, wasRetreatRecoveryActive);

            if (CurrentNode != null)
            {
                LastRetreatedNode = CurrentNode;
                RetreatRecoveryMinimumBountyRank = CurrentNode.Definition != null ? CurrentNode.Definition.BountyRank : 1;
                CurrentNode.MarkDiscarded();
            }

            CurrentNode = null;
            PendingMoneyReward = 0;
            ChainCount = 0;
            IsRetreatRecoveryActive = true;

            if (increaseStageThreat)
                StageThreatLevel++;

            RefreshAvailabilityAfterNormalDecision();
            return payout;
        }

        /// <summary>Chain Rush를 선택하고 Chain과 StageThreat를 상승시킨다.</summary>
        public void ApplyChainRush()
        {
            ChainCount++;
            StageThreatLevel++;
            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>Cash Out을 선택하고 Chain을 초기화한 뒤 StageThreat를 상승시킨다.</summary>
        public void ApplyCashOut()
        {
            ChainCount = 0;
            StageThreatLevel++;
            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>Stage Clear 이후 Stage 내부 상태를 초기화한다.</summary>
        public void ApplyStageClear()
        {
            PendingMoneyReward = 0;
            ChainCount = 0;
            StageThreatLevel = 0;
            IsRetreatRecoveryActive = false;
            RetreatRecoveryMinimumBountyRank = 0;
            LastRetreatedNode = null;
        }

        /// <summary>보류 중인 Money 보상을 비운다.</summary>
        public int DrainPendingMoneyReward()
        {
            int value = PendingMoneyReward;
            PendingMoneyReward = 0;
            return value;
        }

        /// <summary>기존 Parts 기반 코드 호환용 메서드다. 신규 코드는 DrainPendingMoneyReward를 사용한다.</summary>
        public int DrainPendingPartsReward()
        {
            return DrainPendingMoneyReward();
        }

        /// <summary>기존 Pending Overcharge 코드 호환용 메서드다. 신규 구조에서는 항상 0을 반환한다.</summary>
        public int DrainPendingOverchargeReward()
        {
            return 0;
        }

        /// <summary>보스 전투 진입 가능 상태로 만든다.</summary>
        public void UnlockBoss()
        {
            StageBountyNodeState bossNode = FindBossNode();

            if (bossNode != null)
                bossNode.SetAvailable(true);
        }

        /// <summary>일반 승리/정산/Retreat 이후 선택 가능 상태를 갱신한다.</summary>
        public void RefreshAvailabilityAfterNormalDecision()
        {
            bool shouldUnlockBoss = HasClearedNormalBounty || !HasUnresolvedNormalBounty();

            for (int i = 0; i < bountyNodes.Count; i++)
            {
                StageBountyNodeState node = bountyNodes[i];

                if (node == null)
                    continue;

                if (node.IsCleared || node.IsDiscarded)
                    continue;

                bool lockedByRetreatRecovery = ShouldLockByRetreatRecovery(node);
                node.SetRetreatRecoveryLocked(lockedByRetreatRecovery);

                if (lockedByRetreatRecovery)
                    continue;

                if (node.IsBoss)
                {
                    node.SetAvailable(shouldUnlockBoss);
                    continue;
                }

                node.SetAvailable(true);
            }
        }

        /// <summary>현재 Stage에서 일반 수배지를 하나 이상 클리어했는지 여부.</summary>
        public bool HasClearedNormalBounty
        {
            get
            {
                for (int i = 0; i < bountyNodes.Count; i++)
                {
                    StageBountyNodeState node = bountyNodes[i];

                    if (node != null && node.IsNormal && node.IsCleared)
                        return true;
                }

                return false;
            }
        }

        /// <summary>선택 가능한 일반 수배지가 남아 있는지 여부.</summary>
        public bool HasAvailableNormalBounty
        {
            get
            {
                for (int i = 0; i < bountyNodes.Count; i++)
                {
                    StageBountyNodeState node = bountyNodes[i];

                    if (node != null && node.IsNormal && node.IsAvailable)
                        return true;
                }

                return false;
            }
        }

        /// <summary>선택 가능한 보스 수배지가 있는지 여부.</summary>
        public bool HasAvailableBossBounty
        {
            get
            {
                for (int i = 0; i < bountyNodes.Count; i++)
                {
                    StageBountyNodeState node = bountyNodes[i];

                    if (node != null && node.IsBoss && node.IsAvailable)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Retreat Recovery 잠금을 해제한다.</summary>
        private void ClearRetreatRecovery()
        {
            IsRetreatRecoveryActive = false;
            RetreatRecoveryMinimumBountyRank = 0;
            LastRetreatedNode = null;

            for (int i = 0; i < bountyNodes.Count; i++)
                bountyNodes[i].SetRetreatRecoveryLocked(false);
        }

        /// <summary>Retreat 지급 Money를 계산한다.</summary>
        private int CalculateRetreatPayout(float payoutPercent, int emergencyRetreatMoney, bool wasRetreatRecoveryActive)
        {
            if (PendingMoneyReward > 0)
                return Mathf.Max(1, Mathf.FloorToInt(PendingMoneyReward * Mathf.Clamp01(payoutPercent)));

            if (wasRetreatRecoveryActive)
                return 0;

            return Mathf.Max(0, emergencyRetreatMoney);
        }

        /// <summary>Retreat Recovery 제한으로 잠가야 하는지 확인한다.</summary>
        private bool ShouldLockByRetreatRecovery(StageBountyNodeState node)
        {
            if (!IsRetreatRecoveryActive)
                return false;

            if (node == null || node.Definition == null)
                return false;

            if (LastRetreatedNode != null && node == LastRetreatedNode)
                return true;

            return node.Definition.BountyRank < RetreatRecoveryMinimumBountyRank;
        }

        /// <summary>StageDefinition으로부터 수배지 노드를 생성한다.</summary>
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

            RefreshAvailabilityAfterNormalDecision();
        }

        /// <summary>아직 처리되지 않은 일반 수배지가 남아 있는지 확인한다.</summary>
        private bool HasUnresolvedNormalBounty()
        {
            for (int i = 0; i < bountyNodes.Count; i++)
            {
                StageBountyNodeState node = bountyNodes[i];

                if (node == null)
                    continue;

                if (node.IsNormal && !node.IsCleared && !node.IsDiscarded)
                    return true;
            }

            return false;
        }
    }
}
