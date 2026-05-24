using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 World Space Hologram UI에 핵심 전투 판단 정보만 표시한다.</summary>
    public class BattleTableHologramView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool hideWhenNoCastPreview = false;

        [Header("Cast Preview Texts")]
        [SerializeField] private TMP_Text castText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text forceText;

        [Header("Battle Resource Texts")]
        [SerializeField] private TMP_Text attemptText;
        [SerializeField] private TMP_Text rollText;
        [SerializeField] private TMP_Text overchargeText;

        [Header("Display Options")]
        [SerializeField] private bool showAttemptMaxValue = false;
        [SerializeField] private bool showRollMaxValue = false;

        /// <summary>홀로그램 표시 여부를 변경한다.</summary>
        public void SetVisible(bool isVisible)
        {
            // CanvasGroup이 있으면 오브젝트 비활성화 없이 표시 상태만 제어한다.
            if (canvasGroup == null)
            {
                gameObject.SetActive(isVisible);
                return;
            }

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>Attempt, Roll, Overcharge 정보를 갱신한다.</summary>
        public void RefreshBattleMeta(
            int attemptNumber,
            int maxAttempts,
            int remainingRolls,
            int rollPool,
            int overcharge)
        {
            // 테이블 홀로그램에 플레이 판단용 제한 자원만 표시한다.
            string attemptValue = showAttemptMaxValue ? $"{attemptNumber}/{maxAttempts}" : attemptNumber.ToString();
            string rollValue = showRollMaxValue ? $"{remainingRolls}/{rollPool}" : remainingRolls.ToString();

            SetText(attemptText, attemptValue);
            SetText(rollText, rollValue);
            SetText(overchargeText, overcharge.ToString());
        }

        /// <summary>선택된 Cast의 Score / Force 미리보기를 갱신한다.</summary>
        public void RefreshCastPreview(string castName, int score, string forceValue)
        {
            // 선택된 Cast가 있으면 핵심 계산값을 크게 표시한다.
            SetVisible(true);
            SetText(castText, castName);
            SetText(scoreText, score.ToString());
            SetText(forceText, forceValue);
        }

        /// <summary>Cast 미리보기 영역을 기본 상태로 되돌린다.</summary>
        public void ClearCastPreview()
        {
            // 선택 Cast가 없을 때도 레이아웃이 무너지지 않도록 기본값을 유지한다.
            SetText(castText, "CastName");
            SetText(scoreText, "0");
            SetText(forceText, "0");

            if (hideWhenNoCastPreview)
                SetVisible(false);
        }

        /// <summary>SlotPair 계산 중 현재 단계의 Score / Force 값을 표시한다.</summary>
        public void RefreshSlotPairStep(
            int slotIndex,
            int totalSlotCount,
            string castName,
            int scoreAfter,
            string forceAfter)
        {
            // 계산 중에는 Cast 영역에 현재 SlotPair 진행 상태를 짧게 표시한다.
            SetVisible(true);
            SetText(castText, $"Slot {slotIndex + 1}/{totalSlotCount}");
            SetText(scoreText, scoreAfter.ToString());
            SetText(forceText, forceAfter);
        }

        /// <summary>텍스트가 연결되어 있을 때만 값을 대입한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            // 부분 구성된 홀로그램도 허용하기 위해 null 텍스트는 무시한다.
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}
