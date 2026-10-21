// The play order of the levels. Drag level JSON files into "Levels" in the Inspector:
// the first file is Level 1, the second Level 2, ... Reorder by dragging.
// Buttons "Validate all" / "Fill from folder" are drawn by LevelListEditor (Editor folder).
using UnityEngine;

namespace KnitLink
{
    [CreateAssetMenu(menuName = "Knit Link/Level List", fileName = "LevelList")]
    public class LevelList : ScriptableObject
    {
        [Tooltip("Level 1 first. Each entry is one level_XX.json file.")]
        public TextAsset[] levels = new TextAsset[0];
    }
}
