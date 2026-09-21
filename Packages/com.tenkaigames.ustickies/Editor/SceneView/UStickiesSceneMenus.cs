using UnityEditor;
using UnityEngine;

namespace Tenkai.UStickies
{
    internal static class UStickiesSceneMenus
    {
        [MenuItem("GameObject/UStickies/Add Note", false, 30)]
        private static void AddNoteToSelectedGameObject()
        {
            var target = Selection.activeGameObject;
            if (target == null || !target.scene.IsValid())
            {
                return;
            }

            var note = SceneNoteMutationService.Add(target.scene, target.transform.position, target);
            if (note != null)
            {
                UStickiesNoteEditorWindow.Open(target.scene, note, true);
            }
        }

        [MenuItem("GameObject/UStickies/Add Note", true)]
        private static bool ValidateAddNoteToSelectedGameObject() =>
            Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid();
    }
}
