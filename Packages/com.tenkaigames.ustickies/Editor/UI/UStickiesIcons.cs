using UnityEditor;
using UnityEngine;

namespace Tenkai.UStickies
{
    internal static class UStickiesIcons
    {
        private const string IconRoot = "Packages/com.tenkaigames.ustickies/Editor/Icons/";

        private static Texture2D _note;
        private static Texture2D _done;

        public static Texture2D note =>
            _note != null ? _note : _note = AssetDatabase.LoadAssetAtPath<Texture2D>(IconRoot + "note.png");

        public static Texture2D done =>
            _done != null ? _done : _done = AssetDatabase.LoadAssetAtPath<Texture2D>(IconRoot + "note-done.png");
    }
}
