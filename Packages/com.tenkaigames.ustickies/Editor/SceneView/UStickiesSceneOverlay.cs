using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Tenkai.UStickies
{
    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class UStickiesVisibilityToggle : EditorToolbarToggle
    {
        public const string Id = "UStickies/Visibility";

        public UStickiesVisibilityToggle()
        {
            text = "UStickies";
            tooltip = "Show or hide UStickies notes";
            SetValueWithoutNotify(UStickiesUserSettings.instance.notesVisible);
            RegisterCallback<ChangeEvent<bool>>(evt =>
                UStickiesUserSettings.instance.SetNotesVisible(evt.newValue));
            RegisterCallback<AttachToPanelEvent>(_ => UStickiesUserSettings.changed += RefreshValue);
            RegisterCallback<DetachFromPanelEvent>(_ => UStickiesUserSettings.changed -= RefreshValue);
        }

        private void RefreshValue() =>
            SetValueWithoutNotify(UStickiesUserSettings.instance.notesVisible);
    }

    [Overlay(typeof(SceneView), "UStickies", true)]
    internal sealed class UStickiesSceneOverlay : ToolbarOverlay
    {
        public UStickiesSceneOverlay()
            : base(UStickiesVisibilityToggle.Id)
        {
        }
    }
}
