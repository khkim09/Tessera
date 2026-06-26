using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>한 Round의 주사위, Attempt, Roll, HP, Overcharge, Cast 사용 기록을 보관한다.</summary>
    public class RoundState
    {
        private readonly List<DiceInstance> _dice;
        private readonly List<CastSubmitResult> _submitResults;
        private readonly Dictionary<RollPatternType, int> _patternUseCounts;

        /// <summary>SO가 없을 때 사용할 기본 Attempt Roll 횟수다.</summary>
        public const int DefaultPlayerBaseRollsPerAttempt = 3;

        /// <summary>이 Round에 적용 중인 규칙 정보.</summary>
        public RoundRuleContext RuleContext { get; }

        /// <summary>현재 전투 HP 상태.</summary>
        public EncounterState Encounter { get; }

        /// <summary>현재 Stage 단위 Overcharge 상태.</summary>
        public OverchargeState Overcharge { get; }

        /// <summary>현재 Round의 주사위 목록.</summary>
        public IReadOnlyList<DiceInstance> Dice => _dice;

        /// <summary>현재 Attempt 상태.</summary>
        public AttemptState CurrentAttempt { get; private set; }

        /// <summary>제출된 Cast 결과 목록.</summary>
        public IReadOnlyList<CastSubmitResult> SubmitResults => _submitResults;

        /// <summary>Cast 카테고리별 사용 횟수 기록.</summary>
        public IReadOnlyDictionary<RollPatternType, int> PatternUseCounts => _patternUseCounts;

        /// <summary>현재 Attempt에서 사용한 기본 Roll 횟수.</summary>
        public int RollsUsedThisAttempt { get; private set; }

        /// <summary>Bounty 전투 중 유지되는 추가 Roll 충전량.</summary>
        public int ExtraRollCharge { get; private set; }

        /// <summary>현재 Attempt에서 사용한 추가 Roll 횟수.</summary>
        public int ExtraRollsUsedThisAttempt { get; private set; }

        /// <summary>호환용으로 현재 Attempt에서 남은 전체 Roll 횟수를 반환한다.</summary>
        public int RemainingRoundRolls => RemainingRollsThisAttempt;

        /// <summary>플레이어가 Attempt마다 기본으로 사용할 Roll 횟수다.</summary>
        public int PlayerBaseRollsPerAttempt => DefaultPlayerBaseRollsPerAttempt;

        /// <summary>현재 Attempt에서 남은 기본 Roll 횟수다.</summary>
        public int RemainingBaseRollsThisAttempt => Math.Max(0, PlayerBaseRollsPerAttempt - RollsUsedThisAttempt);

        /// <summary>현재 Attempt에서 남은 추가 Roll 횟수다.</summary>
        public int RemainingExtraRollsThisAttempt => Math.Max(0, ExtraRollCharge);

        /// <summary>현재 Attempt에서 남은 기본 및 추가 Roll 합계다.</summary>
        public int RemainingRollsThisAttempt => RemainingBaseRollsThisAttempt + RemainingExtraRollsThisAttempt;

        /// <summary>현재 Attempt에서 표시할 최대 Roll 횟수다.</summary>
        public int MaxRollsThisAttempt => PlayerBaseRollsPerAttempt + ExtraRollsUsedThisAttempt + RemainingExtraRollsThisAttempt;

        /// <summary>현재 Attempt에서 추가 Roll을 사용할 수 있는지 확인한다.</summary>
        public bool CanUseExtraRollThisAttempt => RemainingExtraRollsThisAttempt > 0;

        /// <summary>현재 Attempt에서 첫 Roll을 아직 사용하지 않았는지 확인한다.</summary>
        public bool IsFirstRollThisAttempt => RollsUsedThisAttempt <= 0;

        /// <summary>현재 상대 Intent.</summary>
        public EnemyIntent CurrentEnemyIntent { get; private set; }

        /// <summary>Round가 종료되었는지 확인한다.</summary>
        public bool IsRoundEnded { get; private set; }

        /// <summary>Round 승리 여부를 확인한다.</summary>
        public bool IsRoundWon { get; private set; }

        /// <summary>Round 패배 여부를 확인한다.</summary>
        public bool IsRoundLost { get; private set; }

        /// <summary>현재 Round 결과 타입을 반환한다.</summary>
        public RoundOutcomeType OutcomeType
        {
            get
            {
                if (IsRoundWon)
                    return RoundOutcomeType.Won;

                if (IsRoundLost)
                    return RoundOutcomeType.Lost;

                return RoundOutcomeType.Ongoing;
            }
        }

        /// <summary>Round 상태를 생성한다.</summary>
        public RoundState(
            RoundRuleContext ruleContext,
            EncounterState encounter,
            OverchargeState overcharge,
            IReadOnlyList<DiceInstance> initialDice,
            AttemptState firstAttempt,
            EnemyIntent initialEnemyIntent)
        {
            RuleContext = ruleContext ?? throw new ArgumentNullException(nameof(ruleContext));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            Overcharge = overcharge ?? throw new ArgumentNullException(nameof(overcharge));

            if (initialDice == null)
                throw new ArgumentNullException(nameof(initialDice));

            CurrentAttempt = firstAttempt ?? throw new ArgumentNullException(nameof(firstAttempt));
            CurrentEnemyIntent = initialEnemyIntent ?? EnemyIntent.None();
            RollsUsedThisAttempt = 0;
            ExtraRollCharge = 0;
            ExtraRollsUsedThisAttempt = 0;
            _dice = new List<DiceInstance>(initialDice);
            _submitResults = new List<CastSubmitResult>();
            _patternUseCounts = new Dictionary<RollPatternType, int>();
            IsRoundEnded = false;
            IsRoundWon = false;
            IsRoundLost = false;
        }

        /// <summary>현재 Attempt 번호가 최대 Attempt 수에 도달했는지 확인한다.</summary>
        public bool IsLastAttempt()
        {
            return CurrentAttempt.AttemptNumber >= RuleContext.MaxAttempts;
        }

        /// <summary>지정한 인덱스의 주사위를 반환한다.</summary>
        public DiceInstance GetDice(int index)
        {
            if (index < 0 || index >= _dice.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "주사위 인덱스가 범위를 벗어났습니다.");

            return _dice[index];
        }

        /// <summary>현재 주사위 값을 숫자 목록으로 반환한다.</summary>
        public List<int> GetCurrentDiceValues()
        {
            List<int> values = new List<int>(_dice.Count);

            for (int i = 0; i < _dice.Count; i++)
                values.Add(_dice[i].GetCurrentNumberValue());

            return values;
        }

        /// <summary>지정한 Cast 카테고리의 사용 횟수를 반환한다.</summary>
        public int GetPatternUseCount(RollPatternType patternType)
        {
            if (_patternUseCounts.TryGetValue(patternType, out int count))
                return count;

            return 0;
        }

        /// <summary>지정한 Cast 카테고리를 이번 Round에서 사용할 수 있는지 확인한다.</summary>
        public bool CanUseCastThisRound(RollPatternType patternType)
        {
            if (patternType == RollPatternType.BrokenCast)
                return true;

            int currentUseCount = GetPatternUseCount(patternType);
            int maxUses = RuleContext.MaxUsesPerCastPerRound;

            return currentUseCount < maxUses;
        }

        /// <summary>현재 Attempt가 Cast 제출 가능한 상태인지 확인한다.</summary>
        public bool CanSubmitCastNow()
        {
            if (IsRoundEnded)
                return false;

            if (CurrentAttempt == null)
                return false;

            if (CurrentAttempt.IsSubmitted)
                return false;

            return CurrentAttempt.CanSubmitCast;
        }

        /// <summary>현재 Attempt를 Cast 제출 가능 상태로 변경한다.</summary>
        public void MarkCurrentAttemptCastReady(CastReadinessSource source)
        {
            if (CurrentAttempt == null)
                throw new InvalidOperationException("현재 Attempt가 없습니다.");

            CurrentAttempt.MarkCastReady(source);
        }

        /// <summary>현재 Attempt의 기본 또는 추가 Roll 1회를 소비한다.</summary>
        public bool TrySpendAttemptRoll()
        {
            if (CurrentAttempt == null || CurrentAttempt.IsSubmitted)
                return false;

            if (RollsUsedThisAttempt < PlayerBaseRollsPerAttempt)
            {
                RollsUsedThisAttempt++;
                return true;
            }

            if (!CanUseExtraRollThisAttempt)
                return false;

            ExtraRollCharge--;
            ExtraRollsUsedThisAttempt++;
            return true;
        }

        /// <summary>아이템 등 외부 효과로 ExtraRollCharge를 추가한다.</summary>
        public void AddExtraRollCharge(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "추가 Roll 충전량은 음수가 될 수 없습니다.");

            ExtraRollCharge += amount;
        }

        /// <summary>현재 Attempt에서 추가 Roll 사용 상태를 초기화한다.</summary>
        public void ResetAttemptRollState()
        {
            RollsUsedThisAttempt = 0;
            ExtraRollsUsedThisAttempt = 0;
        }

        /// <summary>새 Attempt와 새 주사위 목록으로 진행 상태를 갱신한다.</summary>
        internal void StartAttempt(AttemptState attempt, IReadOnlyList<DiceInstance> dice)
        {
            if (attempt == null)
                throw new ArgumentNullException(nameof(attempt));

            if (dice == null)
                throw new ArgumentNullException(nameof(dice));

            CurrentAttempt = attempt;
            ResetAttemptRollState();
            _dice.Clear();

            for (int i = 0; i < dice.Count; i++)
                _dice.Add(dice[i]);
        }

        /// <summary>상대 Intent를 변경한다.</summary>
        internal void SetEnemyIntent(EnemyIntent enemyIntent)
        {
            CurrentEnemyIntent = enemyIntent ?? EnemyIntent.None();
        }

        /// <summary>현재 Enemy Intent의 Initiative를 현재 Attempt에 적용한다.</summary>
        internal void ApplyCurrentIntentInitiativeToAttempt()
        {
            if (CurrentAttempt == null)
                throw new InvalidOperationException("현재 Attempt가 없습니다.");

            EnemyIntent intent = CurrentEnemyIntent ?? EnemyIntent.None();
            CurrentAttempt.SetInitiativeOwner(intent.InitiativeOwner);
        }

        /// <summary>Cast 제출 결과를 기록한다.</summary>
        internal void AddSubmitResult(CastSubmitResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _submitResults.Add(result);
            AddPatternUse(result.PatternResult.PatternType);
        }

        /// <summary>Clash에서 사용된 Cast 카테고리 사용 횟수를 기록한다.</summary>
        internal void AddClashPatternUse(RollPatternType patternType)
        {
            AddPatternUse(patternType);
        }

        /// <summary>Round를 승리 상태로 종료한다.</summary>
        internal void MarkWon()
        {
            ClearCastUseLocks();
            IsRoundEnded = true;
            IsRoundWon = true;
            IsRoundLost = false;
        }

        /// <summary>Round를 패배 상태로 종료한다.</summary>
        internal void MarkLost()
        {
            ClearCastUseLocks();
            IsRoundEnded = true;
            IsRoundWon = false;
            IsRoundLost = true;
        }

        /// <summary>Round 종료 시 Cast 사용 제한 Lock을 모두 해제한다.</summary>
        internal void ClearCastUseLocks()
        {
            _patternUseCounts.Clear();
        }

        private void AddPatternUse(RollPatternType patternType)
        {
            if (!_patternUseCounts.ContainsKey(patternType))
                _patternUseCounts.Add(patternType, 0);

            _patternUseCounts[patternType]++;
        }
    }
}
