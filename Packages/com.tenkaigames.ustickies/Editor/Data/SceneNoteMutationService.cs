using System;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tenkai.UStickies
{
    /// <summary>
    /// Sceneデータへの変更、Undo登録、dirty通知を一元管理する。
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneNoteMutationService
    {
        private static string _deletedSelectedNoteId;

        public static event Action changed;

        static SceneNoteMutationService()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        public static SceneNote Add(Scene scene, Vector3 position, GameObject target = null)
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add UStickies Note");

            var database = SceneNoteRepository.Get(scene, true);
            if (database == null)
            {
                Undo.RevertAllDownToGroup(group);
                return null;
            }

            Undo.RecordObject(database, "Add UStickies Note");
            var note = database.Add(position, target, UStickiesProjectSettings.instance.defaultCategoryId);
            MarkChanged(scene, database);
            Undo.CollapseUndoOperations(group);
            SceneNoteSelection.Select(note.id);
            return note;
        }

        public static void Apply(Scene scene, SceneNoteDatabase database, SceneNote note, SceneNoteEdit edit)
        {
            Undo.RecordObject(database, "Edit UStickies Note");
            note.Apply(edit);
            MarkChanged(scene, database);
        }

        public static void Delete(Scene scene, SceneNoteDatabase database, SceneNote note)
        {
            Undo.RecordObject(database, "Delete UStickies Note");
            if (!database.Remove(note.id))
            {
                return;
            }

            if (SceneNoteSelection.selectedNoteId == note.id)
            {
                _deletedSelectedNoteId = note.id;
                SceneNoteSelection.Clear();
            }

            MarkChanged(scene, database);
        }

        public static void Move(Scene scene, SceneNoteDatabase database, SceneNote note, Vector3 position)
        {
            if (note.isBound)
            {
                return;
            }

            Undo.RecordObject(database, "Move UStickies Note");
            note.SetPosition(position);
            MarkChanged(scene, database);
        }

        public static void SetDone(Scene scene, SceneNoteDatabase database, SceneNote note, bool value)
        {
            Undo.RecordObject(database, "Set UStickies Note Completion");
            note.SetDone(value);
            MarkChanged(scene, database);
        }

        public static void SetPinned(Scene scene, SceneNoteDatabase database, SceneNote note, bool value)
        {
            Undo.RecordObject(database, "Set UStickies Note Visibility");
            note.SetPinned(value);
            MarkChanged(scene, database);
        }

        /// <summary>
        /// GameObjectの最終座標を同期し、削除済み参照の通知状態を更新する。
        /// </summary>
        public static void Reconcile(Scene scene, SceneNoteDatabase database)
        {
            var changedDatabase = false;
            foreach (var note in database.notes)
            {
                if (note.isBound)
                {
                    if (note.CaptureTargetPosition())
                    {
                        EditorUtility.SetDirty(database);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                    continue;
                }

                if (!note.hasMissingTarget)
                {
                    continue;
                }

                if (note.MarkMissingTargetNotified())
                {
                    changedDatabase = true;
                    const string message = "A linked GameObject was removed. The note was kept at its last position.";
                    SceneView.lastActiveSceneView?.ShowNotification(new GUIContent($"UStickies: {message}"));
                    Debug.LogWarning($"UStickies: {message}");
                }
            }

            if (changedDatabase)
            {
                MarkChanged(scene, database);
            }
        }

        /// <summary>
        /// 削除されたカテゴリIDを、プロジェクト設定に記録された置換先へ移行する。
        /// </summary>
        public static void ReconcileCategories()
        {
            foreach (var entry in SceneNoteRepository.GetOpenDatabases())
            {
                var changedDatabase = false;
                foreach (var note in entry.database.notes)
                {
                    var resolvedId = UStickiesProjectSettings.instance.ResolveId(note.categoryId);
                    if (resolvedId == note.categoryId)
                    {
                        continue;
                    }

                    if (!changedDatabase)
                    {
                        Undo.RecordObject(entry.database, "Migrate UStickies Categories");
                    }

                    note.SetCategory(resolvedId);
                    changedDatabase = true;
                }

                if (changedDatabase)
                {
                    MarkChanged(entry.scene, entry.database);
                }
            }
        }

        public static void NotifyViewChanged()
        {
            changed?.Invoke();
            SceneView.RepaintAll();
        }

        public static void MoveDuringDrag(Scene scene, SceneNoteDatabase database, SceneNote note, Vector3 position)
        {
            note.SetPosition(position);
            EditorUtility.SetDirty(database);
            EditorSceneManager.MarkSceneDirty(scene);
            changed?.Invoke();
        }

        private static void MarkChanged(Scene scene, SceneNoteDatabase database)
        {
            EditorUtility.SetDirty(database);
            EditorSceneManager.MarkSceneDirty(scene);
            changed?.Invoke();
            SceneView.RepaintAll();
        }

        private static void HandleUndoRedo()
        {
            if (!string.IsNullOrEmpty(_deletedSelectedNoteId)
                && string.IsNullOrEmpty(SceneNoteSelection.selectedNoteId)
                && SceneNoteRepository.TryFind(_deletedSelectedNoteId, out _, out _, out _))
            {
                SceneNoteSelection.Select(_deletedSelectedNoteId);
                _deletedSelectedNoteId = null;
            }

            changed?.Invoke();
            SceneView.RepaintAll();
        }
    }
}
