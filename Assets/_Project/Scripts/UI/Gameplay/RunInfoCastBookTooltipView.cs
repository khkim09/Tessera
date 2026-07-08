using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 Entry Hover 시 표시되는 Cast 설명 Tooltip View다.</summary>
    public class RunInfoCastBookTooltipView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Text Container")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private bool useEnglishDescription;

        [Header("Preview Container")]
        [SerializeField] private GameObject previewRoot;
        [SerializeField] private RectTransform[] previewDiceRoots = new RectTransform[5];
        [SerializeField] private Image[] previewDiceImages = new Image[5];
        [SerializeField] private Sprite[] diceFaceSprites = new Sprite[7];
        [SerializeField] private float normalDiceScale = 0.86f;
        [SerializeField] private float emphasizedDiceScale = 1.12f;

        /// <summary>Tooltip의 RectTransform을 반환한다.</summary>
        public RectTransform RectTransform => transform as RectTransform;

        /// <summary>컴포넌트 추가 시 자식 UI 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            AssignReferencesIfMissing();
        }

        /// <summary>초기화 시 참조를 보정하고 Pointer 차단을 해제한다.</summary>
        private void Awake()
        {
            AssignReferencesIfMissing();
            DisableRaycastTargets();
            Hide();
        }

        /// <summary>지정 Cast 스냅샷과 설명 데이터로 Tooltip을 표시한다.</summary>
        public void Show(RunInfoCastBookEntrySnapshot snapshot, RunInfoCastBookTooltipContent content)
        {
            AssignReferencesIfMissing();
            DisableRaycastTargets();

            if (snapshot == null)
            {
                Hide();
                return;
            }

            RunInfoCastBookTooltipContent safeContent =
                content ?? RunInfoCastBookTooltipCatalog.Resolve(snapshot.PatternType);

            SetText(descriptionText, ResolveDescription(safeContent));
            RefreshPreview(safeContent);
            SetVisible(true);
        }

        /// <summary>Tooltip을 숨긴다.</summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>Tooltip 표시 상태를 변경한다.</summary>
        private void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>현재 언어 설정에 맞는 설명 텍스트를 반환한다.</summary>
        private string ResolveDescription(RunInfoCastBookTooltipContent content)
        {
            if (content == null)
                return string.Empty;

            return useEnglishDescription ? content.DescriptionEn : content.DescriptionKr;
        }

        /// <summary>PreviewContainer에 주사위 미리보기 값을 반영한다.</summary>
        private void RefreshPreview(RunInfoCastBookTooltipContent content)
        {
            if (previewRoot != null)
                previewRoot.SetActive(content != null);

            if (content == null)
                return;

            IReadOnlyList<int> diceValues = content.PreviewDiceValues;
            IReadOnlyList<int> emphasizedIndexes = content.EmphasizedIndexes;

            int slotCount = previewDiceImages != null ? previewDiceImages.Length : 0;

            for (int i = 0; i < slotCount; i++)
            {
                int diceValue = diceValues != null && i < diceValues.Count ? diceValues[i] : 0;
                bool isEmphasized = ContainsIndex(emphasizedIndexes, i);

                SetPreviewDice(i, diceValue, isEmphasized);
            }
        }

        /// <summary>Preview 슬롯 하나의 눈금과 강조 상태를 반영한다.</summary>
        private void SetPreviewDice(int index, int diceValue, bool isEmphasized)
        {
            Image diceImage = ResolvePreviewDiceImage(index);

            if (diceImage != null)
            {
                diceImage.sprite = ResolveDiceFaceSprite(diceValue);
                diceImage.enabled = true;
            }

            RectTransform diceRoot = ResolvePreviewDiceRoot(index);

            if (diceRoot != null)
            {
                float scale = isEmphasized ? emphasizedDiceScale : normalDiceScale;
                diceRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            }
        }

        /// <summary>지정 주사위 값에 해당하는 Sprite를 반환한다.</summary>
        private Sprite ResolveDiceFaceSprite(int diceValue)
        {
            if (diceFaceSprites == null)
                return null;

            if (diceValue < 0 || diceValue >= diceFaceSprites.Length)
                return null;

            return diceFaceSprites[diceValue];
        }

        /// <summary>Preview 슬롯의 Image를 반환한다.</summary>
        private Image ResolvePreviewDiceImage(int index)
        {
            if (previewDiceImages == null)
                return null;

            if (index < 0 || index >= previewDiceImages.Length)
                return null;

            return previewDiceImages[index];
        }

        /// <summary>Preview 슬롯의 Scale 적용 RectTransform을 반환한다.</summary>
        private RectTransform ResolvePreviewDiceRoot(int index)
        {
            if (previewDiceRoots != null &&
                index >= 0 &&
                index < previewDiceRoots.Length &&
                previewDiceRoots[index] != null)
            {
                return previewDiceRoots[index];
            }

            Image diceImage = ResolvePreviewDiceImage(index);

            if (diceImage != null)
                return diceImage.transform as RectTransform;

            return null;
        }

        /// <summary>인덱스 목록에 지정 인덱스가 포함되는지 확인한다.</summary>
        private static bool ContainsIndex(IReadOnlyList<int> indexes, int targetIndex)
        {
            if (indexes == null)
                return false;

            for (int i = 0; i < indexes.Count; i++)
            {
                if (indexes[i] == targetIndex)
                    return true;
            }

            return false;
        }

        /// <summary>누락된 자식 UI 참조를 이름 기준으로 보정한다.</summary>
        private void AssignReferencesIfMissing()
        {
            if (root == null)
                root = gameObject;

            if (descriptionText == null)
                descriptionText = FindTextByName("DescriptionText");
        }

        /// <summary>지정 이름의 TMP_Text 자식 참조를 찾는다.</summary>
        private TMP_Text FindTextByName(string targetName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                    continue;

                if (texts[i].name == targetName)
                    return texts[i];
            }

            return null;
        }

        /// <summary>Tooltip 내부 Graphic이 Entry Hover를 가로막지 않도록 RaycastTarget을 끈다.</summary>
        private void DisableRaycastTargets()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;

                graphics[i].raycastTarget = false;
            }
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value ?? string.Empty;
        }
    }
}
