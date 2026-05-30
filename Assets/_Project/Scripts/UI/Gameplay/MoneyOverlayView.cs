using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>항상 표시되는 Money / Chain Overlay View다.</summary>
    public class MoneyOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text chainText;
        [SerializeField] private TMP_Text moneyText;

        [Header("Display")]
        [SerializeField] private string chainPrefix = "x";
        [SerializeField] private string moneyPrefix = "$";

        private IDisposable economyChangedSubscription;

        /// <summary>Stage Economy 갱신 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            economyChangedSubscription = TesseraEventBus.Subscribe<StageEconomyChangedEvent>(HandleStageEconomyChanged);
            TesseraEventBus.Publish(new StageEconomyRefreshRequestedEvent());
        }

        /// <summary>Stage Economy 갱신 이벤트 구독을 해제한다.</summary>
        private void OnDisable()
        {
            economyChangedSubscription?.Dispose();
            economyChangedSubscription = null;
        }

        /// <summary>RunSession과 BoardState 기준으로 Overlay를 갱신한다.</summary>
        public void Refresh(TesseraRunSession runSession, StageBountyBoardState boardState)
        {
            int money = runSession != null ? runSession.Money : 0;
            int chain = boardState != null ? boardState.ChainCount : 0;

            if (chainText != null)
                chainText.text = $"{chainPrefix} {chain}";

            if (moneyText != null)
                moneyText.text = $"{moneyPrefix}{money}";
        }

        /// <summary>Stage Economy 갱신 이벤트를 처리한다.</summary>
        private void HandleStageEconomyChanged(StageEconomyChangedEvent gameEvent)
        {
            Refresh(gameEvent.RunSession, gameEvent.BoardState);
        }
    }
}
