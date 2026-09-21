using System.Collections.Generic;

using UnityEngine;

namespace Tenkai.UStickies
{
    /// <summary>
    /// 1つのSceneに属する全ノートを保持する非表示コンポーネント。
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class SceneNoteDatabase : MonoBehaviour
    {
        [SerializeField] private int _dataVersion = 1;
        [SerializeField] private long _nextRegistrationOrder;
        [SerializeField] private List<SceneNote> _notes = new();

        public int dataVersion => _dataVersion;
        public IReadOnlyList<SceneNote> notes => _notes;

        public SceneNote Find(string noteId) =>
            _notes.Find(note => note.id == noteId);

        public SceneNote Add(Vector3 position, GameObject target, string categoryId)
        {
            var note = SceneNote.Create(position, target, categoryId, _nextRegistrationOrder++);
            _notes.Add(note);
            return note;
        }

        public bool Remove(string noteId)
        {
            var index = _notes.FindIndex(note => note.id == noteId);
            if (index < 0)
            {
                return false;
            }

            _notes.RemoveAt(index);
            return true;
        }
    }
}
