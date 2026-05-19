using System;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>현재 주사위로 제출 가능한 Cast 후보 한 줄을 표시한다.</summary>
    public class CastCandidateEntryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text castNameText;
        [SerializeField] private TMP_Text damageText;

        [Header("Normal Colors")]
        [SerializeField] private Color normalColor = new Color(0.08f, 0.10f, 0.10f, 0.82f);
        [SerializeField] private Color selectedColor = new Color(0.22f, 0.28f, 0.36f, 0.92f);

        [Header("Kill Highlight")]
        [SerializeField] private Color killBaseColor = new Color(1f, 0.08f, 0.04f, 0.32f);
        [SerializeField] private Color killPeakColor = new Color(1f, 0.04f, 0.02f, 0.68f);
        [SerializeField] private float killBreathSpeed = 2.25f;

        private RollPatternType patternType;
        private bool isKillCandidate;
        private bool isSelected;
        private Action<RollPatternType> clickedCallback;

        /// <summary>Cast 후보 행을 초기화하고 클릭 콜백을 등록한다.</summary>
        public void Initialize(Action<RollPatternType> onClicked)
        {
            clickedCallback = onClicked;

            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        /// <summary>Cast 후보 정보를 UI에 반영한다.</summary>
        public void Bind(
            RollPatternType newPatternType,
            string displayName,
            int visibleDamage,
            bool newIsKillCandidate,
            bool newIsSelected)
        {
            patternType = newPatternType;
            isKillCandidate = newIsKillCandidate;
            isSelected = newIsSelected;

            SetText(castNameText, displayName);
            SetText(damageText, visibleDamage.ToString());
            ApplyStaticColor();
        }

        private void Update()
        {
            if (!isKillCandidate || backgroundImage == null)
                return;

            float t = (Mathf.Sin(Time.unscaledTime * killBreathSpeed) + 1f) * 0.5f;
            backgroundImage.color = Color.Lerp(killBaseColor, killPeakColor, t);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        private void HandleClicked()
        {
            clickedCallback?.Invoke(patternType);
        }

        private void ApplyStaticColor()
        {
            if (backgroundImage == null) return;

            if (isKillCandidate)
            {
                backgroundImage.color = killBaseColor;
                return;
            }

            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }

        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null) return;

            targetText.text = value;
        }
    }
}
