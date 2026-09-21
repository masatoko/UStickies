using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    /// <summary>
    /// Scene選択、検索、フィルタ、ノート一覧操作を提供するメインWindow。
    /// </summary>
    internal sealed class UStickiesNotesWindow : EditorWindow
    {
        [SerializeField] private int _selectedSceneHandle;

        private readonly List<int> _sceneHandles = new();
        private DropdownField _sceneField;
        private ToolbarSearchField _searchField;
        private ToolbarMenu _categoryMenu;
        private ToolbarMenu _completionMenu;
        private ToolbarMenu _sortMenu;
        private ToolbarToggle _applyToSceneToggle;
        private ToolbarToggle _visibilityToggle;
        private ScrollView _list;



        // ===== Window Lifecycle ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Window Lifecycle

        [MenuItem("Window/UStickies/Notes")]
        public static void Open() =>
            GetWindow<UStickiesNotesWindow>("UStickies Notes");

        private void OnEnable()
        {
            SceneNoteMutationService.changed += Rebuild;
            SceneNoteSelection.changed += Rebuild;
            EditorApplication.hierarchyChanged += Rebuild;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorSceneManager.sceneClosed += HandleSceneClosed;
        }

        private void OnDisable()
        {
            SceneNoteMutationService.changed -= Rebuild;
            SceneNoteSelection.changed -= Rebuild;
            EditorApplication.hierarchyChanged -= Rebuild;
            EditorSceneManager.sceneOpened -= HandleSceneOpened;
            EditorSceneManager.sceneClosed -= HandleSceneClosed;
        }

        public void CreateGUI()
        {
            UStickiesTheme.Apply(rootVisualElement);
            rootVisualElement.AddToClassList("ustickies-root");
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyDown);

            BuildPrimaryToolbar();
            BuildFilterToolbar();

            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            rootVisualElement.Add(_list);
            Rebuild();
        }

        #endregion



        // ===== UI Construction ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region UI Construction

        private void BuildPrimaryToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("ustickies-toolbar");

            var addButton = new ToolbarButton(AddNewNote) { text = "Add New Note" };
            toolbar.Add(addButton);

            _sceneField = new DropdownField();
            _sceneField.style.minWidth = 145f;
            _sceneField.RegisterValueChangedCallback(_ => SelectSceneFromField());
            toolbar.Add(_sceneField);

            var spacer = new VisualElement();
            spacer.AddToClassList("ustickies-spacer");
            toolbar.Add(spacer);

            _visibilityToggle = new ToolbarToggle { text = "Visible" };
            _visibilityToggle.SetValueWithoutNotify(UStickiesUserSettings.instance.notesVisible);
            _visibilityToggle.RegisterValueChangedCallback(evt =>
                UStickiesUserSettings.instance.SetNotesVisible(evt.newValue));
            toolbar.Add(_visibilityToggle);
            rootVisualElement.Add(toolbar);
        }

        private void BuildFilterToolbar()
        {
            var searchToolbar = new Toolbar();
            searchToolbar.AddToClassList("ustickies-toolbar");

            _searchField = new ToolbarSearchField { value = UStickiesUserSettings.instance.searchText };
            _searchField.style.flexGrow = 1f;
            _searchField.RegisterValueChangedCallback(evt =>
                UStickiesUserSettings.instance.SetSearchText(evt.newValue));
            searchToolbar.Add(_searchField);
            rootVisualElement.Add(searchToolbar);

            var filterToolbar = new Toolbar();
            filterToolbar.AddToClassList("ustickies-toolbar");

            _categoryMenu = new ToolbarMenu { text = "Categories" };
            filterToolbar.Add(_categoryMenu);
            _completionMenu = new ToolbarMenu();
            filterToolbar.Add(_completionMenu);
            _sortMenu = new ToolbarMenu();
            filterToolbar.Add(_sortMenu);

            _applyToSceneToggle = new ToolbarToggle { text = "Apply to Scene" };
            _applyToSceneToggle.SetValueWithoutNotify(UStickiesUserSettings.instance.applyFiltersToSceneView);
            _applyToSceneToggle.RegisterValueChangedCallback(evt =>
                UStickiesUserSettings.instance.SetApplyFiltersToSceneView(evt.newValue));
            filterToolbar.Add(_applyToSceneToggle);
            rootVisualElement.Add(filterToolbar);

            RebuildMenus();
        }

        #endregion



        // ===== Note List ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Note List

        private void Rebuild()
        {
            if (_list == null)
            {
                return;
            }

            RefreshScenes();
            RebuildMenus();
            _visibilityToggle.SetValueWithoutNotify(UStickiesUserSettings.instance.notesVisible);
            _applyToSceneToggle.SetValueWithoutNotify(UStickiesUserSettings.instance.applyFiltersToSceneView);
            _list.Clear();

            var scene = SelectedScene();
            var database = SceneNoteRepository.Get(scene, false);
            if (database == null || database.notes.Count == 0)
            {
                var empty = new Label("No notes in this Scene.");
                empty.AddToClassList("ustickies-muted");
                empty.style.marginTop = 18f;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                _list.Add(empty);
                return;
            }

            foreach (var note in SceneNoteQuery.Apply(database.notes, UStickiesUserSettings.instance))
            {
                _list.Add(CreateNoteRow(scene, database, note));
            }
        }

        private VisualElement CreateNoteRow(Scene scene, SceneNoteDatabase database, SceneNote note)
        {
            var row = new VisualElement();
            row.AddToClassList("ustickies-row");
            if (note.done)
            {
                row.AddToClassList("ustickies-row--done");
            }

            var selected = SceneNoteSelection.selectedNoteId == note.id;
            if (selected)
            {
                row.AddToClassList("ustickies-row--selected");
            }

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var category = UStickiesProjectSettings.instance.Resolve(note.categoryId);
            var marker = new VisualElement();
            marker.style.width = 8f;
            marker.style.height = 8f;
            marker.style.marginRight = 7f;
            marker.style.backgroundColor = category.color;
            header.Add(marker);

            var summary = new Label(FirstLine(note.body));
            summary.style.flexGrow = 1f;
            header.Add(summary);

            var metadata = new Label(note.done ? $"{category.label} · Done" : category.label);
            metadata.AddToClassList("ustickies-muted");
            header.Add(metadata);
            row.Add(header);

            if (selected)
            {
                AddExpandedContent(row, scene, database, note);
            }

            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    ShowContextMenu(scene, database, note);
                    evt.StopPropagation();
                    return;
                }

                if (evt.button != 0)
                {
                    return;
                }

                SceneNoteSelection.Select(note.id);
                rootVisualElement.Focus();
                if (evt.clickCount >= 2)
                {
                    UStickiesNoteEditorWindow.Open(scene, note, false);
                }

                evt.StopPropagation();
            });
            return row;
        }

        private void AddExpandedContent(
            VisualElement row,
            Scene scene,
            SceneNoteDatabase database,
            SceneNote note)
        {
            var body = new ScrollView(ScrollViewMode.Vertical);
            body.style.maxHeight = 180f;
            body.style.marginTop = 8f;
            var bodyLabel = new Label(string.IsNullOrEmpty(note.body) ? "空のノート" : note.body);
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            body.Add(bodyLabel);
            row.Add(body);

            var actions = new VisualElement();
            actions.AddToClassList("ustickies-actions");
            actions.Add(new Button(() => MoveTo(note)) { text = "Move" });
            actions.Add(new Button(() => UStickiesNoteEditorWindow.Open(scene, note, false)) { text = "Edit" });
            actions.Add(new Button(() =>
                SceneNoteMutationService.SetDone(scene, database, note, !note.done))
            { text = note.done ? "Undone" : "Done" });
            actions.Add(new Button(() =>
                SceneNoteMutationService.SetPinned(scene, database, note, !note.pinned))
            { text = note.pinned ? "Unpin" : "Pin" });
            var delete = new Button(() => SceneNoteMutationService.Delete(scene, database, note)) { text = "Delete" };
            delete.AddToClassList("ustickies-danger");
            actions.Add(delete);
            row.Add(actions);
        }

        #endregion



        // ===== Filters And Scenes ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Filters And Scenes

        private void RebuildMenus()
        {
            if (_categoryMenu == null)
            {
                return;
            }

            var settings = UStickiesUserSettings.instance;
            _categoryMenu.menu.MenuItems().Clear();
            _categoryMenu.menu.AppendAction("All", _ => settings.SelectAllCategories(), action =>
                settings.allCategories ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            _categoryMenu.menu.AppendAction("Clear", _ => settings.ClearCategories());
            _categoryMenu.menu.AppendSeparator();
            foreach (var category in UStickiesProjectSettings.instance.categories)
            {
                var capturedId = category.id;
                _categoryMenu.menu.AppendAction(category.label, _ =>
                    settings.SetCategorySelected(capturedId, !settings.IsCategorySelected(capturedId)), action =>
                    settings.IsCategorySelected(capturedId)
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }

            _completionMenu.text = CompletionLabel(settings.completionFilter);
            _completionMenu.menu.MenuItems().Clear();
            AppendEnumMenu(_completionMenu, SceneNoteCompletionFilter.All, "All");
            AppendEnumMenu(_completionMenu, SceneNoteCompletionFilter.Undone, "Undone");
            AppendEnumMenu(_completionMenu, SceneNoteCompletionFilter.Done, "Done");

            _sortMenu.text = SortLabel(settings.sortMode);
            _sortMenu.menu.MenuItems().Clear();
            AppendSortMenu(SceneNoteSortMode.Newest, "Newest");
            AppendSortMenu(SceneNoteSortMode.Oldest, "Oldest");
            AppendSortMenu(SceneNoteSortMode.Registration, "Scene order");
            AppendSortMenu(SceneNoteSortMode.Category, "Category");
        }

        private void AppendEnumMenu(ToolbarMenu menu, SceneNoteCompletionFilter value, string label)
        {
            menu.menu.AppendAction(label, _ => UStickiesUserSettings.instance.SetCompletionFilter(value), action =>
                UStickiesUserSettings.instance.completionFilter == value
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        private void AppendSortMenu(SceneNoteSortMode value, string label)
        {
            _sortMenu.menu.AppendAction(label, _ => UStickiesUserSettings.instance.SetSortMode(value), action =>
                UStickiesUserSettings.instance.sortMode == value
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        private void RefreshScenes()
        {
            var labels = new List<string>();
            _sceneHandles.Clear();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                _sceneHandles.Add(scene.handle);
                labels.Add(SceneLabel(scene));
            }

            if (!_sceneHandles.Contains(_selectedSceneHandle))
            {
                _selectedSceneHandle = SceneManager.GetActiveScene().handle;
            }

            var selectedIndex = Mathf.Max(0, _sceneHandles.IndexOf(_selectedSceneHandle));
            _sceneField.choices = labels;
            if (labels.Count > 0)
            {
                _sceneField.SetValueWithoutNotify(labels[selectedIndex]);
            }
        }

        private Scene SelectedScene()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.handle == _selectedSceneHandle)
                {
                    return scene;
                }
            }

            return SceneManager.GetActiveScene();
        }

        private void SelectSceneFromField()
        {
            var index = _sceneField.index;
            if (index >= 0 && index < _sceneHandles.Count)
            {
                _selectedSceneHandle = _sceneHandles[index];
                Rebuild();
            }
        }

        #endregion



        // ===== Actions And Input ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Actions And Input

        private void AddNewNote()
        {
            var scene = SelectedScene();
            if (!scene.IsValid())
            {
                return;
            }

            var sceneView = SceneView.lastActiveSceneView;
            var position = sceneView != null ? sceneView.pivot : Vector3.zero;
            var note = SceneNoteMutationService.Add(scene, position);
            if (note != null)
            {
                UStickiesNoteEditorWindow.Open(scene, note, true);
            }
        }

        private static void MoveTo(SceneNote note)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }

            sceneView.LookAt(note.position, sceneView.rotation, 5f, sceneView.orthographic);
            sceneView.Repaint();
        }

        private static void ShowContextMenu(Scene scene, SceneNoteDatabase database, SceneNote note)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Move"), false, () => MoveTo(note));
            menu.AddItem(new GUIContent("Edit"), false, () => UStickiesNoteEditorWindow.Open(scene, note, false));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(note.done ? "Undone" : "Done"), false, () =>
                SceneNoteMutationService.SetDone(scene, database, note, !note.done));
            menu.AddItem(new GUIContent(note.pinned ? "Unpin" : "Pin"), false, () =>
                SceneNoteMutationService.SetPinned(scene, database, note, !note.pinned));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete"), false, () => SceneNoteMutationService.Delete(scene, database, note));
            menu.ShowAsContext();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            var scene = SelectedScene();
            var database = SceneNoteRepository.Get(scene, false);
            if (database == null)
            {
                return;
            }

            var notes = SceneNoteQuery.Apply(database.notes, UStickiesUserSettings.instance);
            var selectedIndex = notes.FindIndex(note => note.id == SceneNoteSelection.selectedNoteId);
            if (evt.keyCode is KeyCode.UpArrow or KeyCode.DownArrow)
            {
                var delta = evt.keyCode == KeyCode.UpArrow ? -1 : 1;
                var nextIndex = Mathf.Clamp(selectedIndex < 0 ? 0 : selectedIndex + delta, 0, notes.Count - 1);
                if (notes.Count > 0)
                {
                    SceneNoteSelection.Select(notes[nextIndex].id);
                }

                evt.StopPropagation();
            }
            else if (selectedIndex >= 0 && evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                UStickiesNoteEditorWindow.Open(scene, notes[selectedIndex], false);
                evt.StopPropagation();
            }
            else if (selectedIndex >= 0 && evt.keyCode == KeyCode.Delete)
            {
                SceneNoteMutationService.Delete(scene, database, notes[selectedIndex]);
                evt.StopPropagation();
            }
        }

        private void HandleSceneOpened(Scene scene, OpenSceneMode mode) =>
            Rebuild();

        private void HandleSceneClosed(Scene scene) =>
            Rebuild();

        #endregion



        // ===== Formatting ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Formatting

        private static string FirstLine(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return "空のノート";
            }

            var lineBreak = body.IndexOfAny(new[] { '\r', '\n' });
            return lineBreak >= 0 ? body[..lineBreak] : body;
        }

        private static string SceneLabel(Scene scene)
        {
            var fileName = string.IsNullOrEmpty(scene.path) ? scene.name : Path.GetFileNameWithoutExtension(scene.path);
            return string.IsNullOrEmpty(fileName) ? "Untitled Scene" : fileName;
        }

        private static string CompletionLabel(SceneNoteCompletionFilter value) => value switch
        {
            SceneNoteCompletionFilter.Undone => "Undone",
            SceneNoteCompletionFilter.Done => "Done",
            _ => "All states"
        };

        private static string SortLabel(SceneNoteSortMode value) => value switch
        {
            SceneNoteSortMode.Oldest => "Oldest",
            SceneNoteSortMode.Registration => "Scene order",
            SceneNoteSortMode.Category => "Category",
            _ => "Newest"
        };

        #endregion
    }
}
