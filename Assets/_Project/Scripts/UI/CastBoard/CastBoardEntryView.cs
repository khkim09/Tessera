using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Cast Board 한 행의 표시 상태를 UI에 반영한다.</summary>
    public class CastBoardEntryView : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;

        [Header("Texts")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text rawScoreText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text useCountText;
        [SerializeField] private TMP_Text recommendedText;
        [SerializeField] private TMP_Text messageText;

        [Header("Colors")]
        [SerializeField] private Color availableColor = new Color(0.16f, 0.18f, 0.16f, 0.88f);
        [SerializeField] private Color recommendedColor = new Color(0.34f, 0.27f, 0.08f, 0.95f);
        [SerializeField] private Color usedColor = new Color(0.17f, 0.17f, 0.17f, 0.75f);
        [SerializeField] private Color blockedColor = new Color(0.28f, 0.10f, 0.10f, 0.85f);
        [SerializeField] private Color conditionNotMetColor = new Color(0.10f, 0.10f, 0.10f, 0.65f);

        /// <summary>Cast Board 행 데이터를 받아 UI 텍스트와 배경 상태를 갱신한다.</summary>
        public void Bind(CastBoardEntryModel model)
        {
            if (model == null)
                return;

            SetText(nameText, model.DisplayName);
            SetText(statusText, model.Status.ToString());
            SetText(rawScoreText, $"Raw {model.RawCastScore}");
            SetText(damageText, BuildDamageText(model));
            SetText(useCountText, BuildUseCountText(model));
            SetText(recommendedText, model.IsRecommended ? "BEST" : string.Empty);
            SetText(messageText, model.Message);

            ApplyBackgroundColor(model);
        }

        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        private static string BuildDamageText(CastBoardEntryModel model)
        {
            if (model.CastPowerBeforeTableRules != model.CastPowerAfterTableRules)
                return $"{model.CastPowerBeforeTableRules} → {model.CastPowerAfterTableRules}";

            return model.CastPowerAfterTableRules.ToString();
        }

        private static string BuildUseCountText(CastBoardEntryModel model)
        {
            if (model.PatternType == RollPatternType.BrokenCast)
                return $"{model.UseCount}/∞";

            return $"{model.UseCount}/{model.MaxUseCount}";
        }

        private void ApplyBackgroundColor(CastBoardEntryModel model)
        {
            if (backgroundImage == null)
                return;

            if (model.IsRecommended)
            {
                backgroundImage.color = recommendedColor;
                return;
            }

            if (model.Status == CastBoardEntryStatus.Available)
            {
                backgroundImage.color = availableColor;
                return;
            }

            if (model.Status == CastBoardEntryStatus.Used)
            {
                backgroundImage.color = usedColor;
                return;
            }

            if (model.Status == CastBoardEntryStatus.BlockedByTableRule)
            {
                backgroundImage.color = blockedColor;
                return;
            }

            backgroundImage.color = conditionNotMetColor;
        }
    }
}
