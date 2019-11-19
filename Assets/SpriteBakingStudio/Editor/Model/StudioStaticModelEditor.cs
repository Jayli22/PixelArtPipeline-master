﻿using UnityEngine;
using UnityEditor;

namespace SBS
{
    [CustomEditor(typeof(StudioStaticModel))]
    public class StudioStaticModelEditor : StudioModelEditor
    {
        private StudioStaticModel model = null;

        void OnEnable()
        {
            model = (StudioStaticModel)target;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (model == null)
                return;

            Undo.RecordObject(model, "Studio Static Model");

            EditorGUILayout.Space();

            model.meshRndr = (MeshRenderer)EditorGUILayout.ObjectField("Mesh Renderer", model.meshRndr, typeof(MeshRenderer), true);
            model.AutoFindMeshRenderer();

            EditorGUILayout.Space();

			DrawPivotOffsetFields(model);

            EditorGUILayout.Space();

			DrawModelSettingButton(model);
        }
    }
}
