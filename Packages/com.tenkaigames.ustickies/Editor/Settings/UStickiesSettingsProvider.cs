using UnityEditor;
using UnityEngine;

namespace Tenkai.UStickies
{
    /// <summary>
    /// UStickiesの表示設定とカテゴリ設定をProject Settingsへ公開する。
    /// </summary>
    internal static class UStickiesSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/UStickies", SettingsScope.Project)
            {
                label = "UStickies",
                activateHandler = (_, root) => root.Add(new UStickiesSettingsView()),
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<UStickiesSettingsProviderKeywords>()
            };
        }

        private sealed class UStickiesSettingsProviderKeywords
        {
            public static readonly GUIContent NoteCard = new("Note Card Appearance");
            public static readonly GUIContent MaximumWidth = new("Maximum Width");
            public static readonly GUIContent MaximumHeight = new("Maximum Height");
            public static readonly GUIContent TextColor = new("Text Color");
            public static readonly GUIContent FontSize = new("Font Size");
            public static readonly GUIContent StickyColor = new("Sticky Color");
            public static readonly GUIContent Opacity = new("Opacity");
            public static readonly GUIContent Categories = new("Categories");
        }
    }
}
