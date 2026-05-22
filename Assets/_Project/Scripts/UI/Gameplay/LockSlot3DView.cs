using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D LockSlot 하나의 표시 상태와 클릭 입력을 관리한다.</summary>
    public class LockSlot3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private MeshRenderer slotRenderer;
        [SerializeField] private TMP_Text diceValueText;
        [SerializeField] private TMP_Text slotIndexText;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.42f, 0.46f, 0.50f, 1f);
        [SerializeField] private Color occupiedColor = new Color(0.72f, 0.74f, 0.76f, 1f);
        [SerializeField] private Color highlightedColor = new Color(0.95f, 0.78f, 0.22f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.92f, 0.90f, 0.72f, 1f);

        private Material runtimeMaterial;
        private Action<int> clickedCallback;
        private int slotIndex = -1;
        private int diceIndex = -1;
        private int diceValue = 0;
        private bool isHovering;

        /// <summary>현재 LockSlot 인덱스를 반환한다.</summary>
        public int SlotIndex => slotIndex;

        /// <summary>현재 배치된 원본 DiceIndex를 반환한다.</summary>
        public int DiceIndex => diceIndex;

        /// <summary>현재 배치된 주사위 눈금을 반환한다.</summary>
        public int DiceValue => diceValue;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트의 MeshRenderer를 기본 슬롯 렌더러로 사용한다.
            slotRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>런타임 Material을 준비한다.</summary>
        private void Awake()
        {
            // 슬롯별 색상 변경이 독립적으로 동작하도록 Material 인스턴스를 준비한다.
            EnsureRuntimeMaterial();
        }

        /// <summary>슬롯 인덱스와 클릭 콜백을 초기화한다.</summary>
        public void Initialize(int slotIndex, Action<int> clickedCallback)
        {
            this.slotIndex = slotIndex;
            this.clickedCallback = clickedCallback;

            // 디버그 단계에서는 슬롯 번호를 1부터 표시한다.
            SetText(slotIndexText, (slotIndex + 1).ToString());
        }

        /// <summary>빈 LockSlot 상태로 표시한다.</summary>
        public void SetEmpty()
        {
            diceIndex = -1;
            diceValue = 0;

            // 비어 있는 슬롯은 기본 회색으로 표시한다.
            SetSlotColor(emptyColor);
            SetText(diceValueText, string.Empty);
        }

        /// <summary>Lock된 주사위가 올라간 상태로 표시한다.</summary>
        public void SetLockedDice(int diceIndex, int diceValue)
        {
            this.diceIndex = diceIndex;
            this.diceValue = diceValue;

            // 배치된 슬롯은 밝은 색과 주사위 값을 표시한다.
            SetSlotColor(occupiedColor);
            SetText(diceValueText, diceValue.ToString());
        }

        /// <summary>계산 중인 LockSlot 강조 여부를 표시한다.</summary>
        public void SetHighlighted(bool isHighlighted)
        {
            // SlotPair 계산 연출 때 현재 슬롯만 강조한다.
            SetSlotColor(isHighlighted ? highlightedColor : GetBaseColor());
        }

        /// <summary>마우스 클릭 시 해당 LockSlot의 Dice Unlock 요청을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (slotIndex < 0)
                return;

            if (diceIndex < 0)
                return;

            // Presenter에 LockSlot index를 전달해 해당 슬롯의 Dice를 Unlock한다.
            clickedCallback?.Invoke(slotIndex);
        }

        /// <summary>마우스가 올라왔을 때 임시 강조한다.</summary>
        private void OnMouseEnter()
        {
            if (diceIndex < 0)
                return;

            // 비어 있지 않은 슬롯만 hover 피드백을 준다.
            isHovering = true;
            SetSlotColor(hoverColor);
        }

        /// <summary>마우스가 벗어났을 때 현재 상태 색상으로 복구한다.</summary>
        private void OnMouseExit()
        {
            // Hover 해제 시 Lock 상태 기준 색상으로 돌아간다.
            isHovering = false;
            SetSlotColor(GetBaseColor());
        }

        /// <summary>현재 상태 기준 기본 색상을 반환한다.</summary>
        private Color GetBaseColor()
        {
            return diceIndex < 0 ? emptyColor : occupiedColor;
        }

        /// <summary>슬롯 렌더러 색상을 변경한다.</summary>
        private void SetSlotColor(Color color)
        {
            if (slotRenderer == null)
                return;

            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
                return;

            runtimeMaterial.color = color;
        }

        /// <summary>런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeMaterial()
        {
            if (slotRenderer == null)
                return;

            if (runtimeMaterial != null)
                return;

            // sharedMaterial이 아니라 material을 사용해 슬롯별 색상을 독립 처리한다.
            runtimeMaterial = slotRenderer.material;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}
