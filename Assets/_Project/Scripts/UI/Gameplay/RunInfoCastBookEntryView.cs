using System;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 창의 Cast 한 줄 UI를 표시한다.</summary>
    public class RunInfoCastBookEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text castNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text forceText;
        [SerializeField] private TMP_Text baseImpactText;
        [SerializeField] private TMP_Text remainingUseText;

        [Header("State Visuals")]
        [SerializeField] private GameObject unavailableOverlay;

        private RunInfoCastBookEntrySnapshot snapshot;

        /// <summary>Tooltip 표시 확장용 Hover 시작 이벤트다.</summary>
        public event Action<RunInfoCastBookEntrySnapshot> HoverStarted;

        /// <summary>Tooltip 표시 확장용 Hover 종료 이벤트다.</summary>
        public event Action<RunInfoCastBookEntrySnapshot> HoverEnded;

        /// <summary>Entry에 표시할 Cast 스냅샷을 반영한다.</summary>
        public void Bind(RunInfoCastBookEntrySnapshot newSnapshot)
        {
            snapshot = newSnapshot;

            if (snapshot == null)
            {
                SetText(castNameText, "-");
                SetText(scoreText, "0");
                SetText(forceText, "0");
                SetText(baseImpactText, "0");
                SetText(remainingUseText, "0");
                SetUnavailableOverlay(false);
                return;
            }

            SetText(castNameText, snapshot.CastName);
            SetText(scoreText, snapshot.Score.ToString());
            SetText(forceText, snapshot.ForceText);
            SetText(baseImpactText, snapshot.BaseImpactText);
            SetText(remainingUseText, snapshot.RemainingUseText);
            SetUnavailableOverlay(snapshot.IsUnavailable);
        }

        /// <summary>Tooltip용 Cast 타입을 반환한다.</summary>
        public RollPatternType GetPatternType()
        {
            return snapshot != null ? snapshot.PatternType : RollPatternType.None;
        }

        /// <summary>포인터 진입 시 Tooltip 확장 이벤트를 발생시킨다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (snapshot == null)
                return;

            HoverStarted?.Invoke(snapshot);
        }

        /// <summary>포인터 이탈 시 Tooltip 확장 이벤트를 발생시킨다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (snapshot == null)
                return;

            HoverEnded?.Invoke(snapshot);
        }

        /// <summary>사용 불가 Overlay 활성 상태를 적용한다.</summary>
        private void SetUnavailableOverlay(bool active)
        {
            if (unavailableOverlay == null)
                return;

            unavailableOverlay.SetActive(active);
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
