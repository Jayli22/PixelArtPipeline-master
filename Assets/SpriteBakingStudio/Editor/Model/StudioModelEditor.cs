using UnityEngine;
using UnityEditor;

namespace SBS
{
    public class StudioModelEditor : Editor
    {
        protected void DrawPivotOffsetFields(StudioModel model)
        {
            model.pivotOffset = EditorGUILayout.Vector3Field("Pivot Offset", model.pivotOffset);
            if (model.pivotOffset.sqrMagnitude > 0)
            {
                EditorGUI.indentLevel++;
                model.directionDependentPivot = EditorGUILayout.Toggle("Direction Dependent", model.directionDependentPivot);
                EditorGUI.indentLevel--;
            }
        }

		protected void DrawModelSettingButton(StudioModel model)
        {
            SpriteBakingStudio studio = FindObjectOfType<SpriteBakingStudio>();
            if (studio == null)
                return;
            StudioSetting setting = studio.setting;

            if (!model.IsReady())
				return;
            
            if (DrawingUtils.DrawWideButton("Set as the Model"))
			{
				if (setting.model != model && setting.model != null)
				{
                    setting.model.gameObject.SetActive(false);
                    setting.model = null;
				}
				if (setting.model == null)
				{
                    setting.model = model;
					model.gameObject.SetActive(true);
				}

                studio.samplings.Clear();
                studio.selectedFrames.Clear();
            }
		}
    }
}
