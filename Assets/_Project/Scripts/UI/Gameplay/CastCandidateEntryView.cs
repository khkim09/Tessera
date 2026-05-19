using System;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>현재 주사위로 가능한 Cast 후보 한 줄을 표시하고 선택 입력을 처리한다.</summary>
    public class CastCandidateEntryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text castNameText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.08f, 0.10f, 0.10f, 0.82f);
        [SerializeField] private Color selectedColor = new Color(0.22f, 0.34f, 0.55f, 0.95f);
        [SerializeField] private Color recommendedColor = new Color(0.18f, 0.24f, 0.32f, 0.92f);

        private RollPatternType patternType;
        private Action<RollPatternType> clickedCallback;

        /// <summary>Cast 후보 행의 클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<RollPatternType> onClicked)
        {
            clickedCallback = onClicked;

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
            button.enabled = true;
            button.interactable = true;
        }

        /// <summary>Cast 후보 정보를 UI에 반영한다.</summary>
        public void Bind(
            RollPatternType newPatternType,
            string displayName,
            int castScore,
            bool isSelected,
            bool isRecommended)
        {
            patternType = newPatternType;

            SetText(castNameText, displayName);
            SetText(scoreText, castScore.ToString());

            ApplyColor(isSelected, isRecommended);
        }

        /// <summary>오브젝트 제거 시 버튼 이벤트를 해제한다.</summary>
        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>후보 행 클릭 시 해당 Cast 타입을 전달한다.</summary>
        private void HandleClicked()
        {
            clickedCallback?.Invoke(patternType);
        }

        /// <summary>선택/추천 상태에 맞는 배경색을 적용한다.</summary>
        private void ApplyColor(bool isSelected, bool isRecommended)
        {
            if (backgroundImage == null)
                return;

            if (isSelected)
            {
                backgroundImage.color = selectedColor;
                return;
            }

            backgroundImage.color = isRecommended ? recommendedColor : normalColor;
        }

        /// <summary>TMP 텍스트 값을 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}
