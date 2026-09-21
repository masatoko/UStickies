using System;

using UnityEngine;

namespace Tenkai.UStickies
{
    [Serializable]
    internal sealed class UStickiesCategory
    {
        [SerializeField] private string _id;
        [SerializeField] private string _label;
        [SerializeField] private Color _color;
        [SerializeField] private bool _builtIn;

        public string id => _id;
        public string label => _label;
        public Color color => _color;
        public bool builtIn => _builtIn;

        public UStickiesCategory(string id, string label, Color color, bool builtIn)
        {
            _id = id;
            _label = label;
            _color = color;
            _builtIn = builtIn;
        }

        public void SetLabel(string value) =>
            _label = value;

        public void SetColor(Color value) =>
            _color = value;
    }

    [Serializable]
    internal sealed class UStickiesCategoryReplacement
    {
        [SerializeField] private string _sourceId;
        [SerializeField] private string _replacementId;

        public string sourceId => _sourceId;
        public string replacementId => _replacementId;

        public UStickiesCategoryReplacement(string sourceId, string replacementId)
        {
            _sourceId = sourceId;
            _replacementId = replacementId;
        }
    }
}
