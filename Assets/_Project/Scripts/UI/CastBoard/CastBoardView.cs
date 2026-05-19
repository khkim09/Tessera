using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Cast Board 전체 행 목록을 생성하고 갱신한다.</summary>
    public class CastBoardView : MonoBehaviour
    {
        [Header("Entry")]
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private CastBoardEntryView entryTemplate;

        private readonly List<CastBoardEntryView> _spawnedEntries = new List<CastBoardEntryView>();

        /// <summary>템플릿 행을 비활성화해 실제 표시 목록과 구분한다.</summary>
        private void Awake()
        {
            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);
        }

        /// <summary>Cast Board ViewModel을 기준으로 전체 행 UI를 다시 그린다.</summary>
        public void Refresh(CastBoardViewModel viewModel)
        {
            if (viewModel == null)
                return;

            ClearSpawnedEntries();

            for (int i = 0; i < viewModel.Entries.Count; i++)
                CreateEntry(viewModel.Entries[i]);
        }

        /// <summary>현재 생성된 Cast Board 행을 모두 제거한다.</summary>
        public void Clear()
        {
            ClearSpawnedEntries();
        }

        private void CreateEntry(CastBoardEntryModel model)
        {
            if (entryRoot == null || entryTemplate == null)
                return;

            CastBoardEntryView entry = Instantiate(entryTemplate, entryRoot);
            entry.gameObject.SetActive(true);
            entry.Bind(model);
            _spawnedEntries.Add(entry);
        }

        private void ClearSpawnedEntries()
        {
            for (int i = _spawnedEntries.Count - 1; i >= 0; i--)
            {
                if (_spawnedEntries[i] != null)
                    Destroy(_spawnedEntries[i].gameObject);
            }

            _spawnedEntries.Clear();
        }
    }
}
