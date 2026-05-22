using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D 주사위 하나의 임시 표시, 클릭 입력, 위치 이동을 관리한다.</summary>
    public class Dice3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private MeshRenderer diceRenderer;
        [SerializeField] private TMP_Text valueText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.82f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.30f, 0.48f, 0.88f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.95f, 0.92f, 0.70f, 1f);

        private Material runtimeMaterial;
        private Coroutine moveCoroutine;
        private Action<int> clickedCallback;
        private int diceIndex = -1;
        private int diceValue = 1;
        private bool isLocked;

        /// <summary>현재 원본 DiceIndex를 반환한다.</summary>
        public int DiceIndex => diceIndex;

        /// <summary>현재 주사위 값을 반환한다.</summary>
        public int DiceValue => diceValue;

        /// <summary>현재 Lock 상태를 반환한다.</summary>
        public bool IsLocked => isLocked;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트의 MeshRenderer를 기본 렌더러로 사용한다.
            diceRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>런타임 전용 Material을 준비한다.</summary>
        private void Awake()
        {
            // 주사위별 색상 변경이 독립적으로 동작하도록 Material 인스턴스를 준비한다.
            EnsureRuntimeMaterial();
        }

        /// <summary>클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> clickedCallback)
        {
            // DiceTray3DView가 전달한 Lock/Unlock 콜백을 저장한다.
            this.clickedCallback = clickedCallback;
        }

        /// <summary>주사위 표시 정보를 갱신한다.</summary>
        public void SetDice(int diceIndex, int diceValue, bool isLocked)
        {
            this.diceIndex = diceIndex;
            this.diceValue = diceValue;
            this.isLocked = isLocked;

            // 값 텍스트와 색상을 현재 Core 상태에 맞춘다.
            SetText(valueText, diceValue.ToString());
            SetDiceColor(GetBaseColor());
            gameObject.SetActive(true);
        }

        /// <summary>주사위 표시를 숨긴다.</summary>
        public void Hide()
        {
            // 아직 연결되지 않은 주사위는 비활성화한다.
            gameObject.SetActive(false);
        }

        /// <summary>주사위를 지정 위치와 회전으로 즉시 이동한다.</summary>
        public void MoveImmediate(Vector3 targetPosition, Quaternion targetRotation)
        {
            // 초기 배치나 즉시 복구가 필요할 때 사용한다.
            StopMoveCoroutine();
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        /// <summary>주사위를 지정 위치와 회전으로 부드럽게 이동한다.</summary>
        public void MoveTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            if (duration <= 0f)
            {
                MoveImmediate(targetPosition, targetRotation);
                return;
            }

            // 기존 이동 중이면 중복 코루틴을 정리한다.
            StopMoveCoroutine();
            moveCoroutine = StartCoroutine(MoveCoroutine(targetPosition, targetRotation, duration));
        }

        /// <summary>마우스 클릭 시 Dice Lock/Unlock 요청을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (diceIndex < 0)
                return;

            // Presenter의 ToggleDiceLock으로 원본 DiceIndex를 전달한다.
            clickedCallback?.Invoke(diceIndex);
        }

        /// <summary>마우스가 올라왔을 때 임시 강조한다.</summary>
        private void OnMouseEnter()
        {
            // PhysicsRaycaster가 없어도 에디터 테스트에서 시각 피드백을 준다.
            SetDiceColor(hoverColor);
        }

        /// <summary>마우스가 벗어났을 때 현재 상태 색상으로 복구한다.</summary>
        private void OnMouseExit()
        {
            // Hover 해제 시 Lock 상태 기준 색상으로 돌아간다.
            SetDiceColor(GetBaseColor());
        }

        /// <summary>부드러운 위치 이동을 처리한다.</summary>
        private IEnumerator MoveCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // Ease-out으로 슬롯에 자연스럽게 안착시킨다.
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = targetRotation;
            moveCoroutine = null;
        }

        /// <summary>진행 중인 이동 코루틴을 중지한다.</summary>
        private void StopMoveCoroutine()
        {
            if (moveCoroutine == null)
                return;

            // 새 이동 요청 전에 기존 이동을 중단한다.
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        /// <summary>현재 상태 기준 기본 색상을 반환한다.</summary>
        private Color GetBaseColor()
        {
            return isLocked ? lockedColor : normalColor;
        }

        /// <summary>주사위 색상을 변경한다.</summary>
        private void SetDiceColor(Color color)
        {
            if (diceRenderer == null)
                return;

            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
                return;

            runtimeMaterial.color = color;
        }

        /// <summary>런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeMaterial()
        {
            if (diceRenderer == null)
                return;

            if (runtimeMaterial != null)
                return;

            // sharedMaterial 대신 material을 사용해 개별 주사위 색상을 독립 처리한다.
            runtimeMaterial = diceRenderer.material;
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
