using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tenkai.UStickies
{
    /// <summary>
    /// Scene View上のノート描画、選択、移動、作成メニューを制御する。
    /// </summary>
    [InitializeOnLoad]
    internal static class UStickiesSceneViewController
    {
        private const float IconSize = 28f;
        private const float CardPadding = 6f;
        private const float ScrollbarWidth = 16f;

        private static readonly Dictionary<string, Vector2> CardScrollPositions = new();
        private static readonly Dictionary<string, Rect> LastCardRects = new();
        private static string _draggingNoteId;
        private static Plane _dragPlane;
        private static Vector3 _dragOffset;
        private static bool _draggingViewOffset;
        private static Vector2 _viewDragOffset;



        // ===== Scene GUI Entry ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Scene GUI Entry

        static UStickiesSceneViewController()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private static void DuringSceneGui(SceneView sceneView)
        {
            var evt = Event.current;
            if (evt.type == EventType.ContextClick)
            {
                ShowCreationMenu(sceneView, evt.mousePosition);
                evt.Use();
                return;
            }

            var settings = UStickiesUserSettings.instance;
            if (!settings.notesVisible)
            {
                return;
            }

            var entries = VisibleEntries(settings)
                .Where(entry => sceneView.camera.WorldToViewportPoint(entry.note.position).z > 0f)
                .ToList();
            if (entries.Count == 0)
            {
                HandleEmptySelection();
                return;
            }

            foreach (var entry in SceneNoteRepository.GetOpenDatabases())
            {
                SceneNoteMutationService.Reconcile(entry.scene, entry.database);
            }

            var ordered = entries
                .OrderBy(entry => entry.note.id == SceneNoteSelection.selectedNoteId ? 1 : 0)
                .ToList();
            var iconRects = ordered.ToDictionary(
                entry => entry.note.id,
                entry => IconRect(entry.note.position, entry.note.viewOffset));

            HandleInput(sceneView, ordered, iconRects);

            Handles.BeginGUI();
            DrawIcons(sceneView, ordered, iconRects);
            DrawCards(ordered, iconRects);
            Handles.EndGUI();
        }

        private static List<SceneNoteEntry> VisibleEntries(UStickiesUserSettings settings)
        {
            var result = new List<SceneNoteEntry>();
            foreach (var entry in SceneNoteRepository.GetOpenDatabases())
            {
                var notes = settings.applyFiltersToSceneView
                    ? SceneNoteQuery.Apply(entry.database.notes, settings)
                    : new List<SceneNote>(entry.database.notes);
                result.AddRange(notes.Select(note => new SceneNoteEntry(entry.scene, entry.database, note)));
            }

            return result;
        }

        #endregion



        // ===== Interaction And Rendering ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Interaction And Rendering

        private static void HandleInput(
            SceneView sceneView,
            IReadOnlyList<SceneNoteEntry> entries,
            IReadOnlyDictionary<string, Rect> iconRects)
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                SceneNoteSelection.Clear();
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt)
            {
                var hit = HitTest(entries, iconRects, evt.mousePosition);
                if (hit.note != null)
                {
                    SceneNoteSelection.Select(hit.note.id);
                    if (evt.control || evt.command)
                    {
                        BeginViewOffsetDrag(hit, evt.mousePosition);
                    }
                    else if (evt.clickCount >= 2)
                    {
                        UStickiesNoteEditorWindow.Open(hit.scene, hit.note, false);
                    }
                    else if (evt.shift && !hit.note.isBound)
                    {
                        BeginDrag(sceneView, hit, evt.mousePosition);
                    }

                    evt.Use();
                    return;
                }

                if (!LastCardRects.Values.Any(rect => rect.Contains(evt.mousePosition)))
                {
                    SceneNoteSelection.Clear();
                }
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0 && !string.IsNullOrEmpty(_draggingNoteId))
            {
                var hit = entries.FirstOrDefault(entry => entry.note.id == _draggingNoteId);
                if (hit.note != null && _draggingViewOffset)
                {
                    SceneNoteMutationService.SetViewOffsetDuringDrag(
                        hit.scene,
                        hit.database,
                        hit.note,
                        evt.mousePosition + _viewDragOffset);
                    evt.Use();
                }
                else if (hit.note != null && TryProjectToDragPlane(evt.mousePosition, out var position))
                {
                    SceneNoteMutationService.MoveDuringDrag(
                        hit.scene,
                        hit.database,
                        hit.note,
                        position + _dragOffset);
                    evt.Use();
                }
            }

            if (evt.rawType == EventType.MouseUp && evt.button == 0)
            {
                _draggingNoteId = null;
                _draggingViewOffset = false;
            }
        }

        private static void DrawIcons(
            SceneView sceneView,
            IReadOnlyList<SceneNoteEntry> entries,
            IReadOnlyDictionary<string, Rect> iconRects)
        {
            foreach (var entry in entries)
            {
                var note = entry.note;
                var category = UStickiesProjectSettings.instance.Resolve(note.categoryId);
                var selected = note.id == SceneNoteSelection.selectedNoteId;
                var alpha = selected ? 1f : note.done ? 0.35f : 0.5f;
                if (IsOccluded(sceneView, note))
                {
                    alpha *= 0.48f;
                }

                var previousColor = GUI.color;
                GUI.color = new Color(category.color.r, category.color.g, category.color.b, alpha);
                GUI.DrawTexture(
                    iconRects[note.id],
                    note.done ? UStickiesIcons.done : UStickiesIcons.note,
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = previousColor;
            }
        }

        private static void DrawCards(
            IReadOnlyList<SceneNoteEntry> entries,
            IReadOnlyDictionary<string, Rect> iconRects)
        {
            LastCardRects.Clear();
            var cards = entries
                .Where(entry => entry.note.pinned || entry.note.id == SceneNoteSelection.selectedNoteId)
                .OrderBy(entry => entry.note.id == SceneNoteSelection.selectedNoteId ? 1 : 0);

            foreach (var entry in cards)
            {
                var note = entry.note;
                var iconRect = iconRects[note.id];
                var cardRect = FindCardRect(iconRect, note);
                LastCardRects[note.id] = cardRect;

                DrawCard(cardRect, note);
            }
        }

        private static void DrawCard(Rect rect, SceneNote note)
        {
            var bodyStyle = CreateBodyStyle(true);
            var settings = UStickiesUserSettings.instance;
            if (settings.useCustomCardTextColor)
            {
                ApplyTextColor(bodyStyle, settings.cardTextColor);
            }

            var text = string.IsNullOrEmpty(note.body) ? "空のノート" : note.body;
            var content = new GUIContent(text);
            var projectSettings = UStickiesProjectSettings.instance;
            var categoryColor = projectSettings.Resolve(note.categoryId).color;
            var backgroundColor = projectSettings.useCustomCardBackgroundColor
                ? projectSettings.cardBackgroundColor
                : categoryColor;
            EditorGUI.DrawRect(
                rect,
                new Color(
                    backgroundColor.r,
                    backgroundColor.g,
                    backgroundColor.b,
                    projectSettings.cardBackgroundOpacity));

            var viewportRect = new Rect(
                rect.x + CardPadding,
                rect.y + CardPadding,
                rect.width - CardPadding * 2f,
                rect.height - CardPadding * 2f);
            var bodyHeight = bodyStyle.CalcHeight(content, viewportRect.width);
            if (bodyHeight <= viewportRect.height)
            {
                GUI.Label(viewportRect, content, bodyStyle);
                CardScrollPositions[note.id] = Vector2.zero;
                return;
            }

            var bodyWidth = viewportRect.width - ScrollbarWidth;
            bodyHeight = bodyStyle.CalcHeight(content, bodyWidth);
            var scroll = CardScrollPositions.TryGetValue(note.id, out var savedScroll) ? savedScroll : Vector2.zero;
            scroll = GUI.BeginScrollView(
                viewportRect,
                scroll,
                new Rect(0f, 0f, bodyWidth, bodyHeight),
                false,
                true);
            GUI.Label(new Rect(0f, 0f, bodyWidth, bodyHeight), content, bodyStyle);
            GUI.EndScrollView();
            CardScrollPositions[note.id] = scroll;
        }

        /// <summary>
        /// 操作状態によって文字色が変わらないように、すべてのGUIStyleStateへ同じ色を設定する。
        /// </summary>
        private static void ApplyTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        /// <summary>
        /// Project Settingsの文字サイズを反映した本文用スタイルを作成する。
        /// </summary>
        private static GUIStyle CreateBodyStyle(bool wordWrap)
        {
            var style = new GUIStyle(EditorStyles.label) { wordWrap = wordWrap };
            var settings = UStickiesProjectSettings.instance;
            if (settings.useCustomCardFontSize)
            {
                style.fontSize = settings.cardFontSize;
            }

            return style;
        }

        /// <summary>
        /// 内容表示をアイコンの右側に固定する。
        /// </summary>
        private static Rect FindCardRect(Rect iconRect, SceneNote note)
        {
            var measurementStyle = CreateBodyStyle(false);
            var text = string.IsNullOrEmpty(note.body) ? "空のノート" : note.body;
            var content = new GUIContent(text);
            var width = Mathf.Min(
                measurementStyle.CalcSize(content).x + CardPadding * 2f,
                UStickiesUserSettings.instance.cardMaximumWidth);
            var bodyStyle = new GUIStyle(measurementStyle) { wordWrap = true };
            var height = Mathf.Min(
                bodyStyle.CalcHeight(content, width - CardPadding * 2f) + CardPadding * 2f,
                UStickiesUserSettings.instance.cardMaximumHeight);
            const float gap = 14f;
            return new Rect(iconRect.xMax + gap, iconRect.yMin, width, height);
        }

        private static void BeginDrag(SceneView sceneView, SceneNoteEntry entry, Vector2 mousePosition)
        {
            _draggingNoteId = entry.note.id;
            _draggingViewOffset = false;
            _dragPlane = new Plane(sceneView.camera.transform.forward, entry.note.position);
            _dragOffset = TryProjectToDragPlane(mousePosition, out var hit)
                ? entry.note.position - hit
                : Vector3.zero;
            Undo.RecordObject(entry.database, "Move UStickies Note");
        }

        private static void BeginViewOffsetDrag(SceneNoteEntry entry, Vector2 mousePosition)
        {
            _draggingNoteId = entry.note.id;
            _draggingViewOffset = true;
            _viewDragOffset = entry.note.viewOffset - mousePosition;
            Undo.RecordObject(entry.database, "Move UStickies Note View Offset");
        }

        private static bool TryProjectToDragPlane(Vector2 mousePosition, out Vector3 position)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (_dragPlane.Raycast(ray, out var distance))
            {
                position = ray.GetPoint(distance);
                return true;
            }

            position = default;
            return false;
        }

        private static SceneNoteEntry HitTest(
            IReadOnlyList<SceneNoteEntry> entries,
            IReadOnlyDictionary<string, Rect> iconRects,
            Vector2 mousePosition)
        {
            for (var index = entries.Count - 1; index >= 0; index--)
            {
                var entry = entries[index];
                if (iconRects[entry.note.id].Contains(mousePosition))
                {
                    return entry;
                }
            }

            return default;
        }

        private static bool IsOccluded(SceneView sceneView, SceneNote note)
        {
            var cameraPosition = sceneView.camera.transform.position;
            var direction = note.position - cameraPosition;
            var distance = direction.magnitude;
            if (distance <= 0.05f)
            {
                return false;
            }

            foreach (var hit in Physics.RaycastAll(cameraPosition, direction.normalized, distance - 0.03f))
            {
                if (note.target != null && hit.transform.IsChildOf(note.target.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        #endregion



        // ===== Creation And Geometry ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
        #region Creation And Geometry

        private static void ShowCreationMenu(SceneView sceneView, Vector2 mousePosition)
        {
            var menu = new GenericMenu();
            var fixedPosition = PositionFromMouse(sceneView, mousePosition);
            var scene = SceneManager.GetActiveScene();
            menu.AddItem(new GUIContent("UStickies/Add Note"), false, () =>
            {
                var note = SceneNoteMutationService.Add(scene, fixedPosition);
                if (note != null)
                {
                    UStickiesNoteEditorWindow.Open(scene, note, true);
                }
            });

            var selected = Selection.activeGameObject;
            if (selected != null && selected.scene.IsValid())
            {
                menu.AddItem(new GUIContent("UStickies/Add Note to Selected GameObject"), false, () =>
                {
                    var note = SceneNoteMutationService.Add(selected.scene, selected.transform.position, selected);
                    if (note != null)
                    {
                        UStickiesNoteEditorWindow.Open(selected.scene, note, true);
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("UStickies/Add Note to Selected GameObject"));
            }

            menu.ShowAsContext();
        }

        private static Vector3 PositionFromMouse(SceneView sceneView, Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                return hit.point;
            }

            var pivotDistance = Vector3.Dot(sceneView.pivot - ray.origin, ray.direction);
            return ray.GetPoint(Mathf.Max(0.1f, pivotDistance));
        }

        private static Rect IconRect(Vector3 position, Vector2 viewOffset)
        {
            var guiPosition = HandleUtility.WorldToGUIPoint(position) + viewOffset;
            return new Rect(
                guiPosition.x - IconSize * 0.5f,
                guiPosition.y - IconSize * 0.5f,
                IconSize,
                IconSize);
        }

        private static void HandleEmptySelection()
        {
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt)
            {
                SceneNoteSelection.Clear();
            }
        }

        #endregion

        private readonly struct SceneNoteEntry
        {
            public readonly Scene scene;
            public readonly SceneNoteDatabase database;
            public readonly SceneNote note;

            public SceneNoteEntry(Scene scene, SceneNoteDatabase database, SceneNote note)
            {
                this.scene = scene;
                this.database = database;
                this.note = note;
            }
        }
    }
}
