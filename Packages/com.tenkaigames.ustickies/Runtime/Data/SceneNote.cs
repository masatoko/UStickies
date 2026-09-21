using System;

using UnityEngine;

namespace Tenkai.UStickies
{
    /// <summary>
    /// Scene内に保存する1件のノートデータ。
    /// </summary>
    [Serializable]
    internal sealed class SceneNote
    {
        [SerializeField] private string _id;
        [SerializeField] private string _body;
        [SerializeField] private string _categoryId;
        [SerializeField] private bool _done;
        [SerializeField] private bool _pinned;
        [SerializeField] private Vector3 _worldPosition;
        [SerializeField] private GameObject _target;
        [SerializeField] private bool _isBound;
        [SerializeField] private bool _missingTargetNotified;
        [SerializeField] private long _createdAtUtcTicks;
        [SerializeField] private long _registrationOrder;

        public string id => _id;
        public string body => _body ?? string.Empty;
        public string categoryId => _categoryId;
        public bool done => _done;
        public bool pinned => _pinned;
        public GameObject target => _target;
        public bool isBound => _isBound && _target != null;
        public bool hasMissingTarget => _isBound && _target == null;
        public long createdAtUtcTicks => _createdAtUtcTicks;
        public long registrationOrder => _registrationOrder;
        public Vector3 position => isBound ? _target.transform.position : _worldPosition;

        public static SceneNote Create(
            Vector3 position,
            GameObject target,
            string categoryId,
            long registrationOrder)
        {
            return new SceneNote
            {
                _id = Guid.NewGuid().ToString("N"),
                _body = string.Empty,
                _categoryId = categoryId,
                _worldPosition = target != null ? target.transform.position : position,
                _target = target,
                _isBound = target != null,
                _createdAtUtcTicks = DateTime.UtcNow.Ticks,
                _registrationOrder = registrationOrder
            };
        }

        /// <summary>
        /// 編集用の一時値を反映し、紐づけ変更時の座標も確定する。
        /// </summary>
        public void Apply(SceneNoteEdit edit)
        {
            _body = edit.body ?? string.Empty;
            _categoryId = edit.categoryId;
            _done = edit.done;
            _pinned = edit.pinned;

            if (edit.target != null)
            {
                _target = edit.target;
                _isBound = true;
                _worldPosition = edit.target.transform.position;
                return;
            }

            if (isBound)
            {
                _worldPosition = _target.transform.position;
            }

            _target = null;
            _isBound = false;
        }

        public void SetPosition(Vector3 position)
        {
            if (isBound)
            {
                return;
            }

            // 削除済み参照をUndo用に保持している場合、手動移動を正式な紐づけ解除とする。
            if (hasMissingTarget)
            {
                _target = null;
                _isBound = false;
                _missingTargetNotified = false;
            }

            _worldPosition = position;
        }

        public void SetDone(bool value) =>
            _done = value;

        public void SetPinned(bool value) =>
            _pinned = value;

        public void SetCategory(string value) =>
            _categoryId = value;

        /// <summary>
        /// GameObject削除後にも残せるよう、現在のTransform位置を退避する。
        /// </summary>
        public bool CaptureTargetPosition()
        {
            if (!isBound)
            {
                return false;
            }

            var currentPosition = _target.transform.position;
            var notificationChanged = _missingTargetNotified;
            _missingTargetNotified = false;
            if ((_worldPosition - currentPosition).sqrMagnitude < 0.000001f)
            {
                return notificationChanged;
            }

            _worldPosition = currentPosition;
            return true;
        }

        public bool MarkMissingTargetNotified()
        {
            if (_missingTargetNotified)
            {
                return false;
            }

            _missingTargetNotified = true;
            return true;
        }
    }

    internal readonly struct SceneNoteEdit
    {
        public readonly string body;
        public readonly string categoryId;
        public readonly bool done;
        public readonly bool pinned;
        public readonly GameObject target;

        public SceneNoteEdit(
            string body,
            string categoryId,
            bool done,
            bool pinned,
            GameObject target)
        {
            this.body = body;
            this.categoryId = categoryId;
            this.done = done;
            this.pinned = pinned;
            this.target = target;
        }
    }
}
