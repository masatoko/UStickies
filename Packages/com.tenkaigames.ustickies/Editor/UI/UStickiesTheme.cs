using UnityEditor;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    internal static class UStickiesTheme
    {
        private const string StylePath = "Packages/com.tenkaigames.ustickies/Editor/UI/UStickiesTheme.uss";

        public static void Apply(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            root.AddToClassList(EditorGUIUtility.isProSkin ? "ustickies-dark" : "ustickies-light");
        }
    }
}
