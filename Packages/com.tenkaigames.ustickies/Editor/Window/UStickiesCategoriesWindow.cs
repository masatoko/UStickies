using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    /// <summary>
    /// プロジェクト共通カテゴリを編集するWindow。
    /// </summary>
    internal sealed class UStickiesCategoriesWindow : EditorWindow
    {
        private List<UStickiesCategory> _viewCategories;
        private ListView _list;

        [MenuItem("Window/UStickies/Categories")]
        public static void Open() =>
            GetWindow<UStickiesCategoriesWindow>("UStickies Categories");

        private void OnEnable()
        {
            UStickiesProjectSettings.changed += Refresh;
            Undo.undoRedoPerformed += Refresh;
        }

        private void OnDisable()
        {
            UStickiesProjectSettings.changed -= Refresh;
            Undo.undoRedoPerformed -= Refresh;
        }

        public void CreateGUI()
        {
            UStickiesTheme.Apply(rootVisualElement);
            rootVisualElement.AddToClassList("ustickies-root");

            var toolbar = new Toolbar();
            toolbar.AddToClassList("ustickies-toolbar");
            var title = new Label("Project Categories");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(title);
            var spacer = new VisualElement();
            spacer.AddToClassList("ustickies-spacer");
            toolbar.Add(spacer);
            var add = new ToolbarButton(() =>
            {
                UStickiesProjectSettings.instance.AddCategory();
                Refresh();
            }) { text = "Add Category" };
            add.AddToClassList("ustickies-primary");
            toolbar.Add(add);
            rootVisualElement.Add(toolbar);

            _list = new ListView
            {
                fixedItemHeight = 36f,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                selectionType = SelectionType.None,
                makeItem = MakeCategoryRow,
                bindItem = BindCategoryRow
            };
            _list.style.flexGrow = 1f;
            _list.itemIndexChanged += (source, destination) =>
            {
                UStickiesProjectSettings.instance.MoveCategory(source, destination);
                Refresh();
            };
            rootVisualElement.Add(_list);
            Refresh();
        }

        private VisualElement MakeCategoryRow()
        {
            var row = new VisualElement();
            row.AddToClassList("ustickies-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var color = new ColorField { name = "color", showAlpha = false };
            color.style.width = 55f;
            color.RegisterValueChangedCallback(evt =>
            {
                if (row.userData is UStickiesCategory category)
                {
                    UStickiesProjectSettings.instance.UpdateCategory(category.id, category.label, evt.newValue);
                }
            });
            row.Add(color);

            var label = new TextField { name = "label", isDelayed = true };
            label.style.flexGrow = 1f;
            label.RegisterValueChangedCallback(evt =>
            {
                if (row.userData is UStickiesCategory category)
                {
                    UStickiesProjectSettings.instance.UpdateCategory(category.id, evt.newValue, category.color);
                }
            });
            row.Add(label);

            var delete = new Button { name = "delete", text = "Delete" };
            delete.AddToClassList("ustickies-danger");
            delete.clicked += () =>
            {
                if (row.userData is UStickiesCategory category)
                {
                    RequestDelete(category);
                }
            };
            row.Add(delete);
            return row;
        }

        private void BindCategoryRow(VisualElement row, int index)
        {
            var category = _viewCategories[index];
            row.userData = category;
            row.Q<ColorField>("color").SetValueWithoutNotify(category.color);
            row.Q<TextField>("label").SetValueWithoutNotify(category.label);
            row.Q<Button>("delete").SetEnabled(!category.builtIn);
        }

        private void RequestDelete(UStickiesCategory category)
        {
            if (category.builtIn)
            {
                return;
            }

            if (CountOpenReferences(category.id) == 0)
            {
                UStickiesProjectSettings.instance.DeleteCategory(
                    category.id,
                    UStickiesProjectSettings.NoteCategoryId);
                return;
            }

            var menu = new GenericMenu();
            foreach (var replacement in UStickiesProjectSettings.instance.categories)
            {
                if (replacement.id == category.id)
                {
                    continue;
                }

                var capturedId = replacement.id;
                menu.AddItem(new GUIContent($"Replace with/{replacement.label}"), false, () =>
                    UStickiesProjectSettings.instance.DeleteCategory(category.id, capturedId));
            }

            menu.ShowAsContext();
        }

        private static int CountOpenReferences(string categoryId)
        {
            var count = 0;
            foreach (var entry in SceneNoteRepository.GetOpenDatabases())
            {
                foreach (var note in entry.database.notes)
                {
                    if (UStickiesProjectSettings.instance.ResolveId(note.categoryId) == categoryId)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void Refresh()
        {
            if (_list == null)
            {
                return;
            }

            _viewCategories = new List<UStickiesCategory>(UStickiesProjectSettings.instance.categories);
            _list.itemsSource = _viewCategories;
            _list.Rebuild();
        }
    }
}
