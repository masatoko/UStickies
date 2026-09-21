using System;

namespace Tenkai.UStickies
{
    internal static class SceneNoteSelection
    {
        public static event Action changed;

        public static string selectedNoteId { get; private set; }

        public static void Select(string noteId)
        {
            if (selectedNoteId == noteId)
            {
                return;
            }

            selectedNoteId = noteId;
            changed?.Invoke();
        }

        public static void Clear() =>
            Select(null);
    }
}
