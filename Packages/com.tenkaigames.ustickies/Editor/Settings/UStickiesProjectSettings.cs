using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Tenkai.UStickies
{
    /// <summary>
    /// チームで共有するカテゴリ定義と削除時のID移行情報を保持する。
    /// </summary>
    [FilePath("ProjectSettings/UStickiesCategories.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UStickiesProjectSettings : ScriptableSingleton<UStickiesProjectSettings>
    {
        public const string NoteCategoryId = "ustickies.category.note";
        public const string TodoCategoryId = "ustickies.category.todo";
        public const string BugCategoryId = "ustickies.category.bug";
        public const float DefaultCardBackgroundOpacity = 0.35f;
        public const int DefaultCardFontSize = 12;
        public const int MinimumCardFontSize = 8;
        public const int MaximumCardFontSize = 48;

        [SerializeField] private List<UStickiesCategory> _categories = new();
        [SerializeField] private List<UStickiesCategoryReplacement> _replacements = new();
        [SerializeField] private bool _useCustomCardBackgroundColor;
        [SerializeField] private Color _cardBackgroundColor = Color.white;
        [SerializeField] private float _cardBackgroundOpacity;
        [SerializeField] private bool _hasCardBackgroundOpacity;
        [SerializeField] private bool _useCustomCardFontSize;
        [SerializeField] private int _cardFontSize = DefaultCardFontSize;
        [SerializeField] private bool _hasCardFontSize;

        public static event Action changed;

        static UStickiesProjectSettings()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        public IReadOnlyList<UStickiesCategory> categories
        {
            get
            {
                EnsureDefaults();
                return _categories;
            }
        }

        public string defaultCategoryId => NoteCategoryId;
        public bool useCustomCardBackgroundColor => _useCustomCardBackgroundColor;
        public Color cardBackgroundColor => _cardBackgroundColor.a > 0f ? _cardBackgroundColor : Color.white;
        public float cardBackgroundOpacity => _hasCardBackgroundOpacity
            ? Mathf.Clamp01(_cardBackgroundOpacity)
            : DefaultCardBackgroundOpacity;
        public bool useCustomCardFontSize => _useCustomCardFontSize;
        public int cardFontSize => _hasCardFontSize
            ? Mathf.Clamp(_cardFontSize, MinimumCardFontSize, MaximumCardFontSize)
            : DefaultCardFontSize;

        public UStickiesCategory Resolve(string categoryId)
        {
            EnsureDefaults();
            var resolvedId = ResolveId(categoryId);
            return _categories.Find(category => category.id == resolvedId) ?? _categories[0];
        }

        public string ResolveId(string categoryId)
        {
            var currentId = categoryId;
            for (var index = 0; index <= _replacements.Count; index++)
            {
                var replacement = _replacements.Find(item => item.sourceId == currentId);
                if (replacement == null)
                {
                    return currentId;
                }

                currentId = replacement.replacementId;
            }

            return NoteCategoryId;
        }

        public UStickiesCategory AddCategory()
        {
            EnsureDefaults();
            Undo.RecordObject(this, "Add UStickies Category");
            var category = new UStickiesCategory(
                Guid.NewGuid().ToString("N"),
                "New Category",
                new Color(0.35f, 0.65f, 0.9f),
                false);
            _categories.Add(category);
            Commit();
            return category;
        }

        public void UpdateCategory(string categoryId, string label, Color color)
        {
            var category = _categories.Find(item => item.id == categoryId);
            if (category == null)
            {
                return;
            }

            Undo.RecordObject(this, "Edit UStickies Category");
            category.SetLabel(string.IsNullOrWhiteSpace(label) ? "Unnamed" : label.Trim());
            category.SetColor(color);
            Commit();
        }

        public bool DeleteCategory(string categoryId, string replacementId)
        {
            var category = _categories.Find(item => item.id == categoryId);
            if (category == null || category.builtIn || categoryId == replacementId)
            {
                return false;
            }

            var replacement = Resolve(replacementId);
            Undo.RecordObject(this, "Delete UStickies Category");
            _categories.Remove(category);
            _replacements.RemoveAll(item => item.sourceId == categoryId);
            _replacements.Add(new UStickiesCategoryReplacement(categoryId, replacement.id));
            Commit();
            return true;
        }

        public void MoveCategory(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _categories.Count
                || destinationIndex < 0 || destinationIndex >= _categories.Count
                || sourceIndex == destinationIndex)
            {
                return;
            }

            Undo.RecordObject(this, "Reorder UStickies Categories");
            var category = _categories[sourceIndex];
            _categories.RemoveAt(sourceIndex);
            _categories.Insert(destinationIndex, category);
            Commit();
        }

        public void SetCardBackgroundAppearance(bool useCustomColor, Color color, float opacity)
        {
            Undo.RecordObject(this, "Edit UStickies Card Appearance");
            _useCustomCardBackgroundColor = useCustomColor;
            _cardBackgroundColor = new Color(color.r, color.g, color.b, 1f);
            _cardBackgroundOpacity = Mathf.Clamp01(opacity);
            _hasCardBackgroundOpacity = true;
            EditorUtility.SetDirty(this);
            Save(true);
            changed?.Invoke();
            SceneNoteMutationService.NotifyViewChanged();
        }

        public void SetCardFontAppearance(bool useCustomFontSize, int fontSize)
        {
            Undo.RecordObject(this, "Edit UStickies Card Font");
            _useCustomCardFontSize = useCustomFontSize;
            _cardFontSize = Mathf.Clamp(fontSize, MinimumCardFontSize, MaximumCardFontSize);
            _hasCardFontSize = true;
            EditorUtility.SetDirty(this);
            Save(true);
            changed?.Invoke();
            SceneNoteMutationService.NotifyViewChanged();
        }

        private void EnsureDefaults()
        {
            if (_categories.Count > 0)
            {
                return;
            }

            _categories.Add(new UStickiesCategory(NoteCategoryId, "Note", new Color(0.65f, 0.65f, 0.65f), true));
            _categories.Add(new UStickiesCategory(TodoCategoryId, "Todo", new Color(0.95f, 0.72f, 0.18f), true));
            _categories.Add(new UStickiesCategory(BugCategoryId, "Bug", new Color(0.9f, 0.25f, 0.23f), true));
            Save(true);
        }

        private void Commit()
        {
            EditorUtility.SetDirty(this);
            Save(true);
            SceneNoteMutationService.ReconcileCategories();
            changed?.Invoke();
        }

        private static void HandleUndoRedo()
        {
            instance.Save(true);
            changed?.Invoke();
            SceneNoteMutationService.NotifyViewChanged();
        }
    }
}
