using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>주사위 값과 잠금 상태를 표시하고 클릭 입력을 전달한다.</summary>
    public class DiceSlotView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text stateText;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.08f, 0.08f, 0.08f, 0.55f);
        [SerializeField] private Color unlockedColor = new Color(0.88f, 0.88f, 0.82f, 0.95f);
        [SerializeField] private Color lockedColor = new Color(0.28f, 0.42f, 0.68f, 0.95f);

        private int diceIndex;
        private Action<int> clickedCallback;

        /// <summary>주사위 슬롯 클릭 콜백을 등록한다.</summary>
        public void Initialize(int index, Action<int> onClicked)
        {
            diceIndex = index;
            clickedCallback = onClicked;

            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        /// <summary>주사위 값을 일반 굴림 슬롯 형태로 표시한다.</summary>
        public void BindTrayDice(int sourceDiceIndex, int value, bool isLocked)
        {
            diceIndex = sourceDiceIndex;

            SetText(valueText, value.ToString());
            SetText(stateText, string.Empty);

            if (backgroundImage != null)
                backgroundImage.color = isLocked ? lockedColor : unlockedColor;

            if (button != null)
                button.interactable = true;
        }

        /// <summary>잠긴 주사위 슬롯을 표시한다.</summary>
        public void BindLockedDice(int sourceDiceIndex, int value)
        {
            diceIndex = sourceDiceIndex;

            SetText(valueText, value.ToString());
            SetText(stateText, string.Empty);

            if (backgroundImage != null)
                backgroundImage.color = lockedColor;

            if (button != null)
                button.interactable = true;
        }

        /// <summary>빈 잠금 슬롯으로 표시한다.</summary>
        public void BindEmpty()
        {
            SetText(valueText, string.Empty);
            SetText(stateText, string.Empty);

            if (backgroundImage != null)
                backgroundImage.color = emptyColor;

            if (button != null)
                button.interactable = false;
        }

        /// <summary>오브젝트 제거 시 버튼 이벤트를 해제한다.</summary>
        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>슬롯 클릭 시 현재 매핑된 주사위 index를 전달한다.</summary>
        private void HandleClicked()
        {
            clickedCallback?.Invoke(diceIndex);
        }

        /// <summary>TMP 텍스트 값을 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null) return;

            targetText.text = value;
        }
    }
}
