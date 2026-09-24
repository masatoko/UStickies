using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    /// <summary>
    /// Project Settingsに表示する設定UIを構築する。
    /// </summary>
    internal sealed class UStickiesSettingsView : VisualElement
    {
        private List<UStickiesCategory> _viewCategories;
        private readonly FloatField _maximumWidthField;
        private readonly FloatField _maximumHeightField;
        private readonly Toggle _useCustomTextColorToggle;
        private readonly ColorField _textColorField;
        private readonly ListView _categoryList;

        public UStickiesSettingsView()
        {
            UStickiesTheme.Apply(this);
            AddToClassList("ustickies-root");
            AddToClassList("ustickies-settings");

            var appearancePanel = new VisualElement();
            appearancePanel.AddToClassList("ustickies-panel");
            appearancePanel.Add(CreateSectionTitle("Note Card Appearance"));

            var settings = UStickiesUserSettings.instance;
            _maximumWidthField = new FloatField("Maximum Width")
            {
                isDelayed = true,
                value = settings.cardMaximumWidth
            };
            _maximumWidthField.RegisterValueChangedCallback(_ => SaveCardAppearance());
            appearancePanel.Add(_maximumWidthField);

            _maximumHeightField = new FloatField("Maximum Height")
            {
                isDelayed = true,
                value = settings.cardMaximumHeight
            };
            _maximumHeightField.RegisterValueChangedCallback(_ => SaveCardAppearance());
            appearancePanel.Add(_maximumHeightField);

            _useCustomTextColorToggle = new Toggle("Use Custom Text Color")
            {
                value = settings.useCustomCardTextColor
            };
            _useCustomTextColorToggle.RegisterValueChangedCallback(evt =>
            {
                _textColorField.SetEnabled(evt.newValue);
                SaveCardAppearance();
            });
            appearancePanel.Add(_useCustomTextColorToggle);

            _textColorField = new ColorField("Text Color")
            {
                showAlpha = true,
                value = settings.cardTextColor
            };
            _textColorField.SetEnabled(settings.useCustomCardTextColor);
            _textColorField.RegisterValueChangedCallback(_ => SaveCardAppearance());
            appearancePanel.Add(_textColorField);

            var limits = new HelpBox(
                $"Width: {UStickiesUserSettings.MinimumCardWidth:0}-{UStickiesUserSettings.MaximumCardWidthLimit:0} px, "
                + $"Height: {UStickiesUserSettings.MinimumCardHeight:0}-{UStickiesUserSettings.MaximumCardHeightLimit:0} px. "
                + "These values are stored per user.",
                HelpBoxMessageType.Info);
            appearancePanel.Add(limits);
            Add(appearancePanel);

            var categoryPanel = new VisualElement();
            categoryPanel.AddToClassList("ustickies-panel");
            categoryPanel.AddToClassList("ustickies-settings-categories");

            var categoryHeader = new VisualElement();
            categoryHeader.AddToClassList("ustickies-settings-header");
            categoryHeader.Add(CreateSectionTitle("Categories"));
            var spacer = new VisualElement();
            spacer.AddToClassList("ustickies-spacer");
            categoryHeader.Add(spacer);
            var addCategoryButton = new Button(() =>
            {
                UStickiesProjectSettings.instance.AddCategory();
                RefreshCategories();
            }) { text = "Add Category" };
            addCategoryButton.AddToClassList("ustickies-primary");
            categoryHeader.Add(addCategoryButton);
            categoryPanel.Add(categoryHeader);

            var categoryDescription = new Label("Categories are shared through ProjectSettings.");
            categoryDescription.AddToClassList("ustickies-muted");
            categoryPanel.Add(categoryDescription);

            _categoryList = new ListView
            {
                fixedItemHeight = 36f,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                selectionType = SelectionType.None,
                makeItem = MakeCategoryRow,
                bindItem = BindCategoryRow
            };
            _categoryList.AddToClassList("ustickies-settings-category-list");
            _categoryList.itemIndexChanged += (source, destination) =>
            {
                UStickiesProjectSettings.instance.MoveCategory(source, destination);
                RefreshCategories();
            };
            categoryPanel.Add(_categoryList);
            Add(categoryPanel);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UStickiesProjectSettings.changed += RefreshCategories;
                Undo.undoRedoPerformed += RefreshCategories;
                RefreshCategories();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                UStickiesProjectSettings.changed -= RefreshCategories;
                Undo.undoRedoPerformed -= RefreshCategories;
            });
        }

        private static Label CreateSectionTitle(string text)
        {
            var title = new Label(text);
            title.AddToClassList("ustickies-settings-title");
            return title;
        }

        private void SaveCardAppearance()
        {
            UStickiesUserSettings.instance.SetCardAppearance(
                _maximumWidthField.value,
                _maximumHeightField.value,
                _useCustomTextColorToggle.value,
                _textColorField.value);
            RefreshCardAppearance();
        }

        private void RefreshCardAppearance()
        {
            var settings = UStickiesUserSettings.instance;
            _maximumWidthField.SetValueWithoutNotify(settings.cardMaximumWidth);
            _maximumHeightField.SetValueWithoutNotify(settings.cardMaximumHeight);
            _useCustomTextColorToggle.SetValueWithoutNotify(settings.useCustomCardTextColor);
            _textColorField.SetValueWithoutNotify(settings.cardTextColor);
            _textColorField.SetEnabled(settings.useCustomCardTextColor);
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

        private void RefreshCategories()
        {
            _viewCategories = new List<UStickiesCategory>(UStickiesProjectSettings.instance.categories);
            _categoryList.itemsSource = _viewCategories;
            _categoryList.Rebuild();
        }
    }
}
