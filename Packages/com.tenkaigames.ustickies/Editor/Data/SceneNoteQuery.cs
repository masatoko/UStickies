using System;
using System.Collections.Generic;
using System.Linq;

namespace Tenkai.UStickies
{
    /// <summary>
    /// ノートをユーザー設定に従って絞り込み、表示順へ並べ替える。
    /// </summary>
    internal static class SceneNoteQuery
    {
        public static List<SceneNote> Apply(IEnumerable<SceneNote> source, UStickiesUserSettings settings)
        {
            var terms = settings.searchText.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries);

            var filtered = source.Where(note => Matches(note, settings, terms));
            return settings.sortMode switch
            {
                SceneNoteSortMode.Oldest => filtered.OrderBy(note => note.createdAtUtcTicks).ToList(),
                SceneNoteSortMode.Registration => filtered.OrderBy(note => note.registrationOrder).ToList(),
                SceneNoteSortMode.Category => filtered
                    .OrderBy(note => CategoryIndex(note.categoryId))
                    .ThenBy(note => note.registrationOrder)
                    .ToList(),
                _ => filtered.OrderByDescending(note => note.createdAtUtcTicks).ToList()
            };
        }

        private static bool Matches(SceneNote note, UStickiesUserSettings settings, IReadOnlyList<string> terms)
        {
            if (!settings.IsCategorySelected(UStickiesProjectSettings.instance.ResolveId(note.categoryId)))
            {
                return false;
            }

            if (settings.completionFilter == SceneNoteCompletionFilter.Undone && note.done
                || settings.completionFilter == SceneNoteCompletionFilter.Done && !note.done)
            {
                return false;
            }

            for (var index = 0; index < terms.Count; index++)
            {
                if (note.body.IndexOf(terms[index], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CategoryIndex(string categoryId)
        {
            var categories = UStickiesProjectSettings.instance.categories;
            var resolvedId = UStickiesProjectSettings.instance.ResolveId(categoryId);
            for (var index = 0; index < categories.Count; index++)
            {
                if (categories[index].id == resolvedId)
                {
                    return index;
                }
            }

            return int.MaxValue;
        }
    }
}
