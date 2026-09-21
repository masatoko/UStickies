using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Tenkai.UStickies
{
    internal enum SceneNoteCompletionFilter
    {
        All,
        Undone,
        Done
    }

    internal enum SceneNoteSortMode
    {
        Newest,
        Oldest,
        Registration,
        Category
    }

    /// <summary>
    /// Sceneへ保存しない、ユーザーごとの検索・表示状態を保持する。
    /// </summary>
    [FilePath("UserSettings/UStickiesUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UStickiesUserSettings : ScriptableSingleton<UStickiesUserSettings>
    {
        [SerializeField] private string _searchText = string.Empty;
        [SerializeField] private bool _allCategories = true;
        [SerializeField] private List<string> _selectedCategoryIds = new();
        [SerializeField] private SceneNoteCompletionFilter _completionFilter;
        [SerializeField] private bool _applyFiltersToSceneView;
        [SerializeField] private SceneNoteSortMode _sortMode;
        [SerializeField] private bool _notesVisible = true;

        public static event Action changed;

        public string searchText => _searchText ?? string.Empty;
        public bool allCategories => _allCategories;
        public IReadOnlyList<string> selectedCategoryIds => _selectedCategoryIds;
        public SceneNoteCompletionFilter completionFilter => _completionFilter;
        public bool applyFiltersToSceneView => _applyFiltersToSceneView;
        public SceneNoteSortMode sortMode => _sortMode;
        public bool notesVisible => _notesVisible;

        public void SetSearchText(string value)
        {
            _searchText = value ?? string.Empty;
            Commit();
        }

        public void SelectAllCategories()
        {
            _allCategories = true;
            _selectedCategoryIds.Clear();
            Commit();
        }

        public void ClearCategories()
        {
            _allCategories = false;
            _selectedCategoryIds.Clear();
            Commit();
        }

        public void SetCategorySelected(string categoryId, bool selected)
        {
            if (_allCategories)
            {
                _selectedCategoryIds.Clear();
                foreach (var category in UStickiesProjectSettings.instance.categories)
                {
                    _selectedCategoryIds.Add(category.id);
                }

                _allCategories = false;
            }

            if (selected && !_selectedCategoryIds.Contains(categoryId))
            {
                _selectedCategoryIds.Add(categoryId);
            }
            else if (!selected)
            {
                _selectedCategoryIds.Remove(categoryId);
            }

            Commit();
        }

        public bool IsCategorySelected(string categoryId) =>
            _allCategories || _selectedCategoryIds.Contains(categoryId);

        public void SetCompletionFilter(SceneNoteCompletionFilter value)
        {
            _completionFilter = value;
            Commit();
        }

        public void SetApplyFiltersToSceneView(bool value)
        {
            _applyFiltersToSceneView = value;
            Commit();
        }

        public void SetSortMode(SceneNoteSortMode value)
        {
            _sortMode = value;
            Commit();
        }

        public void SetNotesVisible(bool value)
        {
            _notesVisible = value;
            Commit();
            SceneView.RepaintAll();
        }

        private void Commit()
        {
            Save(true);
            changed?.Invoke();
            SceneNoteMutationService.NotifyViewChanged();
        }
    }
}
