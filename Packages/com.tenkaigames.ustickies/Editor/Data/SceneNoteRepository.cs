using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tenkai.UStickies
{
    /// <summary>
    /// 開いているSceneからノートデータベースを検索・生成する。
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneNoteRepository
    {
        private const string DatabaseObjectName = "[UStickies]";
        private static double _nextPositionSync;

        static SceneNoteRepository()
        {
            EditorApplication.hierarchyChanged += ReconcileOpenScenes;
            EditorSceneManager.sceneOpened += (_, _) => ReconcileOpenScenes();
            EditorApplication.update += SyncLinkedPositions;
        }

        /// <summary>
        /// 指定Sceneのデータベースを取得し、必要な場合だけ新規生成する。
        /// </summary>
        public static SceneNoteDatabase Get(Scene scene, bool create)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var database = root.GetComponent<SceneNoteDatabase>();
                if (database != null)
                {
                    EnsureDatabaseObjectFlags(root);
                    return database;
                }
            }

            return create ? Create(scene) : null;
        }

        public static IEnumerable<(Scene scene, SceneNoteDatabase database)> GetOpenDatabases()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                var database = Get(scene, false);
                if (database != null)
                {
                    yield return (scene, database);
                }
            }
        }

        public static bool TryFind(string noteId, out Scene scene, out SceneNoteDatabase database, out SceneNote note)
        {
            foreach (var entry in GetOpenDatabases())
            {
                note = entry.database.Find(noteId);
                if (note == null)
                {
                    continue;
                }

                scene = entry.scene;
                database = entry.database;
                return true;
            }

            scene = default;
            database = null;
            note = null;
            return false;
        }

        private static SceneNoteDatabase Create(Scene scene)
        {
            var gameObject = FindOrphanDatabaseObject(scene);
            var created = gameObject == null;
            if (created)
            {
                gameObject = new GameObject(DatabaseObjectName);
                Undo.RegisterCreatedObjectUndo(gameObject, "Create UStickies Database");
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            EnsureDatabaseObjectFlags(gameObject);

            var database = Undo.AddComponent<SceneNoteDatabase>(gameObject);
            if (database == null)
            {
                if (created)
                {
                    Undo.DestroyObjectImmediate(gameObject);
                }

                Debug.LogError("UStickies: Failed to create the Scene note database.");
                return null;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return database;
        }

        /// <summary>
        /// コンポーネント追加に失敗した過去の管理Objectがあれば再利用する。
        /// </summary>
        private static GameObject FindOrphanDatabaseObject(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == DatabaseObjectName
                    && root.CompareTag("EditorOnly")
                    && root.GetComponent<SceneNoteDatabase>() == null)
                {
                    return root;
                }
            }

            return null;
        }

        private static void EnsureDatabaseObjectFlags(GameObject gameObject)
        {
            var expectedFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
            if (gameObject.hideFlags != expectedFlags)
            {
                gameObject.hideFlags = expectedFlags;
                EditorUtility.SetDirty(gameObject);
            }

            if (!gameObject.CompareTag("EditorOnly"))
            {
                gameObject.tag = "EditorOnly";
                EditorUtility.SetDirty(gameObject);
            }
        }

        private static void ReconcileOpenScenes()
        {
            foreach (var entry in GetOpenDatabases())
            {
                SceneNoteMutationService.Reconcile(entry.scene, entry.database);
            }

            SceneNoteMutationService.ReconcileCategories();
        }

        private static void SyncLinkedPositions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.timeSinceStartup < _nextPositionSync)
            {
                return;
            }

            _nextPositionSync = EditorApplication.timeSinceStartup + 0.25d;
            foreach (var entry in GetOpenDatabases())
            {
                SceneNoteMutationService.Reconcile(entry.scene, entry.database);
            }
        }
    }
}
