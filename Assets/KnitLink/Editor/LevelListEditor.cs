// Inspector for the LevelList asset: the normal list (drag JSON files, reorder by dragging) plus
//   Validate all       checks every level and shows the result next to its number
//   Fill from folder   replaces the list with every level_XX.json in Resources/Levels, in number order
//   Open in editor     opens the selected row in the Level Editor
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KnitLink.EditorTools
{
    [CustomEditor(typeof(LevelList))]
    public class LevelListEditor : Editor
    {
        List<LevelReport> reports;
        Vector2 scroll;

        public override void OnInspectorGUI()
        {
            var list = (LevelList)target;
            EditorGUILayout.HelpBox("Level 1 is the first file. Drag files in or reorder them; the game uses this order.", MessageType.Info);
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate all")) reports = KnitLinkSetup.ValidateAll(list, true);
                if (GUILayout.Button("Fill from folder") && EditorUtility.DisplayDialog("Fill from folder",
                        "Replace the list with every level_XX.json in Resources/Levels (number order)?", "Replace", "Cancel"))
                {
                    Undo.RecordObject(list, "Fill level list");
                    list.levels = KnitLinkSetup.LevelFilesInFolder(); EditorUtility.SetDirty(list); reports = null;
                }
                if (GUILayout.Button("Level Editor")) LevelEditorWindow.Open(0);
            }
            if (reports == null) return;

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(420));
            foreach (var r in reports)
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
                string color = !r.Ok ? "#e05252" : r.warnings.Count > 0 ? "#d9a02b" : "#4fae5b";
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"<color={color}><b>Lv{r.num}</b></color>  {r.Summary}", style);
                    if (GUILayout.Button("Edit", GUILayout.Width(44))) LevelEditorWindow.Open(r.num - 1);
                }
                foreach (var e in r.errors) EditorGUILayout.LabelField("    ERROR: " + e, style);
                foreach (var w in r.warnings) EditorGUILayout.LabelField("    warning: " + w, style);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
