using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    /// <summary>
    /// 新規作成と既存編集で共有する、外部クリックでは閉じない編集Window。
    /// </summary>
    internal sealed class UStickiesNoteEditorWindow : EditorWindow
    {
        [SerializeField] private int _sceneHandle;
        [SerializeField] private string _noteId;
        [SerializeField] private bool _isNew;
        [SerializeField] private bool _saved;

        private TextField _bodyField;
        private DropdownField _categoryField;
        private Toggle _doneField;
        private Toggle _pinnedField;
        private ObjectField _targetField;
        private readonly List<string> _categoryIds = new();

        public static void Open(Scene scene, SceneNote note, bool isNew)
        {
            var window = CreateInstance<UStickiesNoteEditorWindow>();
            window._sceneHandle = scene.handle;
            window._noteId = note.id;
            window._isNew = isNew;
            window._saved = false;
            window.titleContent = new GUIContent(isNew ? "New USticky" : "Edit USticky");
            window.minSize = new Vector2(360f, isNew ? 245f : 330f);
            window.maxSize = new Vector2(620f, 720f);
            window.ShowAuxWindow();
            window.Focus();
        }

        public void CreateGUI()
        {
            UStickiesTheme.Apply(rootVisualElement);
            rootVisualElement.AddToClassList("ustickies-root");
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyDown);

            if (!TryResolve(out _, out _, out var note))
            {
                rootVisualElement.Add(new Label("The note is no longer available."));
                return;
            }

            var panel = new VisualElement();
            panel.AddToClassList("ustickies-panel");
            rootVisualElement.Add(panel);

            _bodyField = new TextField("Body")
            {
                multiline = true,
                value = note.body
            };
            _bodyField.style.minHeight = 120f;
            panel.Add(_bodyField);

            BuildCategoryField(note, panel);

            if (!_isNew)
            {
                _doneField = new Toggle("Done") { value = note.done };
                _pinnedField = new Toggle("Pin") { value = note.pinned };
                _targetField = new ObjectField("GameObject")
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true,
                    value = note.target
                };
                panel.Add(_doneField);
                panel.Add(_pinnedField);
                panel.Add(_targetField);
            }

            var actions = new VisualElement();
            actions.AddToClassList("ustickies-actions");
            var cancel = new Button(Cancel) { text = "Cancel" };
            var save = new Button(Save) { text = "Save" };
            save.AddToClassList("ustickies-primary");
            actions.Add(cancel);
            actions.Add(save);
            panel.Add(actions);

            _bodyField.Focus();
        }

        private void OnDestroy()
        {
            if (!_isNew || _saved || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            DeleteDraft();
        }

        private void BuildCategoryField(SceneNote note, VisualElement parent)
        {
            var labels = new List<string>();
            var selectedIndex = 0;
            var resolvedNoteCategory = UStickiesProjectSettings.instance.ResolveId(note.categoryId);
            var categories = UStickiesProjectSettings.instance.categories;
            for (var index = 0; index < categories.Count; index++)
            {
                var category = categories[index];
                _categoryIds.Add(category.id);
                labels.Add(category.label);
                if (category.id == resolvedNoteCategory)
                {
                    selectedIndex = index;
                }
            }

            _categoryField = new DropdownField("Category", labels, selectedIndex);
            parent.Add(_categoryField);
        }

        private void Save()
        {
            if (!TryResolve(out var scene, out var database, out var note))
            {
                Close();
                return;
            }

            var target = _isNew ? note.target : _targetField.value as GameObject;
            if (target != null && (!target.scene.IsValid() || target.scene.handle != scene.handle))
            {
                ShowNotification(new GUIContent("Select a GameObject from the same Scene."));
                return;
            }

            var categoryIndex = Mathf.Max(0, _categoryField.index);
            var edit = new SceneNoteEdit(
                _bodyField.value,
                _categoryIds[categoryIndex],
                _isNew ? note.done : _doneField.value,
                _isNew ? note.pinned : _pinnedField.value,
                target);
            SceneNoteMutationService.Apply(scene, database, note, edit);
            _saved = true;
            Close();
        }

        private void Cancel()
        {
            if (_isNew)
            {
                DeleteDraft();
            }

            _saved = true;
            Close();
        }

        private void DeleteDraft()
        {
            if (TryResolve(out var scene, out var database, out var note))
            {
                SceneNoteMutationService.Delete(scene, database, note);
            }
        }

        private bool TryResolve(out Scene scene, out SceneNoteDatabase database, out SceneNote note)
        {
            scene = FindScene(_sceneHandle);
            database = SceneNoteRepository.Get(scene, false);
            note = database != null ? database.Find(_noteId) : null;
            return scene.IsValid() && note != null;
        }

        private static Scene FindScene(int handle)
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.handle == handle)
                {
                    return scene;
                }
            }

            return default;
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return && (evt.ctrlKey || evt.commandKey))
            {
                Save();
                evt.StopPropagation();
            }
        }
    }
}
