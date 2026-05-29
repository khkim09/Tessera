using System;
using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>
    /// GameMode 변경에 따라 Battle 입력 가능 여부를 제어한다.
    /// Gameplay가 아닌 화면에서는 DiceCup, Cast, Popup 입력을 잠근다.
    /// </summary>
    public class BattleGameplayInputLockController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TesseraGameplayBattlePresenter gameplayPresenter;

        private IDisposable gameModeChangedSubscription;
        private IDisposable gameModeChangeRequestedSubscription;

        /// <summary>GameMode 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            gameModeChangedSubscription = TesseraEventBus.Subscribe<GameModeChangedEvent>(HandleGameModeChanged);
            gameModeChangeRequestedSubscription = TesseraEventBus.Subscribe<GameModeChangeRequestedEvent>(HandleGameModeChangeRequested);
        }

        /// <summary>GameMode 이벤트 구독을 해제한다.</summary>
        private void OnDisable()
        {
            gameModeChangedSubscription?.Dispose();
            gameModeChangeRequestedSubscription?.Dispose();

            gameModeChangedSubscription = null;
            gameModeChangeRequestedSubscription = null;
        }

        /// <summary>GameMode 변경 요청을 입력 잠금 상태에 선반영한다.</summary>
        private void HandleGameModeChangeRequested(GameModeChangeRequestedEvent gameEvent)
        {
            ApplyMode(gameEvent.RequestedMode);
        }

        /// <summary>GameMode 변경 완료를 입력 잠금 상태에 반영한다.</summary>
        private void HandleGameModeChanged(GameModeChangedEvent gameEvent)
        {
            ApplyMode(gameEvent.CurrentMode);
        }

        /// <summary>현재 GameMode에 맞게 Battle 입력 잠금 상태를 적용한다.</summary>
        private void ApplyMode(GameModeType mode)
        {
            bool shouldUnlock = mode == GameModeType.Gameplay;

            if (shouldUnlock)
            {
                SetGameplayInputLocked(false, string.Empty);
                return;
            }

            SetGameplayInputLocked(true, BuildLockReason(mode));
        }

        /// <summary>Gameplay 입력 잠금 상태를 Presenter에 전달한다.</summary>
        private void SetGameplayInputLocked(bool isLocked, string reason)
        {
            if (gameplayPresenter == null)
                return;

            gameplayPresenter.SetExternalGameplayInputLocked(isLocked, reason);
        }

        /// <summary>현재 GameMode에 맞는 입력 잠금 사유 문구를 생성한다.</summary>
        private static string BuildLockReason(GameModeType mode)
        {
            if (mode == GameModeType.BountyBoard)
                return "Choose a bounty.";

            if (mode == GameModeType.RewardDecision)
                return "Choose Cash Out, Chain Rush, or Boss.";

            if (mode == GameModeType.Shop)
                return "Workshop is open.";

            if (mode == GameModeType.Result)
                return "Run result is open.";

            if (mode == GameModeType.RoundSelect)
                return "Round selection is open.";

            return "Gameplay input is locked.";
        }
    }
}
