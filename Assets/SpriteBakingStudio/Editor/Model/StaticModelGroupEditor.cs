using System.IO;
using UnityEngine;
using UnityEditor;

namespace SBS
{
    [CustomEditor(typeof(StaticModelGroup))]
    public class StaticModelGroupEditor : StudioModelEditor
    {
        private StaticModelGroup group = null;

        void OnEnable()
        {
            group = (StaticModelGroup)target;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (group == null)
                return;

            Undo.RecordObject(group, "Static Model Group");

            DrawRootDirectoryFields();

            EditorGUILayout.Space();

            DrawModelToggles();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawPivotOffsetFields(group);
            if(EditorGUI.EndChangeCheck())
            {
                foreach (StaticModelPair pair in group.modelPairs)
                {
                    pair.Model.pivotOffset = group.pivotOffset;
                    pair.Model.directionDependentPivot = group.directionDependentPivot;
                }
            }

            EditorGUILayout.Space();

            if (group.GetCheckedModels().Count == 0)
            {
                EditorGUILayout.HelpBox("At least one sub model should be checked!", MessageType.Warning);
                return;
            }

            DrawModelSettingButton(group);
        }

        private void DrawRootDirectoryFields()
        {
            EditorGUI.BeginChangeCheck();
            group.rootDirectory = EditorGUILayout.TextField("Objects Root Directory", group.rootDirectory);
            bool needRefresh = EditorGUI.EndChangeCheck();

            EditorGUILayout.BeginHorizontal();
            {
                if (DrawingUtils.DrawNarrowButton("Choose Directory"))
                {
                    group.rootDirectory = EditorUtility.OpenFolderPanel("Choose a directory",
                        (group.rootDirectory != null && group.rootDirectory.Length > 0) ? group.rootDirectory : Application.dataPath, "");
                    needRefresh = true;
                }
                needRefresh |= DrawingUtils.DrawNarrowButton("Refresh Sub Models");
            }
            EditorGUILayout.EndHorizontal();
            
            if (group.rootDirectory == null || group.rootDirectory.Length == 0 || !Directory.Exists(group.rootDirectory))
                return;

            if (group.rootDirectory.IndexOf(Application.dataPath) < 0)
            {
                EditorGUILayout.HelpBox("A directory should be in this project's Asset directory!", MessageType.Warning);
                return;
            }

            if (needRefresh)
                group.RefreshModels();
        }

        private void DrawModelToggles()
        {
            if (group.modelPairs.Count == 0)
                return;

            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            foreach (StaticModelPair pair in group.modelPairs)
                pair.Checking = EditorGUILayout.Toggle(pair.Model.name, pair.Checking);

            GUILayout.EndVertical(); // HelpBox
        }
    }
}
