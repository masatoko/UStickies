using NUnit.Framework;

using UnityEngine;

namespace Tenkai.UStickies.Tests
{
    internal sealed class SceneNoteTests
    {
        [Test]
        public void CreateFixedNote_UsesSpecifiedPositionAndEmptyBody()
        {
            var position = new Vector3(1f, 2f, 3f);

            var note = SceneNote.Create(position, null, UStickiesProjectSettings.NoteCategoryId, 7L);

            Assert.That(note.body, Is.Empty);
            Assert.That(note.position, Is.EqualTo(position));
            Assert.That(note.registrationOrder, Is.EqualTo(7L));
            Assert.That(note.isBound, Is.False);
        }

        [Test]
        public void ApplyTargetThenUnbind_KeepsLastTargetPosition()
        {
            var target = new GameObject("UStickies Test Target");
            target.transform.position = new Vector3(4f, 5f, 6f);
            var note = SceneNote.Create(Vector3.zero, null, UStickiesProjectSettings.NoteCategoryId, 0L);

            try
            {
                note.Apply(new SceneNoteEdit("body", UStickiesProjectSettings.TodoCategoryId, false, false, target));
                Assert.That(note.isBound, Is.True);
                Assert.That(note.position, Is.EqualTo(target.transform.position));

                note.Apply(new SceneNoteEdit("body", UStickiesProjectSettings.TodoCategoryId, false, false, null));
                Assert.That(note.isBound, Is.False);
                Assert.That(note.position, Is.EqualTo(target.transform.position));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void EditorOnlyDatabase_CanBeAttachedToGameObject()
        {
            var gameObject = new GameObject("UStickies Test Database");

            try
            {
                var database = gameObject.AddComponent<SceneNoteDatabase>();

                Assert.That(database, Is.Not.Null);
                Assert.That(database.dataVersion, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
